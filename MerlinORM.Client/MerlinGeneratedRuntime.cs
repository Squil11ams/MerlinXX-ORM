using System.ComponentModel;
using System.Data;

namespace MerlinORM.Client;

/// <summary>Runtime coordination used by compiler-generated mapping code.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MerlinGeneratedRuntime
{
    /// <summary>Creates and populates a generated model, including lifecycle hooks.</summary>
    public static object CreateAndPopulate(
        IMerlinGeneratedMapper mapper,
        IDataReader data,
        MerlinGeneratedMappingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(plan);

        var model = mapper.Create(data, plan);

        if (model is MerlinModelBase merlinModel)
        {
            merlinModel.ApplyGeneratedMapping(data, mapper, plan);
        }
        else
        {
            mapper.Populate(model, data, plan);
        }

        return model;
    }
}
