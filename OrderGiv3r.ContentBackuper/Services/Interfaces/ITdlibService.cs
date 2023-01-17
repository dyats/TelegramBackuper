using TL;

namespace OrderGiv3r.Application.Services.Interfaces;

public interface ITdlibService
{
    Task<Messages_Chats> GetAllChatsAsync(CancellationToken cancellationToken = default);
    Task<ChatBase?> GetChatByNameAsync(string chatName, CancellationToken cancellationToken = default);
    Task<List<Message>> GetMessagesFromChatAsync(ChatBase chat, DateTime loadMessagesBeforeDate = default, CancellationToken cancellationToken = default);
}