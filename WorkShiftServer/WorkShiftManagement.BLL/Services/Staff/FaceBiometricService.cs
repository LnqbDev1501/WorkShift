using FaceAiSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public class FaceBiometricService : IFaceBiometricService, IDisposable
{
    private readonly ILogger<FaceBiometricService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IFaceDetectorWithLandmarks? _detector;
    private IFaceEmbeddingsGenerator? _recognizer;
    private bool _initialized;
    private Exception? _initException;
    private bool _disposed;

    public FaceBiometricService(ILogger<FaceBiometricService> logger)
    {
        _logger = logger;
        _logger.LogInformation("FaceBiometricService registered (lazy-loading ONNX models on demand).");
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        _lock.Wait();
        try
        {
            if (_initialized) return;

            if (_initException != null)
            {
                throw new InvalidOperationException($"Face biometric model initialization failed: {_initException.Message}", _initException);
            }

            try
            {
                _logger.LogInformation("Initializing FaceBiometricService with ArcFace and SCRFD ONNX models...");
                _detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
                _recognizer = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
                _initialized = true;
                _logger.LogInformation("FaceBiometricService initialized successfully.");
            }
            catch (Exception ex)
            {
                _initException = ex;
                _logger.LogError(ex, "Failed to initialize FaceBiometricService with ArcFace and SCRFD models.");
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<(float[] Embedding, float Confidence)> ExtractFaceEmbeddingAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            throw new ArgumentException("A valid face image file is required.", nameof(imageFile));
        }

        EnsureInitialized();

        await using var stream = imageFile.OpenReadStream();
        using var image = await Image.LoadAsync<Rgb24>(stream, cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var detectedFaces = _detector!.DetectFaces(image);
            if (detectedFaces == null || detectedFaces.Count == 0)
            {
                throw new InvalidOperationException("No human face detected in the image. Please position your face clearly in the camera frame with good lighting.");
            }

            var bestFace = detectedFaces.OrderByDescending(f => f.Confidence ?? 0).First();
            var confidence = bestFace.Confidence ?? 0f;

            if (confidence < 0.60f)
            {
                throw new InvalidOperationException($"Face detection confidence is too low ({confidence:P0} < 60%). Please face the camera directly in clear lighting.");
            }

            if (bestFace.Landmarks != null && bestFace.Landmarks.Count >= 5)
            {
                _recognizer!.AlignFaceUsingLandmarks(image, bestFace.Landmarks);
            }

            var embedding = _recognizer!.GenerateEmbedding(image);
            if (embedding == null || embedding.Length == 0)
            {
                throw new InvalidOperationException("Failed to generate facial biometric vector from the image.");
            }

            return (embedding, confidence);
        }
        finally
        {
            _lock.Release();
        }
    }

    public (bool IsMatch, float Similarity) VerifyFaceMatch(float[] registeredEmbedding, float[] currentEmbedding, float threshold = 0.45f)
    {
        if (registeredEmbedding == null || registeredEmbedding.Length == 0 ||
            currentEmbedding == null || currentEmbedding.Length == 0)
        {
            return (false, 0f);
        }

        int length = Math.Min(registeredEmbedding.Length, currentEmbedding.Length);
        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < length; i++)
        {
            dotProduct += registeredEmbedding[i] * currentEmbedding[i];
            normA += registeredEmbedding[i] * registeredEmbedding[i];
            normB += currentEmbedding[i] * currentEmbedding[i];
        }

        float denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        float similarity = denominator > 0f ? (dotProduct / denominator) : 0f;

        bool isMatch = similarity >= threshold;
        return (isMatch, similarity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_detector is IDisposable dispDetector)
        {
            dispDetector.Dispose();
        }

        if (_recognizer is IDisposable dispRecognizer)
        {
            dispRecognizer.Dispose();
        }

        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
