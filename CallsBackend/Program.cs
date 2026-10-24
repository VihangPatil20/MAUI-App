using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var apiKey = Environment.GetEnvironmentVariable("CALLS_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException(
        "Configure the CALLS_API_KEY environment variable.");

var connectionString = "Data Source=calls.db";

await using (var db = new SqliteConnection(connectionString))
{
    await db.OpenAsync();

    var command = db.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS Calls (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PhoneNumber TEXT NOT NULL,
            CapturedAtUtc TEXT NOT NULL
        );
        """;

    await command.ExecuteNonQueryAsync();
}

bool IsAuthorized(HttpRequest request)
{
    var supplied = request.Headers["X-API-Key"].ToString();

    return !string.IsNullOrEmpty(supplied) &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied),
            Encoding.UTF8.GetBytes(apiKey));
}

app.MapPost("/calls", async (HttpRequest request, CallRecord record) =>
{
    if (!IsAuthorized(request))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(record.PhoneNumber) ||
        record.PhoneNumber.Length > 40)
        return Results.BadRequest("Invalid phone number.");

    await using var db = new SqliteConnection(connectionString);
    await db.OpenAsync();

    var command = db.CreateCommand();
    command.CommandText = """
        INSERT INTO Calls (PhoneNumber, CapturedAtUtc)
        VALUES ($number, $time);
        """;

    command.Parameters.AddWithValue("$number", record.PhoneNumber);
    command.Parameters.AddWithValue(
        "$time", record.CapturedAtUtc.ToUniversalTime().ToString("O"));

    await command.ExecuteNonQueryAsync();

    return Results.Created("/calls", new { saved = true });
});

app.MapGet("/calls", async (HttpRequest request) =>
{
    if (!IsAuthorized(request))
        return Results.Unauthorized();

    var results = new List<CallRecord>();

    await using var db = new SqliteConnection(connectionString);
    await db.OpenAsync();

    var command = db.CreateCommand();
    command.CommandText = """
        SELECT PhoneNumber, CapturedAtUtc
        FROM Calls
        ORDER BY Id DESC
        LIMIT 200;
        """;

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        results.Add(new CallRecord
        {
            PhoneNumber = reader.GetString(0),
            CapturedAtUtc = DateTimeOffset.Parse(reader.GetString(1))
        });
    }

    return Results.Ok(results);
});

app.Run();

public class CallRecord
{
    public string PhoneNumber { get; set; } = "";
    public DateTimeOffset CapturedAtUtc { get; set; }
}