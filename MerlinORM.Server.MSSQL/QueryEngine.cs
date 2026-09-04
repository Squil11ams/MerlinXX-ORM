using System.Data.Common;
using MerlinORM.Client;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MerlinORM.Server.MSSQL;

/// <summary>
/// Executes Merlin queries through Microsoft SQL Server while reusing the shared relational mapping engine.
/// </summary>
public class QueryEngine : RelationalQueryEngine
{
    /// <summary>Creates an engine using the conventional Merlin configuration lookup.</summary>
    public QueryEngine(string connectionStringKey, string appSettings = "appsettings.json")
        : base(connectionStringKey, appSettings)
    {
    }

    /// <summary>Creates an engine using an application's existing configuration pipeline.</summary>
    public QueryEngine(IConfiguration configuration, string connectionStringKey)
        : base(configuration, connectionStringKey)
    {
    }

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    protected override async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc />
    protected override DbCommand CreateCommand(IMerlinProvider provider, DbConnection connection, bool autoParams = true)
    {
        var command = new SqlCommand(provider.Query, (SqlConnection)connection)
        {
            CommandType = provider.CommandType
        };

        if (autoParams)
        {
            foreach (var parameter in provider.Parameters)
            {
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }

    /// <inheritdoc />
    protected override bool IsProviderException(Exception exception) => exception is SqlException;
}
