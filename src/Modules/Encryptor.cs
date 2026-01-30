using System.Security.Cryptography;
using System.Text;

namespace BetterAmongUs.Modules;

/// <summary>
/// Provides AES encryption and decryption utilities for sensitive data.
/// </summary>
internal static class Encryptor
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEF0123456789");

    /// <summary>
    /// Encrypts a plain text string using AES encryption.
    /// </summary>
    /// <param name="input">The plain text string to encrypt.</param>
    /// <returns>A base64-encoded string containing the encrypted data.</returns>
    internal static string Encrypt(string input)
    {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using (StreamWriter streamWriter = new(cryptoStream))
        {
            streamWriter.Write(input);
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    /// <summary>
    /// Decrypts an AES-encrypted base64 string back to plain text.
    /// </summary>
    /// <param name="input">The base64-encoded encrypted string.</param>
    /// <returns>The decrypted plain text string.</returns>
    internal static string Decrypt(string input)
    {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        using MemoryStream memoryStream = new(Convert.FromBase64String(input));
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader streamReader = new(cryptoStream);
        return streamReader.ReadToEnd();
    }
}