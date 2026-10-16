using System.Text.Json;

namespace UitChat.Core;

// DTOs are shared by the server, WinForms client and integration tests.
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
    public static T Read<T>(JsonElement value) => value.Deserialize<T>(Options)
        ?? throw new InvalidDataException("Nội dung yêu cầu không hợp lệ.");
}

public sealed record Packet(string Type, string Id, JsonElement Data, bool Ok = true, string? Error = null)
{
    public static Packet Create<T>(string type, T data, string id = "") => new(type, id, Json.Element(data));
}
public record Credentials(string Username, string Password);
public record UserView(string Username, bool Online, bool IsFriend);
public record FriendRequestView(string From, string To);
public record ConversationView(string Id, string Name, bool IsGroup, string Owner,
    List<string> Members, string Preview, DateTimeOffset? UpdatedAt, int Unread);
public record Snapshot(string Username, List<UserView> Users, List<FriendRequestView> Requests,
    List<ConversationView> Conversations);
public record UserTarget(string Username);
public record FriendDecision(string Username, bool Accept);
public record ConversationTarget(string ConversationId);
public record GroupCreate(string Name, List<string> Members);
public record GroupMember(string ConversationId, string Username, bool Add);
public record HistoryQuery(string ConversationId, long Before = long.MaxValue, string Search = "");
public record HistoryPage(List<MessageView> Messages, bool HasMore);
public record SendMessage(string ConversationId, string Text, string ClientId,
    string? FileName = null, byte[]? FileBytes = null);
public record MessageView(long Id, string ConversationId, string Sender, string Text,
    DateTimeOffset SentAt, List<string> SeenBy, string? FileName, long FileSize, string ClientId);
public record ReadRequest(string ConversationId, long ThroughId);
public record ReadEvent(string ConversationId, string Username, long ThroughId);
public record TypingEvent(string ConversationId, string Username, bool IsTyping);
public record TypingRequest(string ConversationId, bool IsTyping);
public record FileRequest(long MessageId);
public record FileReply(string FileName, byte[] Bytes);
public record ConversationReply(string Id);
public record Notice(string Text);

public static class Limits
{
    public const int Port = 5050;
    public const int MaxFrameBytes = 8 * 1024 * 1024;
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxTextLength = 4000;
    public const int HistoryPageSize = 50;
    public const int MaxMembers = 50;
    public static string DefaultDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UITChat", "server");
}
