using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MerlinORM.Client
{
    /// <summary>
    /// Should be the base for all Merlin Data Models.
    /// Includes logic for the Mapping System
    /// </summary>
    [DataContract]
    public class MerlinModelBase : IMerlinObject
    {
        #region FIELDS
        /// <summary>
        /// Cached Metadata for the model.
        /// </summary>
        protected MerlinTypeMetadata Metadata =>
                    MerlinMetaCache.Get(GetType());
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Empty constructor to support serialization
        /// </summary>
        public MerlinModelBase() { }
        #endregion

        /// <summary>
        /// Attempts to populate model with data from database.
        /// </summary>
        /// <param name="data">Database row of data to fill model with.</param>
        /// <param name="prefix">Prefix used to alter column name to match dataset.</param>
        /// <exception cref="MerlinMissingColumnException"></exception>
        /// <exception cref="MerlinMappingException"></exception>
        /// <exception cref="MerlinException"></exception>
        public virtual void SetDataObject(IDataReader data, string prefix = "")
        {
            foreach (var prop in Metadata.MappedProperties.Values)
            {
                if (prop.IsMerlinObject)
                {
                    PopulateNestedObject(data, prop);
                    continue;
                }

                var columnName = prefix + prop.ColumnName;
                object? sourceValue;

                try
                {
                    sourceValue = data[columnName];
                }
                catch(Exception e)
                {
                    throw new MerlinMissingColumnException("MERLIN-MAP-1028", this.GetType().GetFriendlyName(), columnName, e);
                }

                SetProperty(prop, columnName, sourceValue);
            }
        }

        /// <summary>
        /// Set the individual property, attempts to use Meta's Converter, to matchup types.
        /// </summary>
        /// <param name="prop">Property in model being set.</param>
        /// <param name="columnName">Column name used to pull data.</param>
        /// <param name="sourceValue">Actual value from data row.</param>
        /// <exception cref="MerlinMappingException"></exception>
        private void SetProperty(MerlinPropertyMetadata prop, string columnName, object? sourceValue)
        {
            try
            {
                var val = prop.Converter(sourceValue);
                
                if (prop.Setter == null)
                {
                    throw new MerlinException(
                        "MERLIN-MAP-1035",
                        $"'{this.GetType().Name}.{prop.PropertyName}' does not have a setter.");
                }

                prop.Setter(this, val);
            }
            catch (Exception ex)
            {
                if (prop.ThrowError)
                {
                    var SourceType = GetSourceType(sourceValue);

                    var msg = $"'{this.GetType().Name}' failed to map property '{prop.PropertyName}:{prop.PropertyType.GetFriendlyName()}' from '{columnName}:{SourceType}'";

                    throw new MerlinMappingException("MERLIN-MAP-1029", msg, ex);
                }

                SetPropertyFallback(prop, columnName, sourceValue, ex);
            }
        }
        
        /// <summary>
        /// Checks if value is null, return string "Null" otherwise get Type.GetFriendlyName()
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetSourceType(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }

            return value.GetType().GetFriendlyName();
        }

        /// <summary>
        /// Incase SetProperty fails, and model is set to not throw exception. Attempt to set to DefaultValue. Throws an exception if this fails.
        /// </summary>
        /// <param name="prop">Property in model being set.</param>
        /// <param name="columnName">Column name used to pull data.</param>
        /// <param name="sourceValue">Actual value from data row.</param>
        /// <param name="originalException">Exception that triggered the Fallback System</param>
        /// <exception cref="MerlinMappingException"></exception>
        private void SetPropertyFallback(MerlinPropertyMetadata prop, string columnName, object? sourceValue, Exception originalException)
        {
            try
            {
                var fallback = prop.Converter(prop.DefaultValue);
                
                if (prop.Setter == null)
                {
                    throw new MerlinException(
                        "MERLIN-MAP-1036",
                        $"'{this.GetType().Name}.{prop.PropertyName}' does not have a setter.");
                }

                prop.Setter(this, fallback);
            }
            catch (Exception lastChanceEx)
            {
                var SourceType = GetSourceType(sourceValue);

                var msg = $"'{this.GetType().Name}' failed to map property '{prop.PropertyName}:{prop.PropertyType.GetFriendlyName()}' from '{columnName}:{SourceType}'{Environment.NewLine}Fallback failed to set to default value '{prop.DefaultValue}'";

                throw new MerlinMappingException("MERLIN-MAP-1030", msg,lastChanceEx, originalException);
            }
        }

        /// <summary>
        /// Attempts to load property as a nested MerlinObject
        /// </summary>
        /// <param name="data">Data row from database.</param>
        /// <param name="prop">Property in model being set.</param>
        /// <exception cref="MerlinException"></exception>
        private void PopulateNestedObject(IDataReader data, MerlinPropertyMetadata prop)
        {
            if (prop.MerlinFactory == null)
            {
                throw new MerlinException("MERLIN-MAP-1031",
                    $"No factory defined for Merlin object '{prop.PropertyName}'.");
            }

            var instance = prop.MerlinFactory();

            if (instance is not IMerlinObject child)
                throw new MerlinException("MERLIN-MAP-1032",
                    $"{prop.PropertyName} is not a valid Merlin object.");

            child.SetDataObject(data, prop.MerlinPrefix);

            if (prop.Setter == null)
            {
                throw new MerlinException(
                    "MERLIN-MAP-1037",
                    $"'{this.GetType().Name}.{prop.PropertyName}' does not have a setter.");
            }

            prop.Setter(this, instance);
        }
    }
}
