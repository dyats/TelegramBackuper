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

        var fileName = $@"{photo.id}.jpg";
        var existingFiles = Directory.GetFiles(telegramPhotosPath, photo.id + ".*");
        var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != photo.LargestPhotoSize.FileSize); // if file exists but not downloaded for 100%, let's download it again
        
        if (existingFiles.Count() == 0 || existingNotFinishedFiles.Any())
        {
            if (existingNotFinishedFiles.Any())
            {
                Console.WriteLine($"Overwriting file {fileName}");
            }

            Console.WriteLine("Downloading photo" + fileName);
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
        var existingFiles = Directory.GetFiles(telegramVideosPath, document.id + ".*");
        var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != document.size); // if file exists but not downloaded for 100%, let's download it again

        if (existingFiles.Count() == 0 || existingNotFinishedFiles.Any())
        {
            if (existingNotFinishedFiles.Any())
            {
                Console.WriteLine($"Overwriting file {fileName}");
            }

            Console.WriteLine("Downloading video" + fileName);
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
        var existingFiles = Directory.GetFiles(_videosSitePath, videoNumber + ".*");
        var url = baseUrl + videoNumber;
        var document = _web.Load(url);
        var downloadFromUrl = document.GetUrlForDownload(htmlMatchCondition, regexMatchGroup);

        var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength;

        // if file exists but not downloaded for 100%, let's download it again
        var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != contentLegnthToDownload);
        if (existingFiles.Length == 0 || existingNotFinishedFiles.Any())
        {
            if (existingNotFinishedFiles.Any())
            {
                Console.WriteLine($"Overwriting file {videoNumber}.mp4");
            }

            Console.WriteLine($"Video {videoNumber} downloading started.");
            await _httpClient.DownloadFileAsync(downloadFromUrl, downloadToPath);
            Console.WriteLine($"Video {videoNumber} downloaded.");
        }
    }

    public async Task DownloadVideoFromTwitterAsync(string link)
    {
        var twitterVideosPath = Path.Combine(_videosPath, "twitter");
        Directory.CreateDirectory(twitterVideosPath);

        var tweetId = Regex.Match(link, "(^.*)/(\\d*)").Groups[2].Value;

        var tweet = await _twitterClient.Tweets.GetTweetAsync(Convert.ToInt64(tweetId));

        foreach(var media in tweet.Media)
        {
            var existingFiles = Directory.GetFiles(twitterVideosPath, tweetId + ".*");

            if (media.VideoDetails is null)
            {
                var downloadFromUrl = media.MediaURLHttps;
                var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength;
                var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != contentLegnthToDownload);
                if (existingFiles.Length == 0 || existingNotFinishedFiles.Any())
                {
                    if (existingNotFinishedFiles.Any())
                    {
                        Console.WriteLine($"Overwriting file {tweetId}.jpg");
                    }

                    var finalPath = Path.Combine(twitterVideosPath, tweetId + ".jpg");
                    Console.WriteLine($"Photo {tweetId}.jpg downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Photo {tweetId}.jpg downloaded.");
                }
                continue;
            }

            var file = media.VideoDetails.Variants.MaxBy(x => x.Bitrate);
            if (file is not null)
            {
                var downloadFromUrl = file.URL; var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength;
                var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != contentLegnthToDownload);
                if (existingFiles.Length == 0 || existingNotFinishedFiles.Any())
                {
                    if (existingNotFinishedFiles.Any())
                    {
                        Console.WriteLine($"Overwriting file {tweetId}.jpg");
                    }

                    var finalPath = Path.Combine(twitterVideosPath, tweetId + ".mp4");
                    Console.WriteLine($"Video {tweetId}.mp4 downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Video {tweetId}.mp4 downloaded.");
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
