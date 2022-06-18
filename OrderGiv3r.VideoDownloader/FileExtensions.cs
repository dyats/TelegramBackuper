using System.Net.Http.Handlers;

namespace OrderGiv3r.VideoDownloader;

public static class FileExtensions
{
    public static async Task DownloadFileAsync(this HttpClient httpClient, string url, string pathToDownload)
    {
        await using var s = await httpClient.GetStreamAsync(new Uri(url));
        await using var fs = new FileStream(pathToDownload, FileMode.Create);
        await s.CopyToAsync(fs);
    }
}