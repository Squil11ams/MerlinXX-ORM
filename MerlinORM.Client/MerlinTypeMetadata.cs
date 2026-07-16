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

        /// <summary>
        /// Creates instance of the TypeMetadata
        /// </summary>
        /// <param name="mappedProps"></param>
        public MerlinTypeMetadata(IReadOnlyDictionary<string, MerlinPropertyMetadata> mappedProps)
        {
            MappedProperties = mappedProps;
        }
    }
}
