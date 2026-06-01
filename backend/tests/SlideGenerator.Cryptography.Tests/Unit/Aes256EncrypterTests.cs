/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography.Tests
 * File: Aes256EncrypterTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using SlideGenerator.Cryptography.Infrastructure;
using Xunit;

namespace SlideGenerator.Cryptography.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Aes256Encrypter" />, covering AES-GCM round-trip behavior,
///     nonce uniqueness, encoding format, and security-sensitive edge cases that the current
///     implementation gets wrong (tamper detection and empty-input handling).
/// </summary>
public sealed class Aes256EncrypterTests
{
    #region BUG — Tampering is silently swallowed

    /// <summary>
    ///     Verifies that <see cref="Aes256Encrypter.Decrypt" /> surfaces tampering by throwing
    ///     <see cref="System.Security.Cryptography.CryptographicException" /> when the AES-GCM
    ///     authentication tag does not match the ciphertext.
    /// </summary>
    [Fact(DisplayName = "Tampered ciphertext throws CryptographicException")]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var encrypter = new Aes256Encrypter();
        var cipher = encrypter.Encrypt("super-secret");
        var raw = Convert.FromBase64String(cipher);

        // Flip the last byte of the ciphertext payload — corrupts the AES-GCM tag region.
        raw[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(raw);

        var act = () => encrypter.Decrypt(tampered);

        act.Should().Throw<CryptographicException>(
            "a security primitive must surface tag-mismatch failures to the caller");
    }

    #endregion

    #region Round-trip

    /// <summary>
    ///     Verifies that a plaintext encrypted by <see cref="Aes256Encrypter.Encrypt" /> can be
    ///     recovered exactly by <see cref="Aes256Encrypter.Decrypt" /> on the same machine/user
    ///     context (key derivation is deterministic for the current process identity).
    /// </summary>
    [Theory]
    [InlineData("hello world")]
    [InlineData("a")]
    [InlineData("Tiếng Việt có dấu — Unicode 漢字 🎉")]
    [InlineData("{\"json\":\"payload\",\"nested\":{\"k\":1}}")]
    public void EncryptDecrypt_RoundTrip_RecoversOriginalPlaintext(string plaintext)
    {
        var encrypter = new Aes256Encrypter();

        var cipher = encrypter.Encrypt(plaintext);
        var recovered = encrypter.Decrypt(cipher);

        recovered.Should().Be(plaintext);
    }

    /// <summary>
    ///     Verifies that the ciphertext is a valid Base64 string distinct from the plaintext.
    /// </summary>
    [Fact]
    public void Encrypt_NonEmptyPlaintext_ReturnsBase64StringDistinctFromInput()
    {
        var encrypter = new Aes256Encrypter();

        var cipher = encrypter.Encrypt("secret-value");

        cipher.Should().NotBe("secret-value");
        var act = () => Convert.FromBase64String(cipher);
        act.Should().NotThrow();
    }

    /// <summary>
    ///     Verifies that encrypting the same plaintext twice produces different ciphertexts
    ///     (random 12-byte nonce per encryption). This is a core correctness property of AES-GCM.
    /// </summary>
    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertexts()
    {
        var encrypter = new Aes256Encrypter();

        var first = encrypter.Encrypt("same-input");
        var second = encrypter.Encrypt("same-input");

        first.Should().NotBe(second);
    }

    /// <summary>
    ///     Verifies that the ciphertext length equals 12 (nonce) + 16 (tag) + plaintext bytes,
    ///     after Base64 decoding. Locks in the wire format so future refactors cannot silently
    ///     break interoperability with already-encrypted data on disk.
    /// </summary>
    [Fact]
    public void Encrypt_AsciiPlaintext_CiphertextLengthMatchesNoncePlusTagPlusPayload()
    {
        var encrypter = new Aes256Encrypter();
        const string plaintext = "abcdefghij"; // 10 bytes UTF-8

        var cipher = encrypter.Encrypt(plaintext);
        var raw = Convert.FromBase64String(cipher);

        raw.Length.Should().Be(12 + 16 + Encoding.UTF8.GetByteCount(plaintext));
    }

    #endregion

    #region Empty plaintext — encrypted output must still carry nonce + tag

    /// <summary>
    ///     Verifies that encrypting an empty string still produces a non-empty Base64 string
    ///     containing the 12-byte nonce and 16-byte tag. Prevents the previous bypass where
    ///     <c>Encrypt("")</c> returned <c>""</c> verbatim and let callers store secrets in clear.
    /// </summary>
    [Fact(DisplayName = "Encrypt(\"\") produces non-empty Base64 carrying nonce + tag")]
    public void Encrypt_EmptyString_ProducesNonEmptyBase64()
    {
        var encrypter = new Aes256Encrypter();

        var cipher = encrypter.Encrypt(string.Empty);

        cipher.Should().NotBeEmpty();
        var raw = Convert.FromBase64String(cipher);
        raw.Length.Should().Be(12 + 16, "empty plaintext still carries nonce + tag");
    }

    /// <summary>
    ///     Verifies the round-trip property for empty plaintext: encrypting and decrypting
    ///     yields the empty string back.
    /// </summary>
    [Fact]
    public void EncryptDecrypt_EmptyString_RoundTrips()
    {
        var encrypter = new Aes256Encrypter();

        var cipher = encrypter.Encrypt(string.Empty);
        var recovered = encrypter.Decrypt(cipher);

        recovered.Should().Be(string.Empty);
    }

    /// <summary>
    ///     Companion property: a non-empty string in cipher form must never be empty after
    ///     encryption. This guarantees the format invariant the empty-string bypass violates.
    /// </summary>
    [Fact]
    public void Encrypt_NonEmptyPlaintext_NeverReturnsEmptyString()
    {
        var encrypter = new Aes256Encrypter();

        var cipher = encrypter.Encrypt(" ");

        cipher.Should().NotBeEmpty();
    }

    #endregion

    #region Decrypt — invalid input handling

    /// <summary>
    ///     Verifies that decrypting an empty string returns an empty string (matches the
    ///     existing short-circuit). Locks in the current contract until the empty-bypass fix
    ///     lands so that consumers are not silently broken by an unrelated change.
    /// </summary>
    [Fact]
    public void Decrypt_EmptyString_ReturnsEmptyString()
    {
        var encrypter = new Aes256Encrypter();

        var result = encrypter.Decrypt(string.Empty);

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that non-Base64 input surfaces a <see cref="FormatException" /> rather than
    ///     silently returning empty. Callers must distinguish "malformed input" from "valid empty".
    /// </summary>
    [Fact]
    public void Decrypt_NonBase64Garbage_ThrowsFormatException()
    {
        var encrypter = new Aes256Encrypter();

        var act = () => encrypter.Decrypt("***not-base64***");

        act.Should().Throw<FormatException>();
    }

    /// <summary>
    ///     Verifies that a Base64 payload shorter than 12 (nonce) + 16 (tag) bytes is rejected
    ///     with a <see cref="System.Security.Cryptography.CryptographicException" />.
    /// </summary>
    [Fact]
    public void Decrypt_CiphertextShorterThanNoncePlusTag_ThrowsCryptographicException()
    {
        var encrypter = new Aes256Encrypter();
        var tooShort = Convert.ToBase64String(new byte[8]);

        var act = () => encrypter.Decrypt(tooShort);

        act.Should().Throw<CryptographicException>();
    }

    #endregion
}