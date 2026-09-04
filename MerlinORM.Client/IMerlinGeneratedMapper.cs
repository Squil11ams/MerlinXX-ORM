using System.Data;

namespace MerlinORM.Client;

/// <summary>Runtime contract implemented by compile-time generated model mappers.</summary>
public interface IMerlinGeneratedMapper
{
    /// <summary>Whether this mapper and all required nested mappers are registered.</summary>
    bool CanMap { get; }

    /// <summary>The model type populated by this mapper.</summary>
    Type ModelType { get; }

    /// <summary>Builds a mapping plan for the current result-set shape.</summary>
    MerlinGeneratedMappingPlan CreatePlan(
        IDataRecord schema,
        MappingStrictness strictness,
        string prefix = "");

    /// <summary>Creates a model instance, including constructor-bound values when required.</summary>
    object Create(IDataRecord data, MerlinGeneratedMappingPlan plan);

    /// <summary>Populates a model using generated assignments and a prepared plan.</summary>
    void Populate(object target, IDataRecord data, MerlinGeneratedMappingPlan plan);
}
