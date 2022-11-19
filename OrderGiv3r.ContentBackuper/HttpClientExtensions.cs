using System.Net.Http.Handlers;

namespace OrderGiv3r.ContentBackuper;

public static class HttpClientExtensions
{
    public static HttpClient CreateHttpClient(string baseAddress)
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
                Console.Write($"Downloaded: {currentPercentage}%\r");
            }
        };

        var httpClient = new HttpClient(ph);
        httpClient.BaseAddress = new Uri(baseAddress);

        return httpClient;
    }

    public static async Task DownloadFileAsync(this HttpClient httpClient, string url, string pathToDownload)
    {
        await using var s = await httpClient.GetStreamAsync(new Uri(url));
        await using var fs = new FileStream(pathToDownload, FileMode.Create);
        await s.CopyToAsync(fs);
    }
}