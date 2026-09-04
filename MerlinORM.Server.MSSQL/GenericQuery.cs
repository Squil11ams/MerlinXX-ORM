namespace MerlinORM.Server.MSSQL;

/// <summary>General-purpose SQL Server query provider.</summary>
public class GenericQuery : BaseQuery
{
    /// <summary>Creates an empty query.</summary>
    public GenericQuery()
    {
    }

    /// <summary>Creates a query with command text.</summary>
    public GenericQuery(string query)
    {
        Query = query;
    }

    /// <summary>Creates a query with one parameter.</summary>
    public GenericQuery(string query, string key, object? value)
        : this(query)
    {
        AddParameter(key, value);
    }

    /// <summary>Creates a query from command text and a parameter dictionary.</summary>
    public GenericQuery(string query, IReadOnlyDictionary<string, object?> parameters)
        : this(query)
    {
        foreach (var parameter in parameters)
        {
            AddParameter(parameter.Key, parameter.Value);
        }
    }

    /// <summary>Returns the SQL and a diagnostic parameter listing.</summary>
    public string GetQuery()
    {
        var output = new System.Text.StringBuilder().AppendLine(Query);
        var index = 1;

        foreach (var parameter in Parameters)
        {
            output.AppendLine($"[{index}] {parameter.ParameterName} => {parameter.Value}");
            index++;
        }

        return output.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => GetQuery();
}
