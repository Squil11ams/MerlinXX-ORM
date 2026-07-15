using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinMetaCache
    {
        private static readonly ConcurrentDictionary<Type, MerlinTypeMetadata> _cache = new();

        public static MerlinTypeMetadata Get(Type type)
        {
            return _cache.GetOrAdd(type, BuildMetadata);
        }

        private static MerlinTypeMetadata BuildMetadata(Type type)
        {
            var mappedProperties = new Dictionary<string, MerlinPropertyMetadata>();

            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanWrite) continue;

                var map = BuildPropertyMeta(type, prop);

                if (map != null)
                {
                    mappedProperties[prop.Name] = map;
                }
            }
            Console.WriteLine("=======================================================");

            foreach (var p in mappedProperties)
            {
                Console.WriteLine($"{p.Key} : {p.Value.PropertyName} {p.Value.PropertyType} {p.Value.IsMerlinObject}");
            }

            return new MerlinTypeMetadata(mappedProperties);
        }

        private static MerlinPropertyMetadata BuildPropertyMeta(Type type, PropertyInfo prop)
        {
            if (prop.IsDefined(typeof(Exclude)))
                return null;

            var autoPop = prop.GetCustomAttribute<AutoPopSettings>();
            var merlinObject = prop.GetCustomAttribute<MerlinObject>();
            var isMerlinObject = prop.GetCustomAttribute<MerlinObject>() != null;

            Console.WriteLine($"{prop.Name} - MerlinObject: {isMerlinObject}");

            var meta = new MerlinPropertyMetadata(prop.PropertyType, isMerlinObject)
            {
                PropertyName = prop.Name,
                ColumnName = autoPop?.key ?? prop.Name,
                PropertyType = prop.PropertyType,
                ThrowError = autoPop?.throwError ?? true,
                DefaultValue = autoPop?.defaultValue,
                MerlinPrefix = merlinObject?.prefix ?? "",
                Setter = CreateSetter(prop)
            };

            Console.WriteLine($"{meta.PropertyName}: IsMerlinBool: {isMerlinObject} IsMerlinObject={meta.IsMerlinObject}, Factory={meta.MerlinFactory}");

            return meta;
        }


        private static Action<object, object?> CreateSetter(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object));
            var value = Expression.Parameter(typeof(object));

            var body = Expression.Assign(
                Expression.Property(
                    Expression.Convert(instance, property.DeclaringType!),
                    property),
                Expression.Convert(value, property.PropertyType));

            return Expression.Lambda<Action<object, object?>>(
                body,
                instance,
                value)
                .Compile();
        }
    }
}