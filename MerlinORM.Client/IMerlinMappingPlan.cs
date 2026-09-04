namespace MerlinORM.Client;

internal interface IMerlinMappingPlan
{
    string Prefix { get; }

    MappingStrictness Strictness { get; }

    bool HasBeforeAutoPopulateHook { get; }

    bool HasAfterAutoPopulateHook { get; }

    bool TryGetOrdinal(string columnName, out int ordinal);
}
