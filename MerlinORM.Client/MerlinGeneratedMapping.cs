namespace MerlinORM.Client;

/// <summary>Stores the generated mapper registered for a model type.</summary>
public static class MerlinGeneratedMapping<T>
{
    /// <summary>The generated mapper, or null when runtime mapping should be used.</summary>
    public static IMerlinGeneratedMapper? Mapper { get; private set; }

    /// <summary>Registers a compiler-generated mapper for this model type.</summary>
    public static void Register(IMerlinGeneratedMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        Mapper = mapper;
    }
}
