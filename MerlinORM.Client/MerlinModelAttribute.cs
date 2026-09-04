namespace MerlinORM.Client;

/// <summary>Opts a class or record that does not derive from <see cref="MerlinModelBase"/> into generated mapping.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MerlinModelAttribute : Attribute;
