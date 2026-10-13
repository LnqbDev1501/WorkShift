using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IFaceBiometricService
{
    /// <summary>
    /// Detects a face in the image, aligns landmarks, and extracts the 512-D ArcFace embedding vector with detection confidence.
    /// Throws InvalidOperationException if no face is detected or face confidence is below acceptable threshold.
    /// </summary>
    Task<(float[] Embedding, float Confidence)> ExtractFaceEmbeddingAsync(IFormFile imageFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes cosine similarity between two 512-D normalized embeddings.
    /// Returns true if similarity >= threshold (default 0.45 for ArcFace), along with the similarity score.
    /// </summary>
    (bool IsMatch, float Similarity) VerifyFaceMatch(float[] registeredEmbedding, float[] currentEmbedding, float threshold = 0.45f);
}
