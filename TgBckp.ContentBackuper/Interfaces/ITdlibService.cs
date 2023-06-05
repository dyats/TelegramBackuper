using TL;

namespace TgBckp.ContentBackuper.Interfaces;

public interface ITdlibService
{
    Task<Messages_Chats> GetAllChatsAsync();
    Task<ChatBase?> GetChatByNameAsync(string chatName);
    Task<List<Message>> GetMessagesFromChatAsync(ChatBase chat, DateTime loadMessagesBeforeDate = default);
}