using System.Data;
using System.Data.Common;
using MerlinORM.Client;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using MsQuery = MerlinORM.Server.MSSQL.GenericQuery;
using MyQuery = MerlinORM.Server.MySQL.GenericQuery;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Test"] = "Server=(local);Database=Merlin;Integrated Security=true;TrustServerCertificate=true"
    })
    .Build();

Assert(MerlinConfig.GetConnectionString(configuration, "Test").Contains("Database=Merlin"),
    "IConfiguration connection string lookup failed.");

Environment.SetEnvironmentVariable("ConnectionStrings__MerlinEnvironmentTest", "Server=environment-test");
try
{
    Assert(MerlinConfig.GetConnectionString("MerlinEnvironmentTest", "missing-test-settings.json") == "Server=environment-test",
        "Environment variable connection string lookup failed.");
}
finally
{
    Environment.SetEnvironmentVariable("ConnectionStrings__MerlinEnvironmentTest", null);
}

var msQuery = new MsQuery("SELECT * FROM Widget WHERE Id = @Id", "@Id", 42);
using var msConnection = new SqlConnection();
using var msCommand = new TestMsEngine(configuration).BuildCommand(msQuery, msConnection);
Assert(msCommand.CommandText == msQuery.Query && msCommand.Parameters.Count == 1,
    "SQL Server command construction failed.");

msQuery.SetSP("GetWidget", 42);
using var storedProcedure = new TestMsEngine(configuration).BuildCommand(msQuery, msConnection);
Assert(storedProcedure.CommandType == CommandType.StoredProcedure && storedProcedure.CommandText == "GetWidget",
    "SQL Server stored-procedure construction failed.");

var myQuery = new MyQuery("SELECT * FROM Widget WHERE Id = @Id", "@Id", 42);
using var myConnection = new MySqlConnection();
using var myCommand = new TestMyEngine(configuration).BuildCommand(myQuery, myConnection);
Assert(myCommand.CommandText == myQuery.Query && myCommand.Parameters.Count == 1,
    "MySQL compatibility command construction failed.");

Console.WriteLine("All MerlinORM smoke tests passed.");
return;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class TestMsEngine(IConfiguration configuration)
    : MerlinORM.Server.MSSQL.QueryEngine(configuration, "Test")
{
    public DbCommand BuildCommand(IMerlinProvider provider, DbConnection connection) =>
        CreateCommand(provider, connection);
}

sealed class TestMyEngine(IConfiguration configuration)
    : MerlinORM.Server.MySQL.QueryEngine(configuration, "Test")
{
    public DbCommand BuildCommand(IMerlinProvider provider, DbConnection connection) =>
        CreateCommand(provider, connection);
}
