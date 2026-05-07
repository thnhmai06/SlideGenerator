/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: CryptoHelper.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Security.Cryptography;
using System.Text;

namespace SlideGenerator.Settings.Security;

/// <summary>
///     Provides cross-platform cryptographic utilities using pure .NET APIs.
/// </summary>
internal static class CryptoHelper
{
    private static readonly byte[] Salt = "SlideGenerator_Salt_2026"u8.ToArray();
    private const int KeySize = 32; // AES-256
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    ///     Encrypts a plain text string using AES.
    ///     The key is derived from the machine name and a salt.
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

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
    ///     Decrypts an AES-GCM-encrypted string.
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            var key = DeriveKey();
            var fullData = Convert.FromBase64String(cipherText);

            var nonce = fullData[..NonceSize];
            var tag = fullData[NonceSize..(NonceSize + TagSize)];
            var ciphertext = fullData[(NonceSize + TagSize)..];
            var decryptedData = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, decryptedData);

            return Encoding.UTF8.GetString(decryptedData);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] DeriveKey()
    {
        var identity = Environment.MachineName + Environment.UserName + "SlideGenerator";
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(identity),
            Salt,
            iterations: 100000,
            HashAlgorithmName.SHA256,
            KeySize);
    }
}