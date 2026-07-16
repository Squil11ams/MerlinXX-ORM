using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Metadata used by the mapping system to build and set properties automatically.
    /// </summary>
    public sealed class MerlinPropertyMetadata
    {
        /// <summary>
        /// Database column to pull from.
        /// </summary>
        public required string ColumnName { get; init; }

        /// <summary>
        /// Property name in class to set
        /// </summary>
        public required string PropertyName { get; init; }

        /// <summary>
        /// Properties Type for Cast/Converting
        /// </summary>
        public required Type PropertyType { get; init; }

        /// <summary>
        /// Throw error if cast/convert fails, or try default value
        /// </summary>
        public bool ThrowError { get; init; } = true;

        /// <summary>
        /// Indicates property is actually another merlin model, applies different logic to set.
        /// </summary>
        public bool IsMerlinObject { get; private set; }

        /// <summary>
        /// Prefix to apply to all properties within the merlin object. Empty if IsMerlinObject = false.
        /// </summary>
        public string MerlinPrefix { get; init; } = string.Empty;

        /// <summary>
        /// Factory used to create MerlinObject
        /// </summary>
        public Func<object>? MerlinFactory { get; init; }

        /// <summary>
        /// Default Value to use if ThrowError = false.
        /// </summary>
        public string? DefaultValue { get; init; }

        /// <summary>
        /// Action to set the property value
        /// </summary>
        public Action<object, object?>? Setter { get; init; }

        /// <summary>
        /// Action to convert the value from X to Y
        /// </summary>
        public Func<object?, object?> Converter { get; init; } = default!;


        /// <summary>
        /// Sets up a new instance and applies either MerlinFactory or Setter
        /// </summary>
        /// <param name="propType"></param>
        /// <param name="isMerlin"></param>
        public MerlinPropertyMetadata(Type propType, bool isMerlin = false)
        {
            IsMerlinObject = isMerlin;

            if (isMerlin)
            {
                MerlinFactory = CreateFactory(propType);
            }
            else
            {
                Converter = CreateConverter(propType);
            }
        }

        /// <summary>
        /// Private Merlin Factory Creation
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static Func<object> CreateFactory(Type type)
        {
            var constructor = Expression.New(type);

            return Expression.Lambda<Func<object>>(
                Expression.Convert(constructor, typeof(object))
            ).Compile();
        }

        
        /// <summary>
        /// Builds Converter Method
        /// TODO: Should probably be moved to a new class MerlinConverter better seperation of duties since other things use this as well.
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        /// <exception cref="MerlinException"></exception>
        public static Func<object?, object?> CreateConverter(Type targetType)
        {
            bool isNullable = Nullable.GetUnderlyingType(targetType) != null;

            Type actualType = Nullable.GetUnderlyingType(targetType)
                              ?? targetType;


            return value =>
            {
                // Database NULL
                if (value == null || value == DBNull.Value)
                {
                    if (isNullable || !actualType.IsValueType)
                        return null;

                    throw new MerlinException("MERLIN-CVT-1033",
                        $"Cannot assign NULL to non-nullable type {targetType.GetFriendlyName()}");
                }


                // Already correct type
                if (actualType.IsAssignableFrom(value.GetType()))
                    return value;


                // Enum handling
                if (actualType.IsEnum)
                {
                    if (value is string text)
                        return Enum.Parse(actualType, text);

                    return Enum.ToObject(actualType, value);
                }


                // New Date types
                if (actualType == typeof(DateOnly))
                {
                    if (value is DateTime dt)
                        return DateOnly.FromDateTime(dt);

                    return DateOnly.Parse(value.ToString()!);
                }


                if (actualType == typeof(TimeOnly))
                {
                    if (value is TimeSpan ts)
                        return TimeOnly.FromTimeSpan(ts);

                    return TimeOnly.Parse(value.ToString()!);
                }


                // Normal conversions
                return Convert.ChangeType(value, actualType);
            };
        }
    }
}
