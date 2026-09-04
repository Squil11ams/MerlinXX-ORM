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

var table = new DataTable();
table.Columns.Add("model_id", typeof(int));
table.Columns.Add("child_name", typeof(string));
table.Rows.Add(1, "First");
table.Rows.Add(2, "Second");

using var reader = table.CreateDataReader();
Assert(reader.Read(), "Ordinal test data was empty.");
var ordinalMap = MerlinOrdinalMap.Build(typeof(OrdinalModel), reader);
var mapped = new List<OrdinalModel>();

do
{
    var model = new OrdinalModel();
    model.SetDataObject(reader, ordinalMap);
    mapped.Add(model);
}
while (reader.Read());

Assert(mapped.Count == 2 && mapped[1].Id == 2 && mapped[1].Child.Name == "Second",
    "Cached ordinal mapping or nested ordinal mapping failed.");
var projectionTable = new DataTable();
projectionTable.Columns.Add("projection_id", typeof(int));
projectionTable.Rows.Add(7);

using var projectionReader = projectionTable.CreateDataReader();
Assert(projectionReader.Read(), "Projection test data was empty.");

AssertThrows<MerlinMissingColumnException>(
    () => MerlinOrdinalMap.Build(typeof(ProjectionModel), projectionReader, MappingStrictness.Strict),
    "Strict mapping accepted a missing property.");

var projectionMap = MerlinOrdinalMap.Build(
    typeof(ProjectionModel),
    projectionReader,
    MappingStrictness.Projection);
var projectionModel = new ProjectionModel();
projectionModel.SetDataObject(projectionReader, projectionMap);
Assert(projectionModel.Id == 7 && projectionModel.Name == "unchanged" && projectionMap.Entries.Length == 1,
    "Projection mapping did not omit the missing property from its execution plan.");

var validatedMap = MerlinOrdinalMap.Build(
    typeof(ProjectionModel),
    projectionReader,
    MappingStrictness.Validated);
Assert(validatedMap.Entries.Length == 1, "Validated mapping did not accept an absent optional property.");

var invalidValidatedTable = new DataTable();
invalidValidatedTable.Columns.Add("projection_name", typeof(string));
invalidValidatedTable.Rows.Add("Name");
using var invalidValidatedReader = invalidValidatedTable.CreateDataReader();
Assert(invalidValidatedReader.Read(), "Validated test data was empty.");
AssertThrows<MerlinMissingColumnException>(
    () => MerlinOrdinalMap.Build(typeof(ProjectionModel), invalidValidatedReader, MappingStrictness.Validated),
    "Validated mapping accepted an absent required property.");

Assert(new MyQuery().MappingStrictness == MappingStrictness.Strict,
    "MySQL queries must default to strict mapping.");
Assert(new MsQuery().MappingStrictness == MappingStrictness.Strict,
    "SQL Server queries must default to strict mapping.");

var optionalNestedTable = new DataTable();
optionalNestedTable.Columns.Add("optional_name", typeof(string));
optionalNestedTable.Rows.Add(DBNull.Value);
optionalNestedTable.Rows.Add("Present");

using var optionalNestedReader = optionalNestedTable.CreateDataReader();
Assert(optionalNestedReader.Read(), "Optional nested test data was empty.");
var optionalNestedMap = MerlinOrdinalMap.Build(typeof(OptionalNestedParent), optionalNestedReader);
var absentParent = new OptionalNestedParent();
absentParent.SetDataObject(optionalNestedReader, optionalNestedMap);
Assert(absentParent.Child == null,
    "An all-NULL nested result should not create an optional nested object.");

Assert(optionalNestedReader.Read(), "Populated nested test row was missing.");
var presentParent = new OptionalNestedParent();
presentParent.SetDataObject(optionalNestedReader, optionalNestedMap);
Assert(presentParent.Child?.Name == "Present",
    "A populated nested result should create and map the optional nested object.");

using var legacyOptionalReader = optionalNestedTable.CreateDataReader();
Assert(legacyOptionalReader.Read(), "Legacy optional nested test data was empty.");
var legacyParent = new OptionalNestedParent();
#pragma warning disable CS0618 // Verify the retained direct-call compatibility entry point.
legacyParent.SetDataObject(legacyOptionalReader);
#pragma warning restore CS0618
Assert(legacyParent.Child == null,
    "Legacy name-based mapping did not honor optional nested creation.");

var hookTable = new DataTable();
hookTable.Columns.Add("hook_id", typeof(int));
hookTable.Columns.Add("hook_name", typeof(string));
hookTable.Rows.Add(0, "Skipped");
hookTable.Rows.Add(2, "mapped");
using var hookReader = hookTable.CreateDataReader();
Assert(hookReader.Read(), "Hook test data was empty.");
var hookMap = MerlinOrdinalMap.Build(typeof(HookModel), hookReader);

var skippedHookModel = new HookModel();
skippedHookModel.SetDataObject(hookReader, hookMap);
Assert(skippedHookModel.BeforeCalled && !skippedHookModel.AfterCalled && skippedHookModel.Id == -1,
    "The before-population hook did not skip automatic mapping.");

Assert(hookReader.Read(), "Populated hook test row was missing.");
var populatedHookModel = new HookModel();
populatedHookModel.SetDataObject(hookReader, hookMap);
Assert(populatedHookModel.BeforeCalled && populatedHookModel.AfterCalled &&
       populatedHookModel.Id == 2 && populatedHookModel.Name == "MAPPED",
    "Lifecycle hooks did not run around ordinal population.");

var generatedHookMapper = MerlinGeneratedMapping<HookModel>.Mapper;
Assert(generatedHookMapper != null && generatedHookMapper.ModelType == typeof(HookModel),
    "The compiler-generated HookModel mapper was not registered.");

using var generatedHookReader = hookTable.CreateDataReader();
Assert(generatedHookReader.Read() && generatedHookReader.Read(),
    "Generated hook test row was missing.");
var generatedHookPlan = generatedHookMapper!.CreatePlan(
    generatedHookReader,
    MappingStrictness.Strict);
var generatedHookModel = new HookModel();
generatedHookModel.SetDataObject(generatedHookReader, generatedHookMapper, generatedHookPlan);
Assert(generatedHookModel.BeforeCalled && generatedHookModel.AfterCalled &&
       generatedHookModel.Id == 2 && generatedHookModel.Name == "MAPPED",
    "Generated mapping did not execute direct assignments through lifecycle hooks.");

var generatedNestedMapper = MerlinGeneratedMapping<OptionalNestedParent>.Mapper;
Assert(generatedNestedMapper?.CanMap == true, "Generated recursive nested mapper was not composed.");
using var generatedNestedReader = optionalNestedTable.CreateDataReader();
Assert(generatedNestedReader.Read(), "Generated nested null row was missing.");
var generatedNestedPlan = generatedNestedMapper!.CreatePlan(generatedNestedReader, MappingStrictness.Strict);
var generatedAbsent = (OptionalNestedParent)MerlinGeneratedRuntime.CreateAndPopulate(
    generatedNestedMapper, generatedNestedReader, generatedNestedPlan);
Assert(generatedAbsent.Child == null, "Generated optional nested creation did not suppress an all-NULL child.");
Assert(generatedNestedReader.Read(), "Generated nested populated row was missing.");
var generatedPresent = (OptionalNestedParent)MerlinGeneratedRuntime.CreateAndPopulate(
    generatedNestedMapper, generatedNestedReader, generatedNestedPlan);
Assert(generatedPresent.Child?.Name == "Present", "Generated recursive child mapping failed.");

var immutableTable = new DataTable();
immutableTable.Columns.Add("immutable_id", typeof(int));
immutableTable.Columns.Add("immutable_name", typeof(string));
immutableTable.Rows.Add(9, "Record");
using var immutableReader = immutableTable.CreateDataReader();
Assert(immutableReader.Read(), "Immutable model row was missing.");
var immutableMapper = MerlinGeneratedMapping<ImmutableModel>.Mapper;
Assert(immutableMapper?.CanMap == true, "Record mapper was not generated.");
var immutablePlan = immutableMapper!.CreatePlan(immutableReader, MappingStrictness.Strict);
var immutable = (ImmutableModel)MerlinGeneratedRuntime.CreateAndPopulate(immutableMapper, immutableReader, immutablePlan);
Assert(immutable.Id == 9 && immutable.Name == "Record", "Generated constructor mapping failed.");

var privateTable = new DataTable();
privateTable.Columns.Add("private_id", typeof(int));
privateTable.Rows.Add(12);
using var privateReader = privateTable.CreateDataReader();
Assert(privateReader.Read(), "Private-setter row was missing.");
var privateMapper = MerlinGeneratedMapping<PrivateSetterModel>.Mapper;
Assert(privateMapper?.CanMap == true, "Partial access shim mapper was not generated.");
var privatePlan = privateMapper!.CreatePlan(privateReader, MappingStrictness.Strict);
var privateModel = (PrivateSetterModel)MerlinGeneratedRuntime.CreateAndPopulate(privateMapper, privateReader, privatePlan);
Assert(privateModel.Id == 12, "Generated partial access shim failed.");

Console.WriteLine("All MerlinORM smoke tests passed.");
return;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
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

sealed class OrdinalModel : MerlinModelBase
{
    [AutoPopSettings("model_id")]
    public int Id { get; set; }

    [MerlinObject("child_")]
    public OrdinalChild Child { get; set; } = null!;
}

sealed class OrdinalChild : MerlinModelBase
{
    [AutoPopSettings("name")]
    public string Name { get; set; } = string.Empty;
}

sealed class ProjectionModel : MerlinModelBase
{
    [MerlinRequired]
    [AutoPopSettings("projection_id")]
    public int Id { get; set; }

    [AutoPopSettings("projection_name")]
    public string Name { get; set; } = "unchanged";
}

sealed class OptionalNestedParent : MerlinModelBase
{
    [MerlinObject("optional_", NestedObjectCreation.WhenAnyColumnHasValue)]
    public OptionalNestedChild? Child { get; set; } = new();
}

sealed class OptionalNestedChild : MerlinModelBase
{
    public string Name { get; set; } = string.Empty;
}

sealed class HookModel : MerlinModelBase
{
    [AutoPopSettings("hook_id")]
    public int Id { get; set; } = -1;

    [AutoPopSettings("hook_name")]
    public string Name { get; set; } = "unchanged";

    [Exclude]
    public bool BeforeCalled { get; private set; }

    [Exclude]
    public bool AfterCalled { get; private set; }

    protected override AutoPopulateDecision OnBeforeAutoPopulate(in MerlinMappingContext context)
    {
        BeforeCalled = true;

        return context.TryGetValue<int>("hook_id", out var id) && id != 0
            ? AutoPopulateDecision.Continue
            : AutoPopulateDecision.Skip;
    }

    protected override void OnAfterAutoPopulate(in MerlinMappingContext context)
    {
        AfterCalled = true;
        Name = Name.ToUpperInvariant();
    }
}

[MerlinModel]
sealed record ImmutableModel(
    [property: AutoPopSettings("immutable_id")] int Id,
    [property: AutoPopSettings("immutable_name")] string Name);

sealed partial class PrivateSetterModel : MerlinModelBase
{
    [AutoPopSettings("private_id")]
    public int Id { get; private set; }
}
