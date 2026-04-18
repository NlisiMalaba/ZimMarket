using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Auth;

public sealed record SubmitSellerKycCommand(string NationalIdKey, string ProofOfResidenceKey) : ICommand;
