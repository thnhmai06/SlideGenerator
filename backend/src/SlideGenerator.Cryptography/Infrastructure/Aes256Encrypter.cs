/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: Aes256Encrypter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Security.Cryptography;
using System.Text;
using SlideGenerator.Cryptography.Application.Abstractions;

namespace SlideGenerator.Cryptography.Infrastructure;

/// <summary>
///     Provides cross-platform cryptographic utilities using pure .NET APIs.
/// </summary>
internal sealed class Aes256Encrypter : IEncrypter
{
    private const int KeySize = 32; // AES-256
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] Salt = "SlideGenerator_Salt_2026"u8.ToArray();

    /// <summary>
    ///     Encrypts a plaintext string with AES-256-GCM. Returns a Base64 string containing
    ///     <c>nonce ‖ tag ‖ ciphertext</c>. Even an empty plaintext produces a non-empty result
    ///     (12-byte nonce + 16-byte tag) so callers cannot distinguish "encrypted" from
    ///     "never-encrypted" by length.
    /// </summary>
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var key = DeriveKey();
        var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    ///     Decrypts an AES-GCM-encrypted Base64 string. Surfaces every failure to the caller —
    ///     callers MUST handle <see cref="CryptographicException" /> (tag mismatch, malformed
    ///     payload) and <see cref="FormatException" /> (non-Base64 input). The previous bare
    ///     <c>catch</c> hid tampering from callers and is no longer present.
    /// </summary>
    /// <exception cref="ArgumentNullException">cipherText is null.</exception>
    /// <exception cref="FormatException">cipherText is not valid Base64.</exception>
    /// <exception cref="CryptographicException">payload too short, or AES-GCM tag mismatch.</exception>
    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        if (cipherText.Length == 0) return string.Empty;

        var key = DeriveKey();
        var fullData = Convert.FromBase64String(cipherText);

        if (fullData.Length < NonceSize + TagSize)
            throw new CryptographicException(
                $"Ciphertext is too short ({fullData.Length} bytes); expected at least {NonceSize + TagSize}.");

        var nonce = fullData[..NonceSize];
        var tag = fullData[NonceSize..(NonceSize + TagSize)];
        var ciphertext = fullData[(NonceSize + TagSize)..];
        var decryptedData = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, decryptedData);

        return Encoding.UTF8.GetString(decryptedData);
    }

    private static byte[] DeriveKey()
    {
        var identity = Environment.MachineName + Environment.UserName + "SlideGenerator";
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(identity),
            Salt,
            100000,
            HashAlgorithmName.SHA256,
            KeySize);
    }
}