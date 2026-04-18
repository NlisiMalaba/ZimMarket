using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ZimMarket.Infrastructure.Payments;

/// <summary>
/// Paynow Zimbabwe hash rules: SHA512 (uppercase hex) over UTF-8 bytes of concatenated values + integration key.
/// </summary>
internal static class PaynowProtocol
{
    public static List<KeyValuePair<string, string>> ParseForm(string payload)
    {
        var list = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(payload))
            return list;

        foreach (string segment in payload.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = segment.IndexOf('=');
            string key = eq >= 0 ? segment[..eq] : segment;
            string value = eq >= 0 ? segment[(eq + 1)..] : string.Empty;
            list.Add(new KeyValuePair<string, string>(key, value));
        }

        return list;
    }

    /// <summary>Outbound: concatenate raw field values in order (exclude hash), then append integration key.</summary>
    public static string ComputeOutboundHash(IReadOnlyList<KeyValuePair<string, string>> fieldsExcludingHash, string integrationKey)
    {
        var concat = new StringBuilder();
        foreach (KeyValuePair<string, string> pair in fieldsExcludingHash)
        {
            if (string.Equals(pair.Key, "hash", StringComparison.OrdinalIgnoreCase))
                continue;

            concat.Append(pair.Value);
        }

        concat.Append(integrationKey);
        return Sha512UpperHex(concat.ToString());
    }

    /// <summary>Inbound: URL-decode each value (except hash key omitted entirely), concatenate in field order, append key, SHA512.</summary>
    public static string ComputeInboundHash(IReadOnlyList<KeyValuePair<string, string>> pairs, string integrationKey)
    {
        var concat = new StringBuilder();
        foreach (KeyValuePair<string, string> pair in pairs)
        {
            if (string.Equals(pair.Key, "hash", StringComparison.OrdinalIgnoreCase))
                continue;

            concat.Append(WebUtility.UrlDecode(pair.Value));
        }

        concat.Append(integrationKey);
        return Sha512UpperHex(concat.ToString());
    }

    public static bool ConstantTimeEqualsHex(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a.Length != b.Length)
            return false;

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(NormalizeHex(a)),
                Convert.FromHexString(NormalizeHex(b)));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string Sha512UpperHex(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        byte[] hash = SHA512.HashData(utf8);
        return Convert.ToHexString(hash);
    }

    private static string NormalizeHex(string hex) => hex.Trim().ToUpperInvariant();
}
