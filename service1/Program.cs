using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

// TODO: replace with correct
string storageUrl = Environment.GetEnvironmentVariable("STORAGE_URL") ?? "http://storage:8200";
string vStoragePath = Environment.GetEnvironmentVariable("VSTORAGE_PATH") ?? "/data/vstorage/log.txt";
string service2Url = Environment.GetEnvironmentVariable("SERVICE2_URL") ?? "http://127.0.0.1:8198";

app.MapGet("/status", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var now = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
    var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).TotalHours;

    var record = $"Timestamp1: {now}: uptime {uptime:F2} hours, free disk in root: {GetRootSpace()} MBytes";

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(vStoragePath)!);
        await File.AppendAllTextAsync(vStoragePath, record + "\n", ct);
    }
    catch (Exception e)
    {
        app.Logger.LogWarning(e, "Failed to append to vStorage");
    }

    try
    {
        var client = factory.CreateClient();
        var content = new StringContent(record, Encoding.UTF8, "text/plain");

        await client.PostAsync($"{storageUrl}/log", content, ct);
    }
    catch (Exception e)
    {
        app.Logger.LogWarning(e, "Failed to post to storage");
    }

    string service2Record;

    try
    {
        var client = factory.CreateClient();
        service2Record = await client.GetStringAsync($"{service2Url}/status", ct);
    }
    catch (Exception e)
    {
        app.Logger.LogWarning(e, "Service2 unreachable");
        service2Record = "Service 2 was not reached";
    }

    return Results.Text($"{record}\n{service2Record}", "text/plain");
});

app.MapGet("/log", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient();
    var text = await client.GetStringAsync($"{storageUrl}/log", ct);

    return Results.Text(text, "text/plain");
});

app.Run("http://0.0.0.0:8199");

static long GetRootSpace()
{
    var root = Path.GetPathRoot(Environment.SystemDirectory) ?? "/";

    return new DriveInfo(root).AvailableFreeSpace / (1024 * 1024);
}