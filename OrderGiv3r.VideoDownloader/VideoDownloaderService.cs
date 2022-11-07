using HtmlAgilityPack;
using OrderGiv3r.VideoDownloader.Interfaces;

namespace OrderGiv3r.VideoDownloader;

public class VideoDownloaderService : IVideoDownloaderService
{
    public async Task DownloadVideoAsync(string downloadFromUrl, string pathToDownload)
    {
        var client = CreateHttpClient();
        
        await client.DownloadFileAsync(downloadFromUrl, pathToDownload);
    }
}
