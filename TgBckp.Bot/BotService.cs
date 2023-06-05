using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgBckp.Bot;

public class BotService
{
    private TelegramBotClient _bot { get; set; }

    private ReceiverOptions _receiverOptions = new ReceiverOptions()
    {
        AllowedUpdates = new UpdateType[]
        {
            UpdateType.Message,
            UpdateType.EditedMessage,
            UpdateType.ChannelPost,
            UpdateType.EditedChannelPost,
        }
    };

    public BotService(TelegramBotClient botClient)
    {
        _bot = botClient;

        _bot.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message)
        {
            if (update.Message?.Type == MessageType.Text)
            {
                var chatId = update.Message.Chat.Id;
                var chatName = update.Message.Chat.FirstName +
                               (update.Message.Chat.LastName is null
                                    ? ""
                                    : " " + update.Message.Chat.LastName);
                var messageID = update.Message.MessageId;
                var text = update.Message.Text;
                var userName = update.Message.Chat.Username;

                Console.WriteLine($"Message from {chatName}: {text}");
            }
        }
        else if (update.Type == UpdateType.ChannelPost)
        {
            if (update.ChannelPost?.Type == MessageType.Text)
            {
                var channelId = update.ChannelPost.Chat.Id;
                var channelName = update.ChannelPost.Chat.Title;
                var messageID = update.ChannelPost.MessageId;
                var text = update.ChannelPost.Text;
                var userName = update.ChannelPost?.AuthorSignature;

                Console.WriteLine($"Post in {channelName}{(userName is null ? "" : " by " + userName)}: {text}");
            }
        }
    }

    private async Task ErrorHandler(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}