using HitNTry.PluginContracts;

namespace HitNTry.Framework;

public interface IHitNTryBusinessService
{
    /// <summary>
    /// Execute a business request. If <see cref="BusinessRequest.PluginId"/> is provided the
    /// request will be forwarded to that plugin, otherwise the default framework implementation runs.
    /// </summary>
    Task<BusinessResult> ExecuteAsync(BusinessRequest request, CancellationToken cancellationToken = default);
}
