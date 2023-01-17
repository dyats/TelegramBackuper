namespace OrderGiv3r.Application.Services.Interfaces;

public interface IChannelDistributorService
{
    Task DistributeContent(CancellationToken cancellationToken = default);
}
