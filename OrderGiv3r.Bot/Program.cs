using Microsoft.Extensions.Configuration;
using OrderGiv3r.Bot;
using OrderGiv3r.VideoDownloader;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using TL;
using WTelegram;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var appConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

// Bot
TelegramBotClient bot = new TelegramBotClient(appConfig["BotToken"]);
BotService botService = new BotService(bot);

var orderGiv3rConfig = new OrderGiv3rConfig(appConfig); 

// Telegram API
using Client ordergiverClient = new Client(orderGiv3rConfig.GetConfig);
WTelegram.Helpers.Log = (i, s1) => { }; // Filter logs a little bit
var user = await ordergiverClient.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

TdlibService tdlibService = new TdlibService(ordergiverClient, user);
var channelName = appConfig["ChannelName"];
var channel = await tdlibService.GetChatByNameAsync(channelName);
if (channel is null)
{
    throw new Exception($"Channel \"{channelName}\" does not exist.");
}

List<Message> messages = new List<Message>();
bool anyLeft = true;
while (anyLeft)
{
    var olderMessages = await tdlibService.GetMessagesFromChatAsync(channel, messages?.FirstOrDefault()?.Date ?? DateTime.UtcNow);
    if (olderMessages is null || !olderMessages.Any() || olderMessages.Count() == 1)
    {
        anyLeft = false;
    }
    else
    {
        messages?.InsertRange(0, olderMessages);
    }
}

var desktopLocation = @$"{appConfig["PathToDownload"]}Telegram Channels Backup\";
var newDirectory = $@"{channel.Title}";
var destination = desktopLocation + newDirectory;

var linksDestination = $@"{destination}\links.txt";
var videoNumbers = new List<int>();
await using (var linksStream = new FileStream(linksDestination, FileMode.Create))
{
    foreach (var message in messages)
    {
        if (message.message.Contains(appConfig["SiteName"]))
        {
            var link = Regex.Match(message.message, RegexCondition.Link).Value;
            var videoNumber = Regex.Match(link, RegexCondition.NumbersInTheEnd).Value;
            videoNumbers.Add(Convert.ToInt32(videoNumber));

            var videoNumberBytes = Encoding.UTF8.GetBytes(videoNumber);
            Console.WriteLine(videoNumber);
            await linksStream.WriteAsync(videoNumberBytes);
            var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
            await linksStream.WriteAsync(newLineBytes);
        }
        else if (message.media is MessageMediaDocument { document: Document document })
        {
            var videosDestination = @$"{destination}\Videos";
            Directory.CreateDirectory(videosDestination);

            int slash = document.mime_type.IndexOf('/'); // quick & dirty conversion from MIME type to file extension
            var fileName = slash > 0 ? $"{document.id}.{document.mime_type[(slash + 1)..]}" : $"{document.id}.bin";
            if (Directory.GetFiles(videosDestination, document.id + ".*").Length == 0)
            {
                Console.WriteLine("Downloading video" + fileName);
                await using var fileStream = File.Create(GeneratePathToDownload(videosDestination, fileName));
                var type = await ordergiverClient.DownloadFileAsync(document, fileStream);
                fileStream.Close();
                Console.WriteLine("Download of the video finished");
            }
        }
        else if (message.media is MessageMediaPhoto { photo: Photo photo })
        {
            var photosDestionation = @$"{destination}\Photos";
            Directory.CreateDirectory(photosDestionation);

            var fileName = $@"{photo.id}.jpg";
            if (Directory.GetFiles(photosDestionation, photo.id + ".*").Length == 0)
            {
                Console.WriteLine("Downloading photo" + fileName);
                await using var fs = File.Create(GeneratePathToDownload(photosDestionation, fileName));
                var type = await ordergiverClient.DownloadFileAsync(photo, fs);
                fs.Close();
                Console.WriteLine("Download oh the photo finished.");
                if (type is not Storage_FileType.unknown and not Storage_FileType.partial)
                    File.Move(GeneratePathToDownload(photosDestionation, fileName), GeneratePathToDownload(photosDestionation, photo.id.ToString(), type.ToString()), true); // rename extension
            }
        }
    }
}

var videoDownloader = new VideoDownloaderService();
foreach(var number in videoNumbers)
{
    var siteDownloadsDestination = @$"{destination}\Videos\{appConfig["SiteName"]}";
    Directory.CreateDirectory(siteDownloadsDestination);
    var downloadedVideosDestionation = @$"{siteDownloadsDestination}\{number}.{appConfig["VideoFormat"]}";
    if (Directory.GetFiles(siteDownloadsDestination, number + ".*").Length == 0)
    {
        Console.WriteLine($"Video {number} downloading started.");
        await videoDownloader.DownloadVideoAsync(appConfig["BaseUrl"] + number, downloadedVideosDestionation, appConfig["HtmlMatchCondition"], Convert.ToInt32(appConfig["RegexMatchedGroupId"]));
        Console.WriteLine($"Video {number} downloaded.");
    }
}

Console.ReadLine();

string GeneratePathToDownload(string destination, string fileName, string? fileType = null)
{
    return $@"{destination}\{fileName}" + (!string.IsNullOrEmpty(fileType) ? $".{fileType}" : "");
}