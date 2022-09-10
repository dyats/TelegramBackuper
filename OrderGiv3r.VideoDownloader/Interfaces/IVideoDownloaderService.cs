namespace OrderGiv3r.VideoDownloader.Interfaces;

public interface IVideoDownloaderService
{
    Task DownloadVideoAsync(string url, string pathToDownload, string matchCondition, int matchedGroup);
}
