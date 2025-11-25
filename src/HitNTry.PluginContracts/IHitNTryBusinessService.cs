using System.Threading;

namespace HitNTry.PluginContracts;

public interface IHitNTryBusinessService
{
    Task<BusinessResult> ExecuteAsync(BusinessRequest request, CancellationToken cancellationToken = default);
}
