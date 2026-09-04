using System.Data;
using MerlinORM.Client;
using Microsoft.Data.SqlClient;

namespace MerlinORM.Server.MSSQL;

/// <summary>Provides a SQL Server query and its parameters.</summary>
public class BaseQuery : IMerlinProvider
{
    /// <inheritdoc />
    public IEnumerable<IDataParameter> Parameters => SqlParams;

    /// <summary>SQL Server parameters associated with the command.</summary>
    public List<SqlParameter> SqlParams { get; protected set; } = [];

    /// <inheritdoc />
    public virtual string Query { get; set; } = string.Empty;

    /// <inheritdoc />
    public CommandType CommandType { get; protected set; } = CommandType.Text;

    /// <summary>Adds a parameter to the query.</summary>
    public void AddParameter(string key, object? value)
    {
        SqlParams.Add(new SqlParameter(key, value ?? DBNull.Value));
    }

    /// <summary>Configures this query as a stored-procedure command.</summary>
    public void SetSP(string command, params object?[] values)
    {
        Query = command;
        CommandType = CommandType.StoredProcedure;
        SqlParams.Clear();

        for (var index = 0; index < values.Length; index++)
        {
            AddParameter(IntToLetters(index + 1), values[index]);
        }
    }

    private static string IntToLetters(int value)
    {
        var result = string.Empty;

        while (--value >= 0)
        {
            result = (char)('A' + value % 26) + result;
            value /= 26;
        }

        return "@" + result;
    }
}
