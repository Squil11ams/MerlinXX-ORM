namespace MerlinORM.Client;

/// <summary>Conversion helpers used by runtime and generated Merlin mappers.</summary>
public static class MerlinConvert
{
    /// <summary>Converts a database value to the requested model-property type.</summary>
    public static T? To<T>(object? value)
    {
        // Provider values commonly already have the requested CLR type. Keep that
        // generated hot path to one type test and unbox/cast operation.
        if (value is T typedValue)
        {
            return typedValue;
        }

        return (T?)ConverterCache<T>.Converter(value);
    }

    private static class ConverterCache<T>
    {
        public static readonly Func<object?, object?> Converter =
            MerlinPropertyMetadata.CreateConverter(typeof(T));
    }
}
