using HtmlAgilityPack;
using OrderGiv3r.VideoDownloader.Interfaces;

namespace OrderGiv3r.VideoDownloader;

public class VideoDownloaderService : IVideoDownloaderService
{
    private readonly HtmlWeb _web;

    public VideoDownloaderService()
    {
        _web = new HtmlWeb();
    }

    public async Task DownloadVideoAsync(string url, string pathToDownload, string matchCondition, int matchedGroup)
    {
        var document = _web.Load(url);
        var downloadFromUrl = document.GetUrlForDownload(matchCondition, matchedGroup);
        var client = CreateHttpClient();
        await client.DownloadFileAsync(downloadFromUrl, pathToDownload);
    }
}
