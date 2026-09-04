using System.Data;

namespace MerlinORM.Client;

/// <summary>
/// Represents the property-to-column plan for one model type and one result-set shape.
/// A plan is built once after a reader is opened and reused for every returned row.
/// </summary>
internal sealed class MerlinOrdinalMap : IMerlinMappingPlan
{
    internal sealed record Entry(
        MerlinPropertyMetadata Property,
        int Ordinal,
        string ColumnName,
        MerlinOrdinalMap? NestedMap);

    public Entry[] Entries { get; }

    public string Prefix { get; }

    public MappingStrictness Strictness { get; }

    public bool HasBeforeAutoPopulateHook { get; }

    public bool HasAfterAutoPopulateHook { get; }

    private readonly IReadOnlyDictionary<string, int> _ordinals;

    private MerlinOrdinalMap(
        Entry[] entries,
        IReadOnlyDictionary<string, int> ordinals,
        MappingStrictness strictness,
        string prefix,
        bool hasBeforeAutoPopulateHook,
        bool hasAfterAutoPopulateHook)
    {
        Entries = entries;
        _ordinals = ordinals;
        Strictness = strictness;
        Prefix = prefix;
        HasBeforeAutoPopulateHook = hasBeforeAutoPopulateHook;
        HasAfterAutoPopulateHook = hasAfterAutoPopulateHook;
    }

    public bool TryGetOrdinal(string columnName, out int ordinal) =>
        _ordinals.TryGetValue(columnName, out ordinal);

    public bool HasAnyValue(IDataRecord record)
    {
        foreach (var entry in Entries)
        {
            if (entry.Ordinal >= 0 && !record.IsDBNull(entry.Ordinal))
            {
                return true;
            }

            if (entry.NestedMap?.HasAnyValue(record) == true)
            {
                return true;
            }
        }

        return false;
    }

    public static bool SupportsOrdinalMapping(Type modelType)
    {
        if (!typeof(MerlinModelBase).IsAssignableFrom(modelType))
        {
            return false;
        }

        return true;
    }

    public static MerlinOrdinalMap Build(
        Type modelType,
        IDataRecord record,
        MappingStrictness strictness = MappingStrictness.Strict,
        string prefix = "")
    {
        var ordinals = new Dictionary<string, int>(record.FieldCount, StringComparer.OrdinalIgnoreCase);

        for (var ordinal = 0; ordinal < record.FieldCount; ordinal++)
        {
            // IDataRecord.GetOrdinal resolves the first duplicate name. Preserve that behavior.
            ordinals.TryAdd(record.GetName(ordinal), ordinal);
        }

        return Build(modelType, ordinals, strictness, prefix);
    }

    private static MerlinOrdinalMap Build(
        Type modelType,
        IReadOnlyDictionary<string, int> ordinals,
        MappingStrictness strictness,
        string prefix)
    {
        var metadata = MerlinMetaCache.Get(modelType);
        var entries = new List<Entry>(metadata.MappedProperties.Count);

        foreach (var property in metadata.MappedProperties.Values)
        {
            if (property.IsMerlinObject)
            {
                var nestedMap = SupportsOrdinalMapping(property.PropertyType)
                    ? Build(property.PropertyType, ordinals, strictness, property.MerlinPrefix)
                    : null;

                if (nestedMap != null && nestedMap.Entries.Length == 0)
                {
                    if (strictness == MappingStrictness.Validated && property.IsRequired)
                    {
                        throw CreateMissingColumnException(modelType, property.PropertyName);
                    }

                    continue;
                }

                entries.Add(new Entry(property, -1, string.Empty, nestedMap));
                continue;
            }

            var columnName = prefix + property.ColumnName;
            if (!ordinals.TryGetValue(columnName, out var ordinal))
            {
                if (strictness == MappingStrictness.Strict ||
                    strictness == MappingStrictness.Validated && property.IsRequired)
                {
                    throw CreateMissingColumnException(modelType, columnName);
                }

                continue;
            }

            entries.Add(new Entry(property, ordinal, columnName, null));
        }

        return new MerlinOrdinalMap(
            entries.ToArray(),
            ordinals,
            strictness,
            prefix,
            metadata.HasBeforeAutoPopulateHook,
            metadata.HasAfterAutoPopulateHook);
    }

    private static MerlinMissingColumnException CreateMissingColumnException(Type modelType, string columnName)
    {
        return new MerlinMissingColumnException(
            "MERLIN-MAP-1028",
            modelType.GetFriendlyName(),
            columnName,
            new IndexOutOfRangeException($"Column '{columnName}' was not present in the result set."));
    }
}
