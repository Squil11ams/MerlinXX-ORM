namespace MerlinORM.Client;

/// <summary>Controls how model properties are matched to a query result set.</summary>
public enum MappingStrictness
{
    /// <summary>Default. Every mapped property must exist in the result set.</summary>
    Strict,

    /// <summary>Populates available columns and ignores properties whose columns are missing.</summary>
    Projection,

    /// <summary>Requires only properties marked with <see cref="MerlinRequired"/>.</summary>
    Validated
}
