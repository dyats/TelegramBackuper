using System.Net;

namespace OrderGiv3r.VideoDownloader;

public static class FileExtensions
{
    public static async Task DownloadFileAsync(this HttpClient httpClient, Uri uri, string fileName)
    {
        await using var s = await httpClient.GetStreamAsync(uri);
        await using var fs = new FileStream(fileName, FileMode.CreateNew);
        await s.CopyToAsync(fs);
    }

    private static async Task DownloadFileWithWebClientAsync()
    {
        var downloadedPercents = 0;

        using (var client = new WebClient())
        {
            client.DownloadProgressChanged += ProgressChanged;
            client.DownloadFileAsync(new Uri(Url), PathToDownload);
        }

        void ProgressChanged(object sender, DownloadProgressChangedEventArgs eventArgs)
        {
            if (eventArgs.ProgressPercentage != downloadedPercents)
            {
                downloadedPercents = eventArgs.ProgressPercentage;
                Console.WriteLine(downloadedPercents);
            }
        }
    }
}