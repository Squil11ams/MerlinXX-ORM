using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinMappingException : MerlinException
    {
        public string TargetObject { get; init; }

        public string? TargetProperty { get; init; }

        public string? SourceColumn { get; init; }

        public string? TargetType {  get; init; }

        public string? SourceType {  get; init; }

        public string? DefaultValue { get; init; }

        public Exception? OtherException { get; init; }

        public MerlinMappingException(string ErrorCode, string targetType, string sourceColumn, string sourceType, Exception inner) 
            : base(inner)
        {
            TargetProperty = targetProperty;
            SourceColumn = sourceColumn;
            TargetType = targetType;
            SourceType = sourceType;
        }


        public MerlinMappingException(string targetProperty, string sourceColumn, Exception inner)
            : base(inner)
        {
            TargetProperty = targetProperty;
            SourceColumn = sourceColumn;
            TargetType = null;
            SourceType = null;
        }

        public MerlinMappingException(MerlinPropertyMetadata meta, string sourceColumn, string sourceType, Exception inner)
            : base(inner)
        {
            TargetProperty = meta.PropertyName;
            SourceColumn = sourceColumn;
            TargetType = meta.PropertyType.Name;
            SourceType = sourceType;
        }

        public MerlinMappingException(object targetObject, MerlinPropertyMetadata meta, string sourceColumn, string sourceValue, Exception inner) : base(inner)
        {
            TargetObject = targetObject.GetType().Name;
            TargetProperty = meta.PropertyName;
            SourceColumn = sourceColumn;
            TargetType = meta.PropertyType.Name;
            SourceType = sourceValue;
        }

        public MerlinMappingException(object targetObject, MerlinPropertyMetadata meta, string sourceColumn, string sourceValue, Exception inner, string defaultValue, Exception otherException) : base(inner)
        {
            TargetObject = targetObject.GetType().Name;
            TargetProperty = meta.PropertyName;
            SourceColumn = sourceColumn;
            TargetType = meta.PropertyType.Name;
            SourceType = sourceValue;
            DefaultValue = defaultValue;
            OtherException = otherException;
        }
    }
}
