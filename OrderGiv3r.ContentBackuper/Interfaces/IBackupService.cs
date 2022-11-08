using TL;

namespace OrderGiv3r.ContentBackuper.Interfaces;

public interface IBackupService
{
    Task DownloadPhotoFromTgAsync(Photo photo);
    Task DownloadVideoFromTgAsync(Document document);
    Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup);
}
