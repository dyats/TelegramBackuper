using TL;

namespace OrderGiv3r.ContentBackuper.Interfaces;

public interface IBackupService
{
    Task DownloadPhotoFromTgAsync(Photo photo);
    Task DownloadVideoFromTgAsync(Document document);
    Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup);
    Task DownloaFileFromTwitterAsync(long tweetId);
    /// <summary>
    /// Gallery of images is not supported by Reddit.NET
    /// </summary>
    /// <param name="postFullName"></param>
    /// <returns></returns>
    Task DownloaFileFromRedditAsync(string postFullName);
}
