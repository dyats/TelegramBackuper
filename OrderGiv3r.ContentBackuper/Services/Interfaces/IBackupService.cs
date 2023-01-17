using TL;

namespace OrderGiv3r.Application.Services.Interfaces;

public interface IBackupService
{
    Task DownloadFromTgAsync(MessageMedia media, CancellationToken cancellationToken = default);
    Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup, CancellationToken cancellationToken = default);
    Task DownloaFileFromTwitterAsync(long tweetId, CancellationToken cancellationToken = default);
}
