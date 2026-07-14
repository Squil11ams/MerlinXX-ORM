using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MerlinORM.Client
{
    [DataContract]
    public class MerlinModelBase : IMerlinObject
    {
        #region FIELDS
        protected MerlinTypeMetadata Metadata =>
                    MerlinMetaCache.Get(GetType());
        #endregion

        #region CONSTRUCTOR
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
                    throw new MerlinMissingColumnException(this.GetType().Name, columnName, e);
                }

                SetProperty(prop, columnName, sourceValue);
            }
        }

        /// <summary>
        /// Set the individual property, attempts to use Meta's Converter, to matchup types.
        /// </summary>
        /// <param name="data">Data row from database.</param>
        /// <param name="prop">Property in model being set.</param>
        /// <param name="columnName">Column name used to pull data.</param>
        /// <param name="sourceValue">Actual value from data row.</param>
        /// <exception cref="MerlinMappingException"></exception>
        private void SetProperty(MerlinPropertyMetadata prop, string columnName, object? sourceValue)
        {
            try
            {
                var val = prop.Converter(sourceValue);
                prop.Setter(this, val);
            }
            catch (Exception ex)
            {
                if (prop.ThrowError)
                {
                    var SourceType = sourceValue == null ? "NULL" : sourceValue.GetType().Name;

                    throw new MerlinMappingException(this, prop, columnName, SourceType, ex);
                }

                SetPropertyFallback(prop, columnName, sourceValue, ex);
            }
        }

        /// <summary>
        /// Incase SetProperty fails, and model is set to not throw exception. Attempt to set to DefaultValue. Throws an exception if this fails.
        /// </summary>
        /// <param name="data">Data row from database.</param>
        /// <param name="prop">Property in model being set.</param>
        /// <param name="columnName">Column name used to pull data.</param>
        /// <param name="sourceValue">Actual value from data row.</param>
        /// <exception cref="MerlinMappingException"></exception>
        private void SetPropertyFallback(MerlinPropertyMetadata prop, string columnName, object? sourceValue, Exception originalException)
        {
            try
            {
                var fallback = prop.Converter(prop.DefaultValue);
                prop.Setter(this, fallback);
            }
            catch (Exception lastChanceEx)
            {
                var SourceType = sourceValue == null ? "NULL" : sourceValue.GetType().Name;

                throw new MerlinMappingException(this, prop, columnName, SourceType, lastChanceEx, prop.DefaultValue, originalException);
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
                throw new MerlinException(
                    $"No factory defined for Merlin object '{prop.PropertyName}'.");
            }

            var instance = prop.MerlinFactory();

            if (instance is not IMerlinObject child)
                throw new MerlinException(
                    $"{prop.PropertyName} is not a valid Merlin object.");

            child.SetDataObject(data, prop.MerlinPrefix);

            prop.Setter(this, instance);
        }
    }
}
