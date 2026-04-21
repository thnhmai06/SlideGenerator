using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SlideGenerator.Domain.Images.Rules;

public static class Fingerprint
{
    public static string Hash(params string[] parts)
    {
        var input = string.Join("|", parts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string Number(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string Number(float value)
    {
        return value.ToString("G9", CultureInfo.InvariantCulture);
    }
}