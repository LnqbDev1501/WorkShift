using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FaceAiSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public class FaceBiometricService : IFaceBiometricService, IDisposable
{
    private const int DefaultPoolSize = 3;
    private const int MaxPoolSize = 8;

    private readonly ILogger<FaceBiometricService> _logger;
    private readonly int _poolSize;
    private readonly int _intraOpThreads;
    private readonly bool _enableCpuMemArena;
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private readonly ConcurrentBag<BiometricWorker> _idleWorkers = new();
    private readonly List<BiometricWorker> _allWorkers = new();
    private readonly object _lifecycleLock = new();
    private Exception? _initException;
    private bool _disposed;

    private sealed class BiometricWorker : IDisposable
    {
        public IFaceDetectorWithLandmarks Detector { get; }
        public IFaceEmbeddingsGenerator Recognizer { get; }

        public BiometricWorker(IFaceDetectorWithLandmarks detector, IFaceEmbeddingsGenerator recognizer)
        {
            Detector = detector;
            Recognizer = recognizer;
        }

        public void Dispose()
        {
            (Detector as IDisposable)?.Dispose();
            (Recognizer as IDisposable)?.Dispose();
        }
    }

    public FaceBiometricService(ILogger<FaceBiometricService> logger, IConfiguration? configuration = null)
    {
        _logger = logger;
        var configuredPoolSize = configuration?.GetValue<int?>("Biometrics:WorkerPoolSize") ?? DefaultPoolSize;
        _poolSize = Math.Clamp(configuredPoolSize, 1, MaxPoolSize);
        _intraOpThreads = Math.Clamp(Environment.ProcessorCount >= 8 ? 4 : Environment.ProcessorCount / _poolSize, 1, 4);
        _enableCpuMemArena = configuration?.GetValue<bool?>("Biometrics:EnableCpuMemArena") ?? false;
        _poolSemaphore = new SemaphoreSlim(_poolSize, _poolSize);

        _logger.LogInformation(
            "FaceBiometricService registered (lazy-loading ONNX worker pool: max {PoolSize} workers, {IntraOpThreads} intra-op threads/worker, CpuMemArena={EnableArena}).",
            _poolSize,
            _intraOpThreads,
            _enableCpuMemArena);
    }

    private SessionOptions CreateWorkerSessionOptions()
    {
        return new SessionOptions
        {
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            InterOpNumThreads = 1,
            IntraOpNumThreads = _intraOpThreads,
            EnableCpuMemArena = _enableCpuMemArena
        };
    }

    private async Task<BiometricWorker> BorrowWorkerAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _poolSemaphore.WaitAsync(cancellationToken);
        bool releasePermitOnFailure = true;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_idleWorkers.TryTake(out var idleWorker))
            {
                releasePermitOnFailure = false;
                return idleWorker;
            }

            await _initSemaphore.WaitAsync(cancellationToken);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_idleWorkers.TryTake(out idleWorker))
                {
                    releasePermitOnFailure = false;
                    return idleWorker;
                }

                if (_initException != null)
                {
                    throw new InvalidOperationException(
                        $"Face biometric model initialization failed: {_initException.Message}",
                        _initException);
                }

                BiometricWorker createdWorker;
                try
                {
                    int workerIndex;
                    lock (_lifecycleLock)
                    {
                        workerIndex = _allWorkers.Count + 1;
                    }

                    _logger.LogInformation(
                        "Initializing ONNX biometric worker {WorkerIndex}/{PoolSize} (ArcFace and SCRFD models)...",
                        workerIndex,
                        _poolSize);

                    using var sessionOptions = CreateWorkerSessionOptions();
                    var detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks(sessionOptions);
                    IFaceEmbeddingsGenerator recognizer;
                    try
                    {
                        recognizer = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator(sessionOptions);
                    }
                    catch
                    {
                        (detector as IDisposable)?.Dispose();
                        throw;
                    }

                    createdWorker = new BiometricWorker(detector, recognizer);
                    lock (_lifecycleLock)
                    {
                        if (_disposed)
                        {
                            createdWorker.Dispose();
                            throw new ObjectDisposedException(nameof(FaceBiometricService));
                        }

                        _allWorkers.Add(createdWorker);
                    }

                    _logger.LogInformation(
                        "ONNX biometric worker {WorkerIndex}/{PoolSize} initialized successfully.",
                        workerIndex,
                        _poolSize);
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    bool hasHealthyWorkers;
                    lock (_lifecycleLock)
                    {
                        hasHealthyWorkers = _allWorkers.Count > 0;
                    }

                    if (!hasHealthyWorkers)
                    {
                        _initException = ex;
                    }

                    _logger.LogError(ex, "Failed to initialize FaceBiometricService with ArcFace and SCRFD models.");
                    throw;
                }

                releasePermitOnFailure = false;
                return createdWorker;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
        finally
        {
            if (releasePermitOnFailure)
            {
                try
                {
                    if (!_disposed)
                    {
                        _poolSemaphore.Release();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }

    private void ReturnWorker(BiometricWorker worker)
    {
        bool disposeWorker = false;
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                _allWorkers.Remove(worker);
                disposeWorker = true;
            }
            else
            {
                _idleWorkers.Add(worker);
            }
        }

        if (disposeWorker)
        {
            worker.Dispose();
        }

        try
        {
            if (!_disposed)
            {
                _poolSemaphore.Release();
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task<(float[] Embedding, float Confidence)> ExtractFaceEmbeddingAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            throw new ArgumentException("A valid face image file is required.", nameof(imageFile));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        await using var stream = imageFile.OpenReadStream();
        using var image = await Image.LoadAsync<Rgb24>(stream, cancellationToken);

        var worker = await BorrowWorkerAsync(cancellationToken);
        try
        {
            var detectedFaces = worker.Detector.DetectFaces(image);
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
                worker.Recognizer.AlignFaceUsingLandmarks(image, bestFace.Landmarks);
            }

            var embedding = worker.Recognizer.GenerateEmbedding(image);
            if (embedding == null || embedding.Length == 0)
            {
                throw new InvalidOperationException("Failed to generate facial biometric vector from the image.");
            }

            return (embedding, confidence);
        }
        finally
        {
            ReturnWorker(worker);
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
        List<BiometricWorker> workersToDispose;
        lock (_lifecycleLock)
        {
            if (_disposed) return;
            _disposed = true;

            workersToDispose = new List<BiometricWorker>();
            while (_idleWorkers.TryTake(out var idle))
            {
                workersToDispose.Add(idle);
                _allWorkers.Remove(idle);
            }
        }

        foreach (var worker in workersToDispose)
        {
            worker.Dispose();
        }

        _initSemaphore.Dispose();
        _poolSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
