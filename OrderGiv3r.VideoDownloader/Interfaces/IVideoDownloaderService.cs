namespace OrderGiv3r.VideoDownloader.Interfaces;

public interface IVideoDownloaderService
{
    Task DownloadVideoAsync(string downloadFromUrl, string pathToDownload);
}
