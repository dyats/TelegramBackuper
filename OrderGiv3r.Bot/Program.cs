using Microsoft.Extensions.Configuration;
using OrderGiv3r.Bot;
using OrderGiv3r.ContentBackuper;
using OrderGiv3r.ContentBackuper.Interfaces;
using Reddit;
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

// 3rd party site
string baseUrl = appConfig["baseUrl"]!;
string htmlMatchCondition = appConfig["HtmlMatchCondition"]!;
int regexMatchGroup = Convert.ToInt32(appConfig["RegexMatchedGroupId"])!;

// telegram
string botToken = appConfig["BotToken"]!;
string channelName = appConfig["ChannelName"]!;
string siteName = appConfig["SiteName"]!;

// twitter
string twitterApiKey = appConfig["Twitter:ApiKey"]!;
string twitterApiSecret = appConfig["Twitter:ApiSecret"]!;
string twitterBearerToken = appConfig["Twitter:BearerToken"]!;

// reddit
string redditAppId = appConfig["Reddit:AppId"]!;
string redditAppSecret = appConfig["Reddit:AppSecret"]!;

// Bot
TelegramBotClient bot = new TelegramBotClient(botToken);
BotService botService = new BotService(bot);

var orderGiv3rConfig = new OrderGiv3rConfig(appConfig);

// Telegram API
using Client ordergiverClient = new Client(orderGiv3rConfig.GetConfig);
WTelegram.Helpers.Log = (i, s1) => { }; // Filter logs a little bit
var user = await ordergiverClient.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

ITdlibService tdlibService = new TdlibService(ordergiverClient, user);

var channel = await tdlibService.GetChatByNameAsync(channelName);
if (channel is null)
{
    throw new Exception($"Channel \"{channelName}\" does not exist.");
}

// Twitter Api
var twitterClient = new TwitterClient(twitterApiKey, twitterApiSecret, twitterBearerToken);

// Reddit Api
var tokens = RedditTokenRetrieval.AuthorizeUser(redditAppId, redditAppSecret);

var redditClient = new RedditClient(redditAppId, tokens.refreshToken, redditAppSecret, tokens.accessToken);
Console.WriteLine("Username: " + redditClient.Account.Me.Name);
Console.WriteLine("Cake Day: " + redditClient.Account.Me.Created.ToString("D"));

var pathToDownload = @$"{appConfig["PathToDownload"]}";
var newDirectory = $@"Channel - [{channel.Title}]";
var destination = pathToDownload + newDirectory;
IBackupService backupService = new BackupService(ordergiverClient, twitterClient, redditClient, destination, baseUrl, siteName);

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
var redditLinks = new List<string>();

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
    else if (message.message.Contains("reddit"))
    {
        var link = Regex.Match(message.message, RegexCondition.Link).Value;
        redditLinks.Add(link);
    }
    else if (message.media is MessageMediaDocument { document: Document document })
    {
        await backupService.DownloadVideoFromTgAsync(document);
    }
    else if (message.media is MessageMediaPhoto { photo: Photo photo })
    {
        await backupService.DownloadPhotoFromTgAsync(photo);
    }
}
foreach (var link in redditLinks)
{
    // Get the ID from the permalink, then preface it with "t3_" to convert it to a Reddit fullname.  --Kris
    Match match = Regex.Match(link, @"\/comments\/([a-z0-9]+)\/");

    string postFullname = "t3_" + (match != null && match.Groups != null && match.Groups.Count >= 2
        ? match.Groups[1].Value
        : "");
    if (postFullname.Equals("t3_"))
    {
        throw new Exception("Unable to extract ID from permalink.");
    }

    await backupService.DownloaFileFromRedditAsync(postFullname);
}
foreach (var link in twitterLinks)
{
    var tweetId = Regex.Match(link, RegexCondition.Twitter.TweetId).Groups[2].Value;
    await backupService.DownloaFileFromTwitterAsync(Convert.ToInt64(tweetId));
}



foreach (var number in videoNumbers)
{
    await backupService.DownloadVideoFromSiteAsync(number, baseUrl, htmlMatchCondition, regexMatchGroup);
}

Console.ReadLine();