namespace ZimMarket.Application.Common.Abstractions;

/// <summary>
/// Implemented by <see cref="ICommand"/> and <see cref="ICommand{T}"/> so pipeline behaviours can treat all commands uniformly (e.g. transactions).
/// </summary>
public interface ICommandMarker;
