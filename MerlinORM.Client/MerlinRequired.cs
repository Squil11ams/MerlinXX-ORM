namespace MerlinORM.Client;

/// <summary>
/// Marks a mapped property as required when a query uses
/// <see cref="MappingStrictness.Validated"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MerlinRequired : Attribute
{
}
