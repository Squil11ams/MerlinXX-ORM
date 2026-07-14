using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public sealed class MerlinTypeMetadata
    {
        public IReadOnlyDictionary<string, MerlinPropertyMetadata> MappedProperties { get; }

        public MerlinTypeMetadata(IReadOnlyDictionary<string, MerlinPropertyMetadata> mappedProps)
        {
            MappedProperties = mappedProps;
        }
    }
}
