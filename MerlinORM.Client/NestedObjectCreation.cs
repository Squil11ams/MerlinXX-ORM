namespace MerlinORM.Client;

/// <summary>Controls when a property marked with <see cref="MerlinObject"/> is instantiated.</summary>
public enum NestedObjectCreation
{
    /// <summary>Current and default behavior: always instantiate and map the nested object.</summary>
    Always,

    /// <summary>Instantiate only when at least one mapped nested column is not database NULL.</summary>
    WhenAnyColumnHasValue
}
