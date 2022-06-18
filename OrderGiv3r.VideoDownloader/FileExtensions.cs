using System.Net.Http.Handlers;

namespace OrderGiv3r.VideoDownloader;

public static class FileExtensions
{
    public static async Task DownloadFileAsync(string url, string pathToDownload)
    {
        var handler = new HttpClientHandler() { AllowAutoRedirect = true };
        var ph = new ProgressMessageHandler(handler);
        
        decimal? percentageProgress = 0;
        ph.HttpReceiveProgress += (_, args) =>
        {
            var downloadedProgress = (double)args.BytesTransferred / args.TotalBytes * 100;
            var currentPercentage = Math.Round(Convert.ToDecimal(downloadedProgress), 0);

            if (percentageProgress.Value != currentPercentage)
            {
                percentageProgress = currentPercentage;
                Console.WriteLine($"Downloaded: {currentPercentage}%");
            }
        };

        var httpClient = new HttpClient(ph);
        
        await using var s = await httpClient.GetStreamAsync(new Uri(url));
        await using var fs = new FileStream(pathToDownload, FileMode.Create);
        await s.CopyToAsync(fs);
    }
}