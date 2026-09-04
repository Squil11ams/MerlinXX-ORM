namespace MerlinORM.Client;

/// <summary>Generated mapping information for one nested model property.</summary>
public readonly struct MerlinGeneratedNestedPlan
{
    /// <summary>The generated mapper for the nested model.</summary>
    public IMerlinGeneratedMapper Mapper { get; }

    /// <summary>The prepared recursive result-set plan.</summary>
    public MerlinGeneratedMappingPlan Plan { get; }

    /// <summary>The configured nested creation behavior.</summary>
    public NestedObjectCreation Creation { get; }

    internal MerlinGeneratedNestedPlan(
        IMerlinGeneratedMapper mapper,
        MerlinGeneratedMappingPlan plan,
        NestedObjectCreation creation)
    {
        Mapper = mapper;
        Plan = plan;
        Creation = creation;
    }
}
