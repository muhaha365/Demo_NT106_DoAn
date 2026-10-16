using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace UitChat.Core;

public sealed class ChatClient : IAsyncDisposable
{
    private readonly TcpClient tcp = new() { NoDelay = true };
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Packet>> pending = new();
    private SslStream? stream;
    private Task? receiveTask, heartbeatTask;
    private int stopped;
    public event Action<Packet>? EventReceived;
    public event Action<string>? Disconnected;
    public bool IsConnected => stream is not null && !lifetime.IsCancellationRequested;

    public async Task ConnectAsync(string host, int port, string fingerprint)
    {
        fingerprint = fingerprint.Replace(" ", "").Replace(":", "").ToUpperInvariant();
        if (fingerprint.Length != 64 || !fingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("Hãy nhập mã SHA-256 chứng chỉ gồm 64 ký tự từ cửa sổ server.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await tcp.ConnectAsync(host, port, timeout.Token);
        stream = new SslStream(tcp.GetStream(), false, (_, certificate, _, _) =>
            certificate is not null && CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(certificate.GetRawCertData()), Convert.FromHexString(fingerprint)));
        await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host, EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }, timeout.Token);
        receiveTask = ReceiveAsync();
        heartbeatTask = HeartbeatAsync();
    }

    public async Task<T> RequestAsync<T>(string type, object data)
    {
        if (!IsConnected) throw new IOException("Chưa kết nối server.");
        string id = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = completion;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await writeGate.WaitAsync(timeout.Token);
            try { await FrameCodec.WriteAsync(stream!, Packet.Create(type, data, id), timeout.Token); }
            catch { Stop("Kết nối bị gián đoạn trong khi gửi dữ liệu."); throw; }
            finally { writeGate.Release(); }
            var reply = await completion.Task.WaitAsync(timeout.Token);
            if (!reply.Ok) throw new InvalidOperationException(reply.Error ?? "Yêu cầu bị từ chối.");
            return Json.Read<T>(reply.Data);
        }
        catch (OperationCanceledException)
        {
            Stop("Server không phản hồi. Đăng nhập lại để đồng bộ lịch sử.");
            throw new IOException("Mất kết nối hoặc hết thời gian chờ. Kiểm tra lịch sử trước khi gửi lại.");
        }
        finally { pending.TryRemove(id, out _); }
    }

    private async Task ReceiveAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var packet = await FrameCodec.ReadAsync(stream!, lifetime.Token);
                if (packet.Id.Length > 0 && pending.TryRemove(packet.Id, out var completion)) completion.TrySetResult(packet);
                else EventReceived?.Invoke(packet);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or SocketException or OperationCanceledException or ObjectDisposedException or System.Text.Json.JsonException)
        { Stop("Đã ngắt kết nối server."); }
    }

    private async Task HeartbeatAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (await timer.WaitForNextTickAsync(lifetime.Token))
                await RequestAsync<Notice>("ping", new { });
        }
        catch (Exception) when (lifetime.IsCancellationRequested) { }
        catch (Exception) { Stop("Không nhận được heartbeat từ server."); }
    }

    private void Stop(string reason)
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        lifetime.Cancel();
        tcp.Dispose();
        foreach (var pair in pending) pair.Value.TrySetException(new IOException(reason));
        pending.Clear();
        Disconnected?.Invoke(reason);
    }

    public async ValueTask DisposeAsync()
    {
        Stop("Đã đóng kết nối.");
        if (receiveTask is not null) await receiveTask;
        if (heartbeatTask is not null) await heartbeatTask;
        stream?.Dispose();
    }
}
