using HtmlAgilityPack;
using OrderGiv3r.ContentBackuper.Interfaces;
using System.Text.RegularExpressions;
using TL;
using Tweetinvi;
using WTelegram;
using static OrderGiv3r.ContentBackuper.HttpClientExtensions;
using Document = TL.Document;

namespace OrderGiv3r.ContentBackuper;

public class BackupService : IBackupService
{
    private readonly Client _client;
    private readonly Client.ProgressCallback progressCallback;
    private readonly HtmlWeb _web;
    private readonly HttpClient _httpClient;

    private readonly TwitterClient _twitterClient;

    private readonly string _photosPath;
    private readonly string _videosPath;
    private readonly string _videosSitePath;

    public BackupService(Client client, TwitterClient twitterClient, string generalPath, string baseAddress, string siteName)
    {
        _client = client;
        _web = new HtmlWeb();
        _httpClient = CreateHttpClient(baseAddress);

        _twitterClient = twitterClient;

        progressCallback = new Client.ProgressCallback((p, r) =>
        {
            Console.Write(p * 100 / r + "%\r");
        });

        _photosPath = generalPath + @"\Photos";
        _videosPath = generalPath + @"\Videos";
        _videosSitePath = Path.Combine(_videosPath, siteName);
        GenerateDirectoriesForFiles();
    }

    public async Task DownloadPhotoFromTgAsync(Photo photo)
    {
        var telegramPhotosPath = Path.Combine(_photosPath, "telegram");
        Directory.CreateDirectory(telegramPhotosPath); // For photos from telegram

        var fileName = $@"{photo.id}.jpeg";
        if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(telegramPhotosPath, fileName), photo.LargestPhotoSize.FileSize))
        {
            Console.WriteLine("Downloading photo " + fileName);
            var finalPath = Path.Combine(telegramPhotosPath, fileName);
            await using var fs = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(photo, fs, progress: progressCallback);
            fs.Close();
            Console.WriteLine("Download of the photo finished.");
            if (type is not Storage_FileType.unknown and not Storage_FileType.partial)
                File.Move(finalPath, Path.Combine(telegramPhotosPath, $@"{photo.id}.{type}")); // rename extension
        }
    }

    public async Task DownloadVideoFromTgAsync(Document document)
    {
        var telegramVideosPath = Path.Combine(_videosPath, "telegram");
        Directory.CreateDirectory(telegramVideosPath); // For videos, GIFs from telegram

        int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from MIME type to file extension
        var fileName = slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin";

        if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(telegramVideosPath, fileName), document.size))
        {
            Console.WriteLine("Downloading video " + fileName);
            var finalPath = Path.Combine(telegramVideosPath, fileName);
            await using var fileStream = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(document, fileStream, progress: progressCallback);
            fileStream.Close();
            Console.WriteLine("Download of the video finished");
        }
    }

    public async Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup)
    {
        var downloadToPath = Path.Combine(_videosSitePath, $@"{videoNumber}.mp4");
        var url = baseUrl + videoNumber;
        var document = _web.Load(url);
        var downloadFromUrl = document.GetUrlForDownload(htmlMatchCondition, regexMatchGroup);

        // most likely that video was deleted
        if (string.IsNullOrEmpty(downloadFromUrl))
        {
            Console.WriteLine($"Video {videoNumber} does not exist anymore.");
            return;
        }

        var requestResult = await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead);
        var contentLegnthToDownload = requestResult.Content.Headers.ContentLength!.Value;
        if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(downloadToPath, contentLegnthToDownload))
        {
            Console.WriteLine($"Video {videoNumber} downloading started.");
            await _httpClient.DownloadFileAsync(downloadFromUrl, downloadToPath);
            Console.WriteLine($"Video {videoNumber} downloaded.");
        }
    }

    public async Task DownloaFileFromTwitterAsync(long tweetId)
    {
        var twitterPhotosPath = Path.Combine(_photosPath, "twitter");
        Directory.CreateDirectory(twitterPhotosPath);

        var twitterVideosPath = Path.Combine(_videosPath, "twitter");
        Directory.CreateDirectory(twitterVideosPath);

        var tweet = await _twitterClient.Tweets.GetTweetAsync(tweetId);

        foreach (var media in tweet.Media.Select((value, i) => new { i, value }))
        {
            var index = tweet.Media.Count() > 1
                ? $"({media.i + 1})" // start from 1 not from 0
                : string.Empty;

            var downloadFromUrl = media.value.VideoDetails is null
                ? media.value.MediaURLHttps // photo url
                : media.value.VideoDetails.Variants.MaxBy(x => x.Bitrate)!.URL; // video url
            var requestResult = await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead);

            if (!requestResult.Content.Headers.ContentLength.HasValue)
            {
                Console.WriteLine($"Content Length is not specified. Check tweet {tweetId} manually.");
                return;
            }

            var contentLegnthToDownload = requestResult.Content.Headers.ContentLength!.Value;

            if (media.value.VideoDetails is null)
            {
                if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(twitterPhotosPath, tweetId + index + ".jpg"), contentLegnthToDownload))
                {
                    var finalPath = Path.Combine(twitterPhotosPath, tweetId + index + ".jpg");
                    Console.WriteLine($"Photo {tweetId}{index}.jpg downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Photo {tweetId}{index}.jpg downloaded.");
                }
                continue;
            }

            var file = media.value.VideoDetails.Variants.MaxBy(x => x.Bitrate);
            if (file is not null)
            {
                if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(twitterVideosPath, tweetId + index + ".mp4"), contentLegnthToDownload))
                {
                    var finalPath = Path.Combine(twitterVideosPath, tweetId + index + ".mp4");
                    Console.WriteLine($"Video {tweetId}{index}.mp4 downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Video {tweetId}{index}.mp4 downloaded.");
                }
            }
        }
    }

    private void GenerateDirectoriesForFiles()
    {
        Directory.CreateDirectory(_photosPath); // For photos
        Directory.CreateDirectory(_videosPath); // For TG videos, GIFs etc.
        Directory.CreateDirectory(_videosSitePath); // For videos from sites
    }
}
