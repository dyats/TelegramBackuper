using OrderGiv3r.Bot;
using System.Text;
using Telegram.Bot;
using WTelegram;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

// Bot
TelegramBotClient bot = new TelegramBotClient(BotToken);
BotService botService = new BotService(bot);



// Tdlib
using Client ordergiverClient = new Client(OrderGiv3rConfig);
var user = await ordergiverClient.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

TdlibService tdlibService = new TdlibService(ordergiverClient, user);
var fuckChannel = await tdlibService.GetChatByNameAsync("fuck");
var messages = await tdlibService.GetMessagesFromChatAsync(fuckChannel);

foreach (var message in messages)
{
    Console.WriteLine(message);
}

Console.ReadLine();