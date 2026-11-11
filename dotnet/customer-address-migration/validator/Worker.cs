using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using CustomerAddresses;

var output = Console.Out;
Console.SetOut(Console.Error);
string group = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement.GetProperty("Group").GetString()!;
string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
bool fresh = group == "fresh-database";
if (!fresh) Fixture.Create(path);
var database = new CustomerDatabase(path);
var migrator = new DatabaseMigrator(database);
if (group == "transactional-upgrade")
{
    using (var connection = Fixture.Open(path))
    using (var trigger = connection.CreateCommand())
    {
        trigger.CommandText = "CREATE TRIGGER reject_version_two BEFORE INSERT ON schema_versions WHEN NEW.version=2 BEGIN SELECT RAISE(ABORT,'fixture migration failure'); END";
        trigger.ExecuteNonQuery();
    }
    var beforeSchema = Fixture.Schema(path);
    var beforeData = Fixture.LegacyRows(path);
    bool failed = false;
    try { migrator.Apply(); }
    catch (Exception) { failed = true; }
    output.Write(JsonSerializer.Serialize(new { Failed = failed, BeforeSchema = beforeSchema,
        AfterSchema = Fixture.Schema(path), BeforeData = beforeData, AfterData = Fixture.LegacyRows(path), Version = Fixture.Version(path) }));
    return;
}
migrator.Apply();
migrator.Apply();
var service = new CustomerService(database);
object? Call(string name, params object[] parameters)
{
    var method = typeof(CustomerService).GetMethods().SingleOrDefault(x => x.Name == name && x.GetParameters().Length == parameters.Length)
        ?? throw new MissingMethodException("Required public API: " + name);
    return method.Invoke(service, parameters);
}
JsonElement Element(object? value) => JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object));
var metadata = new Dictionary<string, object?>();
if (group == "populated-upgrade")
{
    metadata["Views"] = new[] { service.GetCustomer("c-1"), service.GetCustomer("c-2"), service.GetCustomer("c-3") };
    metadata["Lists"] = new[] { Element(Call("GetAddresses", "c-1")), Element(Call("GetAddresses", "c-2")), Element(Call("GetAddresses", "c-3")) };
}
else if (group == "address-operations")
{
    var added = Element(Call("AddAddress", "c-1", new Address("22 Oak Rd", "York", "YO1", "GB"), false));
    string id = added.GetProperty("Id").GetString()!;
    metadata["Selected"] = Call("SetPrimaryAddress", "c-1", id);
    metadata["Repeated"] = Call("SetPrimaryAddress", "c-1", id);
    metadata["SelectedView"] = service.GetCustomer("c-1");
    metadata["Updated"] = service.SetAddress("c-1", new Address("24 Oak Rd", "York", "YO2", "GB"));
    metadata["UpdatedView"] = new CustomerService(new CustomerDatabase(path)).GetCustomer("c-1");
    metadata["Addresses"] = Call("GetAddresses", "c-1");
    metadata["Primary"] = Call("GetPrimaryAddress", "c-1");
    metadata["SelectedId"] = id;
}
else if (group == "primary-invariants")
{
    metadata["InitiallyEmpty"] = Call("GetAddresses", "c-3");
    metadata["InitiallyPrimary"] = Call("GetPrimaryAddress", "c-3");
    metadata["First"] = Call("AddAddress", "c-3", new Address("One", "Bath", "P1", "GB"), false);
    metadata["FirstRows"] = Fixture.Addresses(path);
    metadata["Second"] = Call("AddAddress", "c-3", new Address("Two", "Bath", "P2", "GB"), false);
    metadata["SecondRows"] = Fixture.Addresses(path);
    var third = Element(Call("AddAddress", "c-3", new Address("Three", "Bath", "P3", "GB"), true));
    metadata["Third"] = third;
    var other = Element(Call("GetAddresses", "c-1")).EnumerateArray().Single();
    metadata["ForeignSelection"] = Call("SetPrimaryAddress", "c-3", other.GetProperty("Id").GetString()!);
    metadata["MissingSelection"] = Call("SetPrimaryAddress", "c-3", "missing");
    bool unknownRejected = false;
    try { Call("AddAddress", "missing", new Address("Four", "Bath", "P4", "GB"), false); }
    catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException) { unknownRejected = true; }
    metadata["UnknownRejected"] = unknownRejected;
    metadata["Primary"] = Call("GetPrimaryAddress", "c-3");
    metadata["Legacy"] = service.GetCustomer("c-3");
    metadata["Addresses"] = Call("GetAddresses", "c-3");
}
else if (group == "fresh-database")
{
    service.CreateCustomer("new-1", "Fresh", new Address("New", "Leeds", "LS1", "GB"));
    service.CreateCustomer("new-2", "Addressless", null);
    metadata["Renamed"] = service.RenameCustomer("new-1", "Fresh revised");
    metadata["Updated"] = service.SetAddress("new-1", new Address("Revised", "Leeds", "LS2", "GB"));
    metadata["View"] = new CustomerService(new CustomerDatabase(path)).GetCustomer("new-1");
    metadata["EmptyView"] = service.GetCustomer("new-2");
    metadata["Addresses"] = Call("GetAddresses", "new-1");
    metadata["EmptyAddresses"] = Call("GetAddresses", "new-2");
    metadata["Primary"] = Call("GetPrimaryAddress", "new-1");
    var directory = new CustomerDirectory(database);
    metadata["Count"] = directory.Count();
    metadata["Page"] = directory.Page(1, 1);
    metadata["Csv"] = new CustomerExport(directory).Csv();
    metadata["MissingView"] = service.GetCustomer("missing");
    metadata["Established"] = service.SetAddress("new-2", new Address("Established", "Leeds", "LS3", "GB"));
    metadata["EstablishedView"] = new CustomerService(new CustomerDatabase(path)).GetCustomer("new-2");
}
else throw new ArgumentException("Unknown group.");
output.Write(JsonSerializer.Serialize(new { Data = metadata, Rows = Fixture.Addresses(path),
    Customers = Fixture.Customers(path), Version = Fixture.Version(path) }));

static class Fixture
{
    public static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection("Data Source=" + path);
        connection.Open();
        return connection;
    }

    public static void Create(string path)
    {
        using var connection = Open(path);
        using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE schema_versions(version INTEGER PRIMARY KEY);
            INSERT INTO schema_versions VALUES (1);
            CREATE TABLE customers(id TEXT PRIMARY KEY,name TEXT NOT NULL,line1 TEXT NULL,
                city TEXT NULL,postal_code TEXT NULL,country_code TEXT NULL);
            INSERT INTO customers VALUES('c-1','Ada','12 Bridge St','Bath','BA1 1AA','GB');
            INSERT INTO customers VALUES('c-2','Grace, Hopper','4 Rue de l''Ete','Lyon','69001','FR');
            INSERT INTO customers VALUES('c-3','Lin',NULL,NULL,NULL,NULL);
            """;
        create.ExecuteNonQuery();
    }

    public static long Version(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(version) FROM schema_versions";
        return (long)command.ExecuteScalar()!;
    }

    public static List<Dictionary<string, object?>> Query(string path, string sql)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        List<Dictionary<string, object?>> rows = [];
        while (reader.Read())
        {
            Dictionary<string, object?> row = new();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    public static object Customers(string path) => Query(path, "SELECT id AS Id,name AS Name FROM customers ORDER BY id");
    public static object Addresses(string path) => Query(path, "SELECT id AS Id,customer_id AS CustomerId,line1 AS Line1,city AS City,postal_code AS PostalCode,country_code AS CountryCode,is_primary AS IsPrimary FROM customer_addresses ORDER BY id");
    public static object Schema(string path) => Query(path, "SELECT type,name,COALESCE(sql,'') AS sql FROM sqlite_master ORDER BY type,name");
    public static object LegacyRows(string path) => Query(path, "SELECT * FROM customers ORDER BY id");
}
