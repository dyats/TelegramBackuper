using TL;

namespace OrderGiv3r.ContentBackuper.Interfaces;

public interface ITdlibService
{
    Task<Messages_Chats> GetAllChatsAsync();
    Task<ChatBase?> GetChatByNameAsync(string chatName);
    Task<List<Message>> GetMessagesFromChatAsync(ChatBase chat, DateTime loadMessagesBeforeDate = default);
}