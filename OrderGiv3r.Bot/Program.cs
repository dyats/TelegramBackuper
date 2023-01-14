using Microsoft.Extensions.Configuration;
using OrderGiv3r.Bot;
using OrderGiv3r.ContentBackuper;
using OrderGiv3r.ContentBackuper.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using TL;
using Tweetinvi;
using WTelegram;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var appConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

string baseUrl = appConfig["baseUrl"]!;
string htmlMatchCondition = appConfig["HtmlMatchCondition"]!;
int regexMatchGroup = Convert.ToInt32(appConfig["RegexMatchedGroupId"])!;

string botToken = appConfig["BotToken"]!;
string channelName = appConfig["ChannelName"]!;
string siteName = appConfig["SiteName"]!;

string twitterApiKey = appConfig["Twitter:ApiKey"]!;
string twitterApiSecret = appConfig["Twitter:ApiSecret"]!;
string twitterBearerToken = appConfig["Twitter:BearerToken"]!;

// Bot
TelegramBotClient bot = new TelegramBotClient(botToken);
BotService botService = new BotService(bot);

var orderGiv3rConfig = new OrderGiv3rConfig(appConfig);

// Telegram API
using Client ordergiverClient = new Client(orderGiv3rConfig.GetConfig);
WTelegram.Helpers.Log = (i, s1) => { }; // Filter logs a little bit
var user = await ordergiverClient.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

ITdlibService tdlibService = new TdlibService(ordergiverClient);

var channel = await tdlibService.GetChatByNameAsync(channelName);
if (channel is null)
{
    throw new Exception($"Channel \"{channelName}\" does not exist.");
}

// Twitter Api
var twitterClient = new TwitterClient(twitterApiKey, twitterApiSecret, twitterBearerToken);

var pathToDownload = @$"{appConfig["PathToDownload"]}";
var newDirectory = $@"Channel - [{channel.Title}]";
var destination = pathToDownload + newDirectory;
IBackupService backupService = new BackupService(ordergiverClient, twitterClient, destination, baseUrl, siteName);

List<Message> messages = new List<Message>();
bool anyLeft = true;
while (anyLeft)
{
    var olderMessages = await tdlibService.GetMessagesFromChatAsync(channel, messages.FirstOrDefault()?.Date.AddMilliseconds(-1) ?? DateTime.UtcNow);
    if (olderMessages is null || !olderMessages.Any() || olderMessages.Count() == 1)
    {
        anyLeft = false;
        continue;
    }

    messages.InsertRange(0, olderMessages);
}

var linksDestination = $@"{destination}\links.txt";
var videoNumbers = new List<int>();
var twitterLinks = new List<string>();

Console.WriteLine("[TELEGRAM]");
foreach (var message in messages)
{
    if (message.message.Contains(siteName))
    {
        var link = Regex.Match(message.message, RegexCondition.Link).Value;
        var videoNumber = Regex.Match(link, RegexCondition.NumbersInTheEnd).Value;
        videoNumbers.Add(Convert.ToInt32(videoNumber));
    }
    else if (message.message.Contains("twitter"))
    {
        var link = Regex.Match(message.message, RegexCondition.Link).Value;
        twitterLinks.Add(link);
    }
    else if (message.media is MessageMedia)
    {
        await backupService.DownloadFromTgAsync(message.media);
    }
}

Console.WriteLine("[TWITTER]");
foreach (var link in twitterLinks)
{
    var tweetId = Regex.Match(link, RegexCondition.Twitter.TweetId).Groups[2].Value;
    await backupService.DownloaFileFromTwitterAsync(Convert.ToInt64(tweetId));
}

Console.WriteLine($"[{siteName.ToUpper()}]");
foreach (var number in videoNumbers)
{
    await backupService.DownloadVideoFromSiteAsync(number, baseUrl, htmlMatchCondition, regexMatchGroup);
}

Console.ReadLine();