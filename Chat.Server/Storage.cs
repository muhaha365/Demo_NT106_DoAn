using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using UitChat.Core;

namespace UitChat.Server;

internal sealed class Account
{
    public string Username { get; set; } = "";
    public string Salt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public HashSet<string> Friends { get; set; } = [];
}
internal sealed class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public bool IsGroup { get; set; }
    public string Owner { get; set; } = "";
    public List<string> Members { get; set; } = [];
    public Dictionary<string, long> ReadThrough { get; set; } = [];
}
internal sealed class Database
{
    public int SchemaVersion { get; set; } = 1;
    public long NextMessageId { get; set; } = 1;
    public List<Account> Accounts { get; set; } = [];
    public List<FriendRequestView> Requests { get; set; } = [];
    public List<Conversation> Conversations { get; set; } = [];
    public List<MessageView> Messages { get; set; } = [];
}

internal sealed class Storage : IDisposable
{
    private readonly string path;
    private readonly FileStream instanceLock;
    public string DirectoryPath { get; }
    public Database Data { get; private set; }

    public Storage(string directory)
    {
        DirectoryPath = Path.GetFullPath(directory);
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(Path.Combine(DirectoryPath, "files"));
        // Only one server may mutate this database, even if another port is used.
        instanceLock = new FileStream(Path.Combine(DirectoryPath, "server.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        path = Path.Combine(DirectoryPath, "chat.json");
        try
        {
            Data = File.Exists(path) ? JsonSerializer.Deserialize<Database>(File.ReadAllText(path), Json.Options)
                ?? throw new InvalidDataException("Tệp dữ liệu rỗng.") : new Database();
            if (Data.SchemaVersion != 1) throw new InvalidDataException("Phiên bản dữ liệu không được hỗ trợ.");
        }
        catch { instanceLock.Dispose(); throw; }
    }

    public Database Copy() => JsonSerializer.Deserialize<Database>(JsonSerializer.SerializeToUtf8Bytes(Data, Json.Options), Json.Options)!;

    public void Commit(Database next)
    {
        // Commit to disk before publishing events. Failure leaves the old state intact.
        string temp = path + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, next, Json.Options);
            file.Flush(flushToDisk: true);
        }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
        else File.Move(temp, path);
        Data = next;
    }
    public string AttachmentPath(long id) => Path.Combine(DirectoryPath, "files", id + ".bin");
    public void Dispose() => instanceLock.Dispose();
}

public static class Passwords
{
    public const int Iterations = 600_000;
    public static (string Salt, string Hash) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(Derive(password, salt)));
    }
    private static byte[] Derive(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
    public static bool Verify(string password, string salt, string hash) =>
        CryptographicOperations.FixedTimeEquals(Derive(password, Convert.FromBase64String(salt)), Convert.FromBase64String(hash));
}

public static class ServerCertificate
{
    public static X509Certificate2 LoadOrCreate(string directory)
    {
        string path = Path.Combine(directory, "server.pfx");
        if (File.Exists(path)) return X509CertificateLoader.LoadPkcs12FromFile(path, null);
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest("CN=UITChat", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
        File.WriteAllBytes(path, cert.Export(X509ContentType.Pfx));
        return X509CertificateLoader.LoadPkcs12FromFile(path, null);
    }
}
