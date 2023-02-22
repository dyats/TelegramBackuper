using TL;

namespace OrderGiv3r.Application.Services.Interfaces;

public interface IChannelDistributorService
{
    Task DistributeContentAsync(InputPeer channelId, string message, CancellationToken cancellationToken = default);
}
