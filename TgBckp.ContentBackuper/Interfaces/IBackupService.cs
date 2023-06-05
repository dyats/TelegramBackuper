using TL;

namespace TgBckp.ContentBackuper.Interfaces;

public interface IBackupService
{
    Task DownloadFromTgAsync(MessageMedia media);
    Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup);
    Task DownloaFileFromTwitterAsync(long tweetId);
}
