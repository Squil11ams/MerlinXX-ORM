using System.Data;

namespace MerlinORM.Client;

/// <summary>Prepared result-set ordinals used by a generated model mapper.</summary>
public sealed class MerlinGeneratedMappingPlan : IMerlinMappingPlan
{
    private readonly int[] _ordinals;
    private readonly IReadOnlyDictionary<string, int> _columnOrdinals;
    private readonly Dictionary<int, MerlinGeneratedNestedPlan> _nestedPlans = new();
    private readonly bool _hasBeforeHook;
    private readonly bool _hasAfterHook;

    /// <summary>The prefix applied to model column names.</summary>
    public string Prefix { get; }

    /// <summary>The query's missing-column policy.</summary>
    public MappingStrictness Strictness { get; }

    bool IMerlinMappingPlan.HasBeforeAutoPopulateHook => _hasBeforeHook;

    bool IMerlinMappingPlan.HasAfterAutoPopulateHook => _hasAfterHook;

    private MerlinGeneratedMappingPlan(
        int[] ordinals,
        IReadOnlyDictionary<string, int> columnOrdinals,
        MappingStrictness strictness,
        string prefix,
        bool hasBeforeHook,
        bool hasAfterHook)
    {
        _ordinals = ordinals;
        _columnOrdinals = columnOrdinals;
        Strictness = strictness;
        Prefix = prefix;
        _hasBeforeHook = hasBeforeHook;
        _hasAfterHook = hasAfterHook;
    }

    /// <summary>Creates a plan for generated property descriptors.</summary>
    public static MerlinGeneratedMappingPlan Create(
        IDataRecord schema,
        IReadOnlyList<string?> columnNames,
        IReadOnlyList<bool> requiredProperties,
        MappingStrictness strictness,
        string prefix,
        string modelName,
        bool hasBeforeHook,
        bool hasAfterHook)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(columnNames);
        ArgumentNullException.ThrowIfNull(requiredProperties);

        if (columnNames.Count != requiredProperties.Count)
        {
            throw new ArgumentException("Generated mapping descriptors must have matching lengths.");
        }

        var available = new Dictionary<string, int>(schema.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var ordinal = 0; ordinal < schema.FieldCount; ordinal++)
        {
            available.TryAdd(schema.GetName(ordinal), ordinal);
        }

        var ordinals = new int[columnNames.Count];
        for (var index = 0; index < columnNames.Count; index++)
        {
            if (columnNames[index] is null)
            {
                ordinals[index] = -1;
                continue;
            }

            var columnName = prefix + columnNames[index];
            if (available.TryGetValue(columnName, out var ordinal))
            {
                ordinals[index] = ordinal;
                continue;
            }

            ordinals[index] = -1;
            if (strictness == MappingStrictness.Strict ||
                strictness == MappingStrictness.Validated && requiredProperties[index])
            {
                throw new MerlinMissingColumnException(
                    "MERLIN-MAP-1028",
                    modelName,
                    columnName,
                    new IndexOutOfRangeException($"Column '{columnName}' was not present in the result set."));
            }
        }

        return new MerlinGeneratedMappingPlan(
            ordinals,
            available,
            strictness,
            prefix,
            hasBeforeHook,
            hasAfterHook);
    }

    /// <summary>Returns a generated property ordinal when its column is available.</summary>
    public bool TryGetPropertyOrdinal(int propertyIndex, out int ordinal)
    {
        ordinal = _ordinals[propertyIndex];
        return ordinal >= 0;
    }

    /// <summary>Adds a recursively generated nested mapping plan.</summary>
    public void AddNestedPlan(
        int propertyIndex,
        IMerlinGeneratedMapper mapper,
        MerlinGeneratedMappingPlan plan,
        NestedObjectCreation creation,
        bool required)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(plan);

        if (Strictness == MappingStrictness.Validated && required && !plan.HasMappedColumns)
        {
            throw new MerlinMissingColumnException(
                "MERLIN-MAP-1028",
                mapper.ModelType.Name,
                mapper.ModelType.Name,
                new IndexOutOfRangeException("No columns were available for the required nested model."));
        }

        _nestedPlans[propertyIndex] = new MerlinGeneratedNestedPlan(mapper, plan, creation);
    }

    /// <summary>Gets the generated plan associated with a nested property.</summary>
    public bool TryGetNestedPlan(int propertyIndex, out MerlinGeneratedNestedPlan nestedPlan) =>
        _nestedPlans.TryGetValue(propertyIndex, out nestedPlan);

    /// <summary>Whether the plan contains at least one available scalar column.</summary>
    public bool HasMappedColumns
    {
        get
        {
            foreach (var ordinal in _ordinals)
            {
                if (ordinal >= 0)
                {
                    return true;
                }
            }

            foreach (var nested in _nestedPlans.Values)
            {
                if (nested.Plan.HasMappedColumns)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Whether any mapped scalar value in this plan tree is not database NULL.</summary>
    public bool HasAnyValue(IDataRecord data)
    {
        foreach (var ordinal in _ordinals)
        {
            if (ordinal >= 0 && !data.IsDBNull(ordinal))
            {
                return true;
            }
        }

        foreach (var nested in _nestedPlans.Values)
        {
            if (nested.Plan.HasAnyValue(data))
            {
                return true;
            }
        }

        return false;
    }

    bool IMerlinMappingPlan.TryGetOrdinal(string columnName, out int ordinal) =>
        _columnOrdinals.TryGetValue(columnName, out ordinal);

}
