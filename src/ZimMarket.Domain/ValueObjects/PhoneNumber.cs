using System.Text.RegularExpressions;
using ZimMarket.Shared;

namespace ZimMarket.Domain.ValueObjects;

public sealed partial class PhoneNumber
{
    private static readonly Regex ZimInternational = ZimInternationalRegex();

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PhoneNumber>.Failure("Phone number is required.");

        var normalized = Whitespace().Replace(value.Trim(), "");

        if (!ZimInternational.IsMatch(normalized))
            return Result<PhoneNumber>.Failure(
                "Phone number must be a valid Zimbabwe international number (e.g. +2637XXXXXXXX).");

        return Result<PhoneNumber>.Success(new PhoneNumber(normalized));
    }

    [GeneratedRegex(@"^\+263[1-9]\d{8}$")]
    private static partial Regex ZimInternationalRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
