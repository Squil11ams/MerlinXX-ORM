using System.Data;

namespace MerlinORM.Client;

/// <summary>
/// Provides allocation-free access to the current result row and its precomputed column ordinals.
/// </summary>
public readonly struct MerlinMappingContext
{
    private readonly IMerlinMappingPlan _mappingPlan;

    /// <summary>The current result row. It cannot advance the underlying reader.</summary>
    public IDataRecord Data { get; }

    /// <summary>The prefix applied to mapped column names for the current model.</summary>
    public string Prefix => _mappingPlan.Prefix;

    /// <summary>The missing-column policy selected by the query.</summary>
    public MappingStrictness Strictness => _mappingPlan.Strictness;

    internal MerlinMappingContext(IDataRecord data, IMerlinMappingPlan mappingPlan)
    {
        Data = data;
        _mappingPlan = mappingPlan;
    }

    /// <summary>Looks up an exact result-column name in the cached ordinal table.</summary>
    public bool TryGetOrdinal(string columnName, out int ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        return _mappingPlan.TryGetOrdinal(columnName, out ordinal);
    }

    /// <summary>Looks up a column after applying the current model prefix.</summary>
    public bool TryGetMappedOrdinal(string columnName, out int ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        return _mappingPlan.TryGetOrdinal(Prefix + columnName, out ordinal);
    }

    /// <summary>Reads and converts an exact result column. Missing and database-NULL values return false.</summary>
    public bool TryGetValue<T>(string columnName, out T? value)
    {
        if (TryGetOrdinal(columnName, out var ordinal))
        {
            return TryReadValue(ordinal, out value);
        }

        value = default;
        return false;
    }

    /// <summary>Reads and converts a result column after applying the current model prefix.</summary>
    public bool TryGetMappedValue<T>(string columnName, out T? value)
    {
        if (TryGetMappedOrdinal(columnName, out var ordinal))
        {
            return TryReadValue(ordinal, out value);
        }

        value = default;
        return false;
    }

    private bool TryReadValue<T>(int ordinal, out T? value)
    {
        if (Data.IsDBNull(ordinal))
        {
            value = default;
            return false;
        }

        value = MerlinConvert.To<T>(Data.GetValue(ordinal));
        return true;
    }
}
