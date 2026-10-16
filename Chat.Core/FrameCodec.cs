using System.Buffers.Binary;
using System.Text.Json;

namespace UitChat.Core;

public static class FrameCodec
{
    // TCP is a byte stream: a Read can return part of a frame or several frames.
    // The 4-byte big-endian length prefixes exactly one UTF-8 JSON packet.
    public static async Task WriteAsync(Stream stream, Packet packet, CancellationToken ct = default)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(packet, Json.Options);
        if (body.Length > Limits.MaxFrameBytes) throw new InvalidDataException("Gói tin vượt giới hạn 8 MB.");
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, body.Length);
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(body, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<Packet> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] header = new byte[4];
        await stream.ReadExactlyAsync(header, ct);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > Limits.MaxFrameBytes) throw new InvalidDataException("Độ dài khung tin không hợp lệ.");
        byte[] body = new byte[length];
        await stream.ReadExactlyAsync(body, ct);
        var packet = JsonSerializer.Deserialize<Packet>(body, Json.Options)
            ?? throw new InvalidDataException("Gói tin rỗng.");
        if (string.IsNullOrWhiteSpace(packet.Type) || packet.Id is null || packet.Id.Length > 64)
            throw new InvalidDataException("Header gói tin không hợp lệ.");
        return packet;
    }
}
