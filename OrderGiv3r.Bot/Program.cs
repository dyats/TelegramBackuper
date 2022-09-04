using Microsoft.Extensions.Configuration;
using OrderGiv3r.Bot;
using System.Text;
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
var user = await ordergiverClient.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

TdlibService tdlibService = new TdlibService(ordergiverClient, user);
var channelName = appConfig["ChannelName"];
var channel = await tdlibService.GetChatByNameAsync(channelName);
if (channel is null)
{
    throw new Exception($"Channel \"{channelName}\" does not exist.");
}
var messages = await tdlibService.GetMessagesFromChatAsync(channel);

var desktopLocation = @$"{appConfig["PathToDownload"]}Telegram Channels Backup\";
var newDirectory = $@"{channel.Title}";
var destination = desktopLocation + newDirectory;

foreach (var message in messages)
{
    if (message.media is MessageMediaDocument { document: Document document })
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

Console.ReadLine();

string GeneratePathToDownload(string destination, string fileName, string? fileType = null)
{
    return $@"{destination}\{fileName}" + (!string.IsNullOrEmpty(fileType) ? $".{fileType}" : "");
}