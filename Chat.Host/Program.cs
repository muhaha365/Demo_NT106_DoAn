using System.Net;
using System.Text;
using UitChat.Core;
using UitChat.Server;

Console.OutputEncoding = Encoding.UTF8;
string directory = args.Length > 0 ? args[0] : Limits.DefaultDataDirectory;
int port = args.Length > 1 ? int.Parse(args[1]) : Limits.Port;
string address = args.Length > 2 ? args[2] : "127.0.0.1";
try
{
    await using var server = new ChatServer(directory, IPAddress.Parse(address), port);
    server.Log += line => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
    server.Start();
    Console.WriteLine($"Dữ liệu: {directory}\nSHA-256: {server.Fingerprint}\nCtrl+C để dừng server.");
    var exit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; exit.TrySetResult(); };
    await exit.Task;
}
catch (Exception ex) { Console.Error.WriteLine("Không thể chạy server: " + ex.Message); Environment.ExitCode = 1; }
