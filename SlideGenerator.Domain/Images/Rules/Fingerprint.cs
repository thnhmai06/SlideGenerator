using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SlideGenerator.Domain.Images.Rules;

/// <summary>
///     Provides utility methods for generating unique fingerprints and formatting numbers.
/// </summary>
/// <remarks>
///     This class ensures consistent hashing and culture-independent string formatting
///     for caching and identifying image states or rules.
/// </remarks>
public static class Fingerprint
{
    /// <summary>
    ///     Generates a SHA-256 hash from the provided string parts.
    /// </summary>
    /// <param name="parts">An array of string parts to combine and hash.</param>
    /// <returns>A lowercase hexadecimal string representing the hash.</returns>
    public static string Hash(params string[] parts)
    {
        var input = string.Join("|", parts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    ///     Formats an integer as a string using the invariant culture.
    /// </summary>
    /// <param name="value">The <see cref="int" /> value to format.</param>
    /// <returns>The formatted string.</returns>
    public static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Formats an unsigned integer as a string using the invariant culture.
    /// </summary>
    /// <param name="value">The <see cref="uint" /> value to format.</param>
    /// <returns>The formatted string.</returns>
    public static string Number(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Formats a floating-point number as a string using the invariant culture and "G9" format.
    /// </summary>
    /// <param name="value">The <see cref="float" /> value to format.</param>
    /// <returns>The formatted string.</returns>
    public static string Number(float value)
    {
        return value.ToString("G9", CultureInfo.InvariantCulture);
    }
}
