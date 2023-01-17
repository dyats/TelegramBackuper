using HtmlAgilityPack;
using MimeTypes;
using OrderGiv3r.Application.Extensions;
using OrderGiv3r.Application.Services.Interfaces;
using TL;
using Tweetinvi;
using WTelegram;
using static OrderGiv3r.Application.Extensions.HttpClientExtensions;
using Document = TL.Document;

namespace OrderGiv3r.Application.Services;

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

    public async Task DownloadFromTgAsync(MessageMedia media, CancellationToken cancellationToken = default)
    {
        if (media is MessageMediaPhoto { photo: Photo photo })
        {
            await DownloadPhotoFromTgAsync(photo);
        }
        else if (media is MessageMediaDocument { document: Document document })
        {
            var fileType = MimeTypeMap.GetExtension(document.mime_type);
            var fileName = $"{document.id}{fileType}";

            await DownloadDocumentFromTgAsync(document, fileName);
        }
    }

    public async Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup, CancellationToken cancellationToken = default)
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

        var requestResult = await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var contentLegnthToDownload = requestResult.Content.Headers.ContentLength!.Value;
        if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(downloadToPath, contentLegnthToDownload))
        {
            Console.WriteLine($"Video {videoNumber} downloading started.");
            await _httpClient.DownloadFileAsync(downloadFromUrl, downloadToPath, cancellationToken);
            Console.WriteLine($"Video {videoNumber} downloaded.");
        }
    }

    public async Task DownloaFileFromTwitterAsync(long tweetId, CancellationToken cancellationToken = default)
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
            var requestResult = await new HttpClient().GetAsync(new Uri(downloadFromUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

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
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath, cancellationToken);
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
                    await _httpClient.DownloadFileAsync(downloadFromUrl, finalPath, cancellationToken);
                    Console.WriteLine($"Video {tweetId}{index}.mp4 downloaded.");
                }
            }
        }
    }

    /// <summary>
    /// Downloads photos
    /// </summary>
    /// <param name="photo"></param>
    /// <returns></returns>
    private async Task DownloadPhotoFromTgAsync(Photo photo)
    {
        var telegramPhotosPath = GetPathAndDocumentType("telegram", "image/jpeg").path;

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

    /// <summary>
    /// Downloads documents including videos, GIFs and photos(if they saved without compression as file)
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task DownloadDocumentFromTgAsync(Document document, string fileName)
    {
        var directory = GetPathAndDocumentType("telegram", document.mime_type);

        if (!FileExtensions.IsFileAlreadyExistsAndFullyDownloaded(Path.Combine(directory.path, fileName), document.size))
        {
            Console.WriteLine($"Downloading {directory.documentType} {fileName}");
            var finalPath = Path.Combine(directory.path, fileName);
            await using var fileStream = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(document, fileStream, progress: progressCallback);
            fileStream.Close();
            Console.WriteLine($"Download of the {directory.documentType} finished");
        }
    }

    private void GenerateDirectoriesForFiles()
    {
        Directory.CreateDirectory(_photosPath); // For photos
        Directory.CreateDirectory(_videosPath); // For TG videos, GIFs etc.
        Directory.CreateDirectory(_videosSitePath); // For videos from sites
    }

    private (string path, string documentType) GetPathAndDocumentType(string folderName, string mimeType)
    {
        var path = "";
        var documentType = "";
        if (Constants.MimeTypes.Photos.Values.Contains(mimeType))
        {
            path = Path.Combine(_photosPath, folderName);
            documentType = "photo";
        }
        else if (Constants.MimeTypes.Videos.Values.Contains(mimeType))
        {
            path = Path.Combine(_videosPath, folderName);
            documentType = "video";
        }
        Directory.CreateDirectory(path);
        return (path, documentType);
    }
}
