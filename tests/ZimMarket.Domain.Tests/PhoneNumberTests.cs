using ZimMarket.Domain.ValueObjects;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+263771234567")]
    [InlineData("+263 77 123 4567")]
    public void Create_accepts_valid_zimbabwe_international_numbers(string input)
    {
        var result = PhoneNumber.Create(input);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("+263771234567");
    }

    [Theory]
    [InlineData("+264771234567")]
    [InlineData("0771234567")]
    [InlineData("")]
    [InlineData("+26377123456")]
    public void Create_rejects_invalid_numbers(string input)
    {
        PhoneNumber.Create(input).IsFailure.Should().BeTrue();
    }
}
