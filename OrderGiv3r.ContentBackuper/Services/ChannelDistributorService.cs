using OrderGiv3r.Application.Services.Interfaces;
using TL;

namespace OrderGiv3r.Application.Services;

public class ChannelDistributorService : IChannelDistributorService
{
    private readonly ITdlibService _tdlibService;

    public ChannelDistributorService(ITdlibService tdlibService)
    {
        _tdlibService = tdlibService;
    }

    public async Task DistributeContentAsync(InputPeer channelId, string message, CancellationToken cancellationToken = default)
    {
        await _tdlibService.SendMessageToChannelAsync(channelId, message);
    }
}
