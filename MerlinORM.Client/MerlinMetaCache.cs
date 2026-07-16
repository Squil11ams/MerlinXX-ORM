using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Maintains cached type metadata used during object hydration and property mapping.
    /// Metadata is generated once per model type and reused for subsequent operations.
    /// </summary>
    public class MerlinMetaCache
    {

        /// <summary>
        /// Stores generated <see cref="MerlinTypeMetadata"/> instances keyed by model <see cref="Type"/>.
        /// Each type is processed once and reused for subsequent mapping operations.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MerlinTypeMetadata> _cache = new();



        /// <summary>
        /// Retrieves the metadata definition for the specified model type.
        /// If metadata has not previously been generated, it is created and added to the cache.
        /// </summary>
        /// <param name="type">
        /// The model type for which metadata is requested.
        /// </param>
        /// <returns>
        /// The cached <see cref="MerlinTypeMetadata"/> associated with the specified type.
        /// </returns>
        public static MerlinTypeMetadata Get(Type type)
        {
            return _cache.GetOrAdd(type, BuildMetadata);
        }



        /// <summary>
        /// Builds metadata for the specified model type.
        /// </summary>
        /// <remarks>
        /// Property discovery is performed from the derived type to the base type.
        /// This ensures derived properties take precedence when a property name is
        /// shared across an inheritance hierarchy.
        /// 
        /// Fixes issue #1: Overridden properties in derived classes were previously
        /// overwritten by inherited base class properties during metadata generation.
        /// </remarks>
        /// <param name="type">The model type to generate metadata for.</param>
        /// <returns>
        /// A <see cref="MerlinTypeMetadata"/> containing the mapped property metadata
        /// for the specified type.
        /// </returns>
        private static MerlinTypeMetadata BuildMetadata(Type type)
        {
            var mappedProperties = new Dictionary<string, MerlinPropertyMetadata>();

            for (Type? current = type;  current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var prop in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!prop.CanWrite)
                    {
                        continue;
                    }

                    if (mappedProperties.ContainsKey(prop.Name))
                    {
                        continue;
                    }

                    var map = BuildPropertyMeta(prop);

                    if (map != null)
                    {
                        mappedProperties.Add(prop.Name, map);
                    }
                }
            }

            return new MerlinTypeMetadata(mappedProperties);
        }



        /// <summary>
        /// Creates a metadata definition for a mapped model property.
        /// </summary>
        /// <param name="prop">Property to process</param>
        /// <returns>
        /// A <see cref="MerlinPropertyMetadata"/> instance if the property is mapped;
        /// otherwise <c>null</c> when the property is marked with <see cref="Exclude"/>.
        /// </returns>
        private static MerlinPropertyMetadata? BuildPropertyMeta(PropertyInfo prop)
        {
            if (prop.IsDefined(typeof(Exclude)))
                return null;

            var autoPop = prop.GetCustomAttribute<AutoPopSettings>();
            var merlinObject = prop.GetCustomAttribute<MerlinObject>();
            var isMerlinObject = prop.GetCustomAttribute<MerlinObject>() != null;

            return new MerlinPropertyMetadata(prop.PropertyType, isMerlinObject)
            {
                PropertyName = prop.Name,
                ColumnName = autoPop?.key ?? prop.Name,
                PropertyType = prop.PropertyType,
                ThrowError = autoPop?.throwError ?? true,
                DefaultValue = autoPop?.defaultValue,
                MerlinPrefix = merlinObject?.prefix ?? "",
                Setter = CreateSetter(prop)
            };
        }

        /// <summary>
        /// Creates a compiled setter delegate for the specified property.
        /// </summary>
        /// <param name="property">
        /// The property for which to generate a setter.
        /// </param>
        /// <returns>
        /// An <see cref="Action{T1,T2}"/> delegate that assigns a value to the property.
        /// The first parameter is the target object instance, and the second parameter
        /// is the value to assign.
        /// </returns>
        /// <remarks>
        /// This method uses expression trees to compile a strongly-typed property setter
        /// into a reusable delegate. This avoids the overhead of repeated reflection-based
        /// calls to <see cref="PropertyInfo.SetValue(object, object?)"/> during object mapping.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="property"/> is null.
        /// </exception>
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