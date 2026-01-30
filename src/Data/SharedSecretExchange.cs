using BetterAmongUs.Helpers;
using System.Security.Cryptography;

namespace BetterAmongUs.Data;

/// <summary>
/// Handles secure key exchange using Elliptic Curve Diffie-Hellman (ECDH) for establishing shared secrets.
/// </summary>
internal sealed class SharedSecretExchange
{
    private readonly ECDiffieHellman dh;
    private byte[] publicKey;
    private int tempKey;
    private byte[] sharedSecret = [];

    /// <summary>
    /// Initializes a new instance of the SharedSecretExchange class with a new ECDH key pair.
    /// </summary>
    internal SharedSecretExchange()
    {
        dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        publicKey = dh.ExportSubjectPublicKeyInfo();
        var random = new Random();
        tempKey = random.Next();
    }

    /// <summary>
    /// Gets the public key for key exchange.
    /// </summary>
    /// <returns>The public key in SubjectPublicKeyInfo format.</returns>
    internal byte[] GetPublicKey()
    {
        return publicKey;
    }

    /// <summary>
    /// Gets the temporary key used for initial communication.
    /// </summary>
    /// <returns>A random integer used as a temporary key.</returns>
    internal int GetTempKey()
    {
        return tempKey;
    }

    /// <summary>
    /// Gets the established shared secret.
    /// </summary>
    /// <returns>The shared secret byte array.</returns>
    internal byte[] GetSharedSecret() => sharedSecret;

    /// <summary>
    /// Generates a shared secret using another party's public key.
    /// </summary>
    /// <param name="otherPartyPublicKey">The other party's public key in SubjectPublicKeyInfo format.</param>
    /// <returns>The shared secret byte array, or an empty array if generation fails.</returns>
    internal byte[] GenerateSharedSecret(byte[] otherPartyPublicKey)
    {
        if (sharedSecret.Length > 0) return sharedSecret;

        try
        {
            using ECDiffieHellman otherPartyDH = ECDiffieHellman.Create();
            otherPartyDH.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);

            sharedSecret = dh.DeriveKeyMaterial(otherPartyDH.PublicKey);
            dh.Dispose();
            return sharedSecret;
        }
        catch (Exception ex)
        {
            Logger_.Error($"Error generating shared secret: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Gets a numeric hash of the shared secret for verification purposes.
    /// </summary>
    /// <returns>A 32-bit integer hash of the shared secret, or 0 if no shared secret exists.</returns>
    internal int GetSharedSecretHash()
    {
        if (sharedSecret.Length == 0)
            return 0;

        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(sharedSecret);
        int numericHash = BitConverter.ToInt32(hashBytes, 0);
        return Math.Abs(numericHash);
    }

    /// <summary>
    /// Gets a value indicating whether the key exchange data has been cleared for security.
    /// </summary>
    internal bool HasBeenCleared { get; private set; }

    /// <summary>
    /// Clears all sensitive key exchange data for security purposes.
    /// </summary>
    internal void ClearData()
    {
        if (HasBeenCleared) return;
        HasBeenCleared = true;
        publicKey = [];
        tempKey = 0;
        sharedSecret = [];
    }
}