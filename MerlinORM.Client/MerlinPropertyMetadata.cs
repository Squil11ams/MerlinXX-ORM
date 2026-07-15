using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MerlinORM.Client
{
    public sealed class MerlinPropertyMetadata
    {
        public string ColumnName { get; init; }

        public string PropertyName { get; init; }

        public Type PropertyType { get; init; }

        public bool ThrowError { get; init; }

        public bool IsMerlinObject { get; private set; }

        public string MerlinPrefix { get; init; }

        public Func<object>? MerlinFactory { get; init; }

        public string? DefaultValue { get; init; }

        public Action<object, object?>? Setter { get; init; }

        public Func<object?, object?> Converter { get; init; } = default!;


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

        private static Func<object> CreateFactory(Type type)
        {
            var constructor = Expression.New(type);

            return Expression.Lambda<Func<object>>(
                Expression.Convert(constructor, typeof(object))
            ).Compile();
        }

        
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
                        $"Cannot assign NULL to non-nullable type {targetType.Name}");
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
