using OrderGiv3r.ContentBackuper.Interfaces;
using TL;
using WTelegram;

namespace OrderGiv3r.ContentBackuper;

public class TdlibService : ITdlibService
{
    private Client _client { get; set; }
    private User _user { get; set; }

    public TdlibService(Client client, User user)
    {
        _client = client;
        _user = user;
    }

    public async Task<Messages_Chats> GetAllChatsAsync()
    {
        var allChats = await _client.Messages_GetAllChats();

        return allChats;
    }

    public async Task<ChatBase?> GetChatByNameAsync(string chatName)
    {
        var usersChats = await GetAllChatsAsync();
        var chat = usersChats.chats.FirstOrDefault(x => x.Value.Title.ToLower() == chatName.ToLower()).Value;

        return chat;
    }

    public async Task<List<Message>> GetMessagesFromChatAsync(ChatBase chat, DateTime loadMessagesBeforeDate = default)
    {
        var messages = new List<Message>();

        var messageBases = await _client.Messages_GetHistory(chat.ToInputPeer(), offset_date: loadMessagesBeforeDate);
        foreach (var messageBase in messageBases.Messages.OrderBy(x => x.Date))
            if (messageBase is Message message)
                messages.Add(message);

        return messages;
    }
}