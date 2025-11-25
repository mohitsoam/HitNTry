using HitNTry.PluginContracts;
using HitNTry.Framework.Abstractions;
using Microsoft.Extensions.Logging;

namespace HitNTry.Framework;

public sealed class HitNTryBusinessService : HitNTry.PluginContracts.IHitNTryBusinessService
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<HitNTryBusinessService> _logger;

    public HitNTryBusinessService(IPluginManager pluginManager, ILogger<HitNTryBusinessService> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;
    }

    public async Task<BusinessResult> ExecuteAsync(BusinessRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.PluginId))
        {
            try
            {
                _logger.LogInformation("Invoking plugin {Plugin} for business action {Action}", request.PluginId, request.Action);
                var execRequest = new PluginExecutionRequest(
                    CorrelationId: null,
                    Tags: null,
                    Version: null,
                    Properties: request.Properties);

                var result = await _pluginManager.ExecuteAsync(request.PluginId, execRequest, cancellationToken);
                return new BusinessResult(result.Succeeded, result.Error?.Message ?? "Plugin executed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {Plugin} invocation failed", request.PluginId);
                return new BusinessResult(false, ex.Message);
            }
        }

        // Default framework business logic.
        _logger.LogInformation("Executing default framework business logic for action {Action}", request.Action);
        await Task.Delay(10, cancellationToken); // placeholder for real logic
        return new BusinessResult(true, "Default framework handler executed");
    }
}
