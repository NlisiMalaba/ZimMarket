namespace ZimMarket.Application.Common.Models;

public sealed record ValidationError(string Field, string Message);
