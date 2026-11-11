using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using OrderSearch;

var output = Console.Out;
Console.SetOut(Console.Error);
var request = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
// Independent fixture SQL uses the published existing schema, not repository mocks.
using (var connection = new SqliteConnection("Data Source=" + path))
{
    connection.Open();
    using var create = connection.CreateCommand();
    create.CommandText = "CREATE TABLE orders(id TEXT PRIMARY KEY,tenant_id TEXT NOT NULL,created_utc TEXT NOT NULL,status TEXT NOT NULL,total_minor INTEGER NOT NULL)";
    create.ExecuteNonQuery();
    foreach (var row in request.GetProperty("Orders").EnumerateArray().Reverse())
    {
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO orders VALUES($id,$tenant,$created,$status,$total)";
        insert.Parameters.AddWithValue("$id", row.GetProperty("Id").GetString());
        insert.Parameters.AddWithValue("$tenant", row.GetProperty("TenantId").GetString());
        insert.Parameters.AddWithValue("$created", row.GetProperty("CreatedUtc").GetDateTimeOffset().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", System.Globalization.CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$status", row.GetProperty("Status").GetString());
        insert.Parameters.AddWithValue("$total", row.GetProperty("TotalMinor").GetInt64());
        insert.ExecuteNonQuery();
    }
}
await using var app = OrderApplication.Build(path);
app.Urls.Add("http://127.0.0.1:0");
await app.StartAsync();
string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
List<object> responses = [];
foreach (var call in request.GetProperty("Calls").EnumerateArray())
{
    using var message = new HttpRequestMessage(new HttpMethod(call.GetProperty("Method").GetString()!), call.GetProperty("Path").GetString());
    if (call.GetProperty("Token").ValueKind != JsonValueKind.Null)
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", call.GetProperty("Token").GetString());
    if (call.GetProperty("SpoofTenant").ValueKind != JsonValueKind.Null)
        message.Headers.Add("X-Tenant-Id", call.GetProperty("SpoofTenant").GetString());
    if (call.GetProperty("Body").ValueKind != JsonValueKind.Null)
        message.Content = new StringContent(call.GetProperty("Body").GetRawText(), System.Text.Encoding.UTF8, "application/json");
    using var response = await client.SendAsync(message);
    responses.Add(new { Status = (int)response.StatusCode, Body = await response.Content.ReadAsStringAsync() });
}
await app.StopAsync();
output.Write(JsonSerializer.Serialize(new { Responses = responses }));
