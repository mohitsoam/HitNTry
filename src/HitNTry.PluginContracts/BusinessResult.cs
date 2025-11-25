namespace HitNTry.PluginContracts;

public sealed record BusinessResult(bool Success, string? Message = null, object? Data = null);
