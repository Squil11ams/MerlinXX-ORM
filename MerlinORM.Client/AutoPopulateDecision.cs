namespace MerlinORM.Client;

/// <summary>Controls whether Merlin's automatic property population runs for the current row.</summary>
public enum AutoPopulateDecision
{
    /// <summary>Continue with automatic ordinal-based property population.</summary>
    Continue,

    /// <summary>Skip automatic property population for this model and row.</summary>
    Skip
}
