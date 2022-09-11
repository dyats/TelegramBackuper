﻿using OrderGiv3r.ContentBackuper.Interfaces;
using OrderGiv3r.VideoDownloader;
using OrderGiv3r.VideoDownloader.Interfaces;
using TL;
using WTelegram;

namespace OrderGiv3r.ContentBackuper;

public class BackupService : IBackupService
{
    private readonly Client _client;
    private readonly ITdlibService _tdlibService;
    private readonly IVideoDownloaderService _videoDownloaderService;

    private readonly string _photosPath;
    private readonly string _videosPath;
    private readonly string _videosSitePath;

    public BackupService(Client client, ITdlibService tdlibService, string generalPath, string siteName)
    {
        _client = client;
        _tdlibService = tdlibService;
        _videoDownloaderService = new VideoDownloaderService();
        _photosPath = generalPath + @"\Photos";
        _videosPath = generalPath + @"\Videos";
        _videosSitePath = Path.Combine(_videosPath, siteName);
        GenerateDirectoriesForFiles();
    }

    public async Task DownloadPhotoFromTgAsync(Photo photo)
    {
        var fileName = $@"{photo.id}.jpg";
        if (Directory.GetFiles(_photosPath, photo.id + ".*").Length == 0)
        {
            Console.WriteLine("Downloading photo" + fileName);
            var finalPath = Path.Combine(_photosPath, fileName);
            await using var fs = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(photo, fs);
            fs.Close();
            Console.WriteLine("Download oh the photo finished.");
            if (type is not Storage_FileType.unknown and not Storage_FileType.partial)
                File.Move(finalPath, Path.Combine(_photosPath, $@"{photo.id}.{type}")); // rename extension
        }
    }

    public async Task DownloadVideoFromTgAsync(Document document)
    {
        int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from MIME type to file extension
        var fileName = slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin";
        if (Directory.GetFiles(_videosPath, document.id + ".*").Length == 0)
        {
            Console.WriteLine("Downloading video" + fileName);
            var finalPath = Path.Combine(_photosPath, fileName);
            await using var fileStream = File.Create(finalPath);
            var type = await _client.DownloadFileAsync(document, fileStream);
            fileStream.Close();
            Console.WriteLine("Download of the video finished");
        }
    }

    public async Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup)
    {
        var finalPath = Path.Combine(_videosSitePath, $@"{videoNumber}.mp4");
        if (Directory.GetFiles(_videosSitePath, videoNumber + ".*").Length == 0)
        {
            Console.WriteLine($"Video {videoNumber} downloading started.");
            await _videoDownloaderService.DownloadVideoAsync(baseUrl + videoNumber, finalPath, htmlMatchCondition, regexMatchGroup);
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
