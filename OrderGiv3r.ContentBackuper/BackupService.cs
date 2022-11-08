using HtmlAgilityPack;
using OrderGiv3r.ContentBackuper.Interfaces;
using OrderGiv3r.VideoDownloader;
using OrderGiv3r.VideoDownloader.Interfaces;
using TL;
using WTelegram;

namespace OrderGiv3r.ContentBackuper;

public class BackupService : IBackupService
{
    private readonly Client _client;
    private readonly IVideoDownloaderService _videoDownloaderService;
    private readonly Client.ProgressCallback progressCallback;
    private readonly HtmlWeb _web;

    private readonly string _photosPath;
    private readonly string _videosPath;
    private readonly string _videosSitePath;

    public BackupService(Client client, string generalPath, string siteName)
    {
        _client = client;
        _videoDownloaderService = new VideoDownloaderService();
        _web = new HtmlWeb();

        progressCallback = new Client.ProgressCallback((p, r) => {
            Console.Write(p * 100 / r + "%\r");
        });

        _photosPath = generalPath + @"\Photos";
        _videosPath = generalPath + @"\Videos";
        _videosSitePath = Path.Combine(_videosPath, siteName);
        GenerateDirectoriesForFiles();
    }

    public async Task DownloadPhotoFromTgAsync(Photo photo)
    {
        var fileName = $@"{photo.id}.jpg";
        var existingFiles = Directory.GetFiles(_photosPath, photo.id + ".*");
        var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != photo.LargestPhotoSize.FileSize); // if file exists but not downloaded for 100%, let's download it again
        if (existingNotFinishedFiles.Any())
        {
            Console.WriteLine($"Overwriting file {fileName}");
        }
        if (existingFiles.Count() == 0 || existingNotFinishedFiles.Any())
        {
            Console.WriteLine("Downloading photo" + fileName);
            var finalPath = Path.Combine(_photosPath, fileName);
            await using var fs = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(photo, fs, progress: progressCallback);
            fs.Close();
            Console.WriteLine("Download of the photo finished.");
            if (type is not Storage_FileType.unknown and not Storage_FileType.partial)
                File.Move(finalPath, Path.Combine(_photosPath, $@"{photo.id}.{type}")); // rename extension
        }
    }

    public async Task DownloadVideoFromTgAsync(Document document)
    {
        int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from MIME type to file extension
        var fileName = slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin";
        var existingFiles = Directory.GetFiles(_videosPath, document.id + ".*");
        var existingNotFinishedFiles = existingFiles.Where(x => new FileInfo(x).Length != document.size); // if file exists but not downloaded for 100%, let's download it again
        if (existingNotFinishedFiles.Any())
        {
            Console.WriteLine($"Overwriting file {fileName}");
        }
        if (existingFiles.Count() == 0 || existingNotFinishedFiles.Any())
        {
            Console.WriteLine("Downloading video" + fileName);
            var finalPath = Path.Combine(_videosPath, fileName);
            await using var fileStream = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(document, fileStream, progress: progressCallback);
            fileStream.Close();
            Console.WriteLine("Download of the video finished");
        }
    }

    public async Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup)
    {
        var finalPath = Path.Combine(_videosSitePath, $@"{videoNumber}.mp4");
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
            await _videoDownloaderService.DownloadVideoAsync(downloadFromUrl, finalPath);
            Console.WriteLine($"Video {videoNumber} downloaded.");
        }
    }
    
    private void GenerateDirectoriesForFiles()
    {
        Directory.CreateDirectory(_photosPath); // For photos
        Directory.CreateDirectory(_videosPath); // For TG videos, GIFs etc.
        Directory.CreateDirectory(_videosSitePath); // For videos from sites
    }
}
