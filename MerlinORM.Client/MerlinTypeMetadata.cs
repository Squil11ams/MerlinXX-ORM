using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Object used by the <see cref="MerlinMetaCache"/>
    /// Reresents that actual cached data.
    /// </summary>
    public sealed class MerlinTypeMetadata
    {
        /// <summary>
        /// List of properties cached for the type.
        /// </summary>
        public IReadOnlyDictionary<string, MerlinPropertyMetadata> MappedProperties { get; }

        /// <summary>Whether the model overrides the before-population lifecycle hook.</summary>
        public bool HasBeforeAutoPopulateHook { get; }

        /// <summary>Whether the model overrides the after-population lifecycle hook.</summary>
        public bool HasAfterAutoPopulateHook { get; }

        /// <summary>
        /// Creates instance of the TypeMetadata
        /// </summary>
        /// <param name="mappedProps"></param>
        /// <param name="hasBeforeAutoPopulateHook">Whether the before-population hook is overridden.</param>
        /// <param name="hasAfterAutoPopulateHook">Whether the after-population hook is overridden.</param>
        public MerlinTypeMetadata(
            IReadOnlyDictionary<string, MerlinPropertyMetadata> mappedProps,
            bool hasBeforeAutoPopulateHook = false,
            bool hasAfterAutoPopulateHook = false)
        {
            MappedProperties = mappedProps;
            HasBeforeAutoPopulateHook = hasBeforeAutoPopulateHook;
            HasAfterAutoPopulateHook = hasAfterAutoPopulateHook;
        }
    }
}
