namespace HitNTry.PluginContracts;

public sealed record PluginExecutionResult(
    bool Succeeded,
    string PluginName,
    TimeSpan Duration,
    string? Payload = null,
    Exception? Error = null)
{
    public static PluginExecutionResult Success(string pluginName, TimeSpan duration, string? payload = null)
        => new(true, pluginName, duration, payload);

    public static PluginExecutionResult Failure(string pluginName, TimeSpan duration, Exception error, string? payload = null)
        => new(false, pluginName, duration, payload, error);
}

