using HtmlAgilityPack;
using OrderGiv3r.ContentBackuper.Interfaces;
using Reddit.Controllers;
using Reddit;
using TL;
using Tweetinvi;
using WTelegram;
using static OrderGiv3r.ContentBackuper.HttpClientExtensions;
using Document = TL.Document;
using Newtonsoft.Json.Linq;

namespace OrderGiv3r.ContentBackuper;

public class BackupService : IBackupService
{
    private readonly Client _client;
    private readonly Client.ProgressCallback progressCallback;
    private readonly HtmlWeb _web;
    private readonly HttpClient _httpClient;

    private readonly TwitterClient _twitterClient;
    private readonly RedditClient _redditClient;

    private readonly string _photosPath;
    private readonly string _videosPath;
    private readonly string _videosSitePath;

    public BackupService(Client client, TwitterClient twitterClient, RedditClient redditClient, string generalPath, string baseAddress, string siteName)
    {
        _client = client;
        _web = new HtmlWeb();
        _httpClient = CreateHttpClient(baseAddress);

        _twitterClient = twitterClient;
        _redditClient = redditClient;

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

        var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength!.Value;
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

        foreach(var media in tweet.Media)
        {
            var downloadFromUrl = media.VideoDetails is null
                ? media.MediaURLHttps // photo url
                : media.VideoDetails.Variants.MaxBy(x => x.Bitrate)!.URL; // video url
            var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength!.Value;

            if (media.VideoDetails is null)
            {
                if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(twitterPhotosPath, tweetId + ".jpg"), contentLegnthToDownload))
                {
                    var finalPath = Path.Combine(twitterPhotosPath, tweetId + ".jpg");
                    Console.WriteLine($"Photo {tweetId}.jpg downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Photo {tweetId}.jpg downloaded.");
                }
                continue;
            }

            var file = media.VideoDetails.Variants.MaxBy(x => x.Bitrate);
            if (file is not null)
            {
                if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(twitterVideosPath, tweetId + ".mp4"), contentLegnthToDownload))
                {
                    var finalPath = Path.Combine(twitterVideosPath, tweetId + ".mp4");
                    Console.WriteLine($"Video {tweetId}.mp4 downloading started.");
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath);
                    Console.WriteLine($"Video {tweetId}.mp4 downloaded.");
                }
            }
        }
    }

    public async Task DownloaFileFromRedditAsync(string postFullName)
    {
        var redditPhotosPath = Path.Combine(_photosPath, "reddit");
        Directory.CreateDirectory(redditPhotosPath);

        var redditVideosPath = Path.Combine(_videosPath, "reddit");
        Directory.CreateDirectory(redditVideosPath);

        var redditPost = _redditClient.Post(postFullName).About() as LinkPost;

        if (redditPost is not null)
        {
            if (redditPost.Preview is null)
            {
                Console.WriteLine("There's no media file in this post OR we couldn't fetch them.");
                return;
            }

            redditPost.Preview.TryGetValue("reddit_video_preview", out var redditVideoPreviewObj);
            if (redditVideoPreviewObj is not null)
            {
                var fallbackUrl = redditVideoPreviewObj["fallback_url"]!.ToString();
                var contentLegnthToDownload = (await new HttpClient().GetAsync(new Uri(fallbackUrl), HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength!.Value;
                if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(redditVideosPath, postFullName + ".mp4"), contentLegnthToDownload))
                {
                    var finalPath = Path.Combine(redditVideosPath, postFullName + ".mp4");
                    Console.WriteLine($"Video {postFullName}.mp4 downloading started.");
                    await _httpClient.DownloadFileAsync(fallbackUrl, finalPath);
                    Console.WriteLine($"Video {postFullName}.mp4 downloaded.");
                }
                return;
            }

            // TODO - fetch URL media file in Media
            //redditPost.Listing.Media

            redditPost.Preview.TryGetValue("images", out var imagesObj);
            foreach(var item in imagesObj)
            {
                var isVideo = true;
                var variants = item.SelectToken("variants");
                JToken? file;
                file = variants?.SelectToken("mp4");
                if (file is null)
                {
                    file = variants?.SelectToken("gif");
                }
                if (file is null)
                {
                    file = item;
                    isVideo = false;
                }

                var sourcePhotoObj = file.SelectToken("source");
                if (sourcePhotoObj is not null)
                {
                    var extension = isVideo ? ".mp4" : ".jpg";
                    var sourceUrl = sourcePhotoObj["url"]!.ToString();
                    var result = await new HttpClient().GetAsync(new Uri(sourceUrl), HttpCompletionOption.ResponseHeadersRead);
                    var contentLegnthToDownload = result.Content.Headers.ContentLength!.Value;
                    if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(redditPhotosPath, postFullName + extension), contentLegnthToDownload))
                    {
                        var finalPath = Path.Combine(redditPhotosPath, postFullName + extension);
                        Console.WriteLine($"Photo {postFullName + extension} downloading started.");
                        await _httpClient.DownloadFileAsync(sourceUrl, finalPath);
                        Console.WriteLine($"Photo {postFullName + extension}.jpg downloaded.");
                    }
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
