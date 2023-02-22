using OrderGiv3r.Application.Services.Interfaces;
using TL;
using WTelegram;

namespace OrderGiv3r.Application.Services;

public class TdlibService : ITdlibService
{
    private Client _client { get; set; }

    public TdlibService(Client client)
    {
        _client = client;
    }

    public async Task<Messages_Chats> GetAllChatsAsync(CancellationToken cancellationToken = default)
    {
        var allChats = await _client.Messages_GetAllChats();

        return allChats;
    }

    public async Task<ChatBase?> GetChatByNameAsync(string chatName, CancellationToken cancellationToken = default)
    {
        var usersChats = await GetAllChatsAsync(cancellationToken);
        var chat = usersChats.chats.FirstOrDefault(x => x.Value.Title.ToLower() == chatName.ToLower()).Value;

        return chat;
    }

    public async Task<List<Message>> GetMessagesFromChatAsync(ChatBase chat, DateTime loadMessagesBeforeDate = default, CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>();

        var messageBases = await _client.Messages_GetHistory(chat.ToInputPeer(), offset_date: loadMessagesBeforeDate);
        foreach (var messageBase in messageBases.Messages.OrderBy(x => x.Date))
            if (messageBase is Message message)
                messages.Add(message);

        return messages;
    }

    public async Task SendMessageToChannelAsync(InputPeer channelId, string message, CancellationToken cancellationToken = default)
    {
        await _client.SendMessageAsync(channelId, message);
    }
}