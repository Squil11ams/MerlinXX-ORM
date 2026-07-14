using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public sealed class MerlinOptions
    {
        /// <summary>
        /// When true, Merlin continues mapping after individual property failures
        /// and throws a MerlinAggregateMappingException containing all errors.
        /// Default: false.
        /// </summary>
        public bool CollectMappingErrors { get; init; }

        /// <summary>
        /// When true, Merlin caches column ordinals for the IDataReader.
        /// Default: false.
        /// </summary>
        public bool CacheOrdinals { get; init; }
    }
}
