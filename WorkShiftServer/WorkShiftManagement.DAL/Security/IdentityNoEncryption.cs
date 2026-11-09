using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace WorkShiftManagement.DAL.Security;

/// <summary>
/// Deterministic authenticated AES-256-GCM encryption for User.IdentityNo.
/// Existing unencrypted rows remain readable and are encrypted when created or updated.
/// </summary>
public static class IdentityNoEncryption
{
    public const string Prefix = "ENC:v1:";

    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("WorkShift.User.IdentityNo.v1");
    private static readonly byte[] EncKeyInfo = Encoding.UTF8.GetBytes("enc-aes256gcm-v1");
    private static readonly byte[] NonceKeyInfo = Encoding.UTF8.GetBytes("nonce-hmacsha256-v1");

    private sealed record KeyMaterial(byte[] EncKey, byte[] NonceKey);

    private static KeyMaterial? _keyMaterial;

    public static bool IsConfigured => Volatile.Read(ref _keyMaterial) is not null;

    public static void Configure(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length < 16)
        {
            throw new ArgumentException("Encryption:IdentityNoKey must be at least 16 characters.", nameof(key));
        }

        var ikm = SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()));
        var encKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, info: EncKeyInfo);
        var nonceKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, info: NonceKeyInfo);

        Volatile.Write(ref _keyMaterial, new KeyMaterial(encKey, nonceKey));
    }

    public static bool IsEncrypted(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return trimmed;
        }

        var keys = RequireKeys();
        var plaintextBytes = Encoding.UTF8.GetBytes(trimmed);

        Span<byte> nonceHmac = stackalloc byte[32];
        HMACSHA256.HashData(keys.NonceKey, plaintextBytes, nonceHmac);
        var nonce = nonceHmac[..12];

        var payload = new byte[12 + 16 + plaintextBytes.Length];
        nonce.CopyTo(payload.AsSpan(0, 12));
        var tagSpan = payload.AsSpan(12, 16);
        var cipherSpan = payload.AsSpan(28, plaintextBytes.Length);

        using var aes = new AesGcm(keys.EncKey, 16);
        aes.Encrypt(nonce, plaintextBytes, cipherSpan, tagSpan, AssociatedData);

        return Prefix + Convert.ToBase64String(payload);
    }

    public static string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        var keys = RequireKeys();
        var encoded = value.AsSpan(Prefix.Length);
        var payload = Convert.FromBase64String(encoded.ToString());

        if (payload.Length < 12 + 16)
        {
            throw new CryptographicException("Encrypted IdentityNo payload is malformed.");
        }

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var cipher = payload.AsSpan(28);
        var plaintextBytes = new byte[cipher.Length];

        using var aes = new AesGcm(keys.EncKey, 16);
        aes.Decrypt(nonce, cipher, tag, plaintextBytes, AssociatedData);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var plain = value.Trim();
        if (plain.StartsWith(Prefix, StringComparison.Ordinal))
        {
            try
            {
                plain = Unprotect(plain);
            }
            catch
            {
                return "***";
            }
        }

        if (plain.Length <= 4)
        {
            return new string('*', plain.Length);
        }

        return new string('*', plain.Length - 4) + plain[^4..];
    }

    private static KeyMaterial RequireKeys() =>
        Volatile.Read(ref _keyMaterial)
        ?? throw new InvalidOperationException("IdentityNo encryption key is not configured. Call IdentityNoEncryption.Configure(...) at startup.");
}

