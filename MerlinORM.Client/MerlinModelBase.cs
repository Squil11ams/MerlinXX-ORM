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
        [Obsolete(
            "SetDataObject is no longer an override point. " +
            "Override OnBeforeAutoPopulate or OnAfterAutoPopulate instead.")]
        public void SetDataObject(IDataReader data, string prefix = "")
        {
            ArgumentNullException.ThrowIfNull(data);
            var ordinalMap = MerlinOrdinalMap.Build(GetType(), data, MappingStrictness.Strict, prefix);
            SetDataObject(data, ordinalMap);
        }

        /// <summary>
        /// Populates this model using a precomputed result-set ordinal plan.
        /// </summary>
        internal void SetDataObject(IDataReader data, MerlinOrdinalMap ordinalMap)
        {
            MerlinMappingContext context = default;

            if (ordinalMap.HasBeforeAutoPopulateHook)
            {
                context = new MerlinMappingContext(data, ordinalMap);

                if (OnBeforeAutoPopulate(in context) == AutoPopulateDecision.Skip)
                {
                    return;
                }
            }

            foreach (var entry in ordinalMap.Entries)
            {
                if (entry.Property.IsMerlinObject)
                {
                    PopulateNestedObject(data, entry.Property, entry.NestedMap);
                    continue;
                }

                SetProperty(entry.Property, entry.ColumnName, data.GetValue(entry.Ordinal));
            }

            if (ordinalMap.HasAfterAutoPopulateHook)
            {
                if (!ordinalMap.HasBeforeAutoPopulateHook)
                {
                    context = new MerlinMappingContext(data, ordinalMap);
                }

                OnAfterAutoPopulate(in context);
            }
        }

        internal void SetDataObject(
            IDataReader data,
            IMerlinGeneratedMapper mapper,
            MerlinGeneratedMappingPlan mappingPlan)
        {
            var plan = (IMerlinMappingPlan)mappingPlan;
            MerlinMappingContext context = default;

            if (plan.HasBeforeAutoPopulateHook)
            {
                context = new MerlinMappingContext(data, plan);

                if (OnBeforeAutoPopulate(in context) == AutoPopulateDecision.Skip)
                {
                    return;
                }
            }

            mapper.Populate(this, data, mappingPlan);

            if (plan.HasAfterAutoPopulateHook)
            {
                if (!plan.HasBeforeAutoPopulateHook)
                {
                    context = new MerlinMappingContext(data, plan);
                }

                OnAfterAutoPopulate(in context);
            }
        }

        /// <summary>Applies a generated mapper while preserving model lifecycle hooks.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void ApplyGeneratedMapping(
            IDataReader data,
            IMerlinGeneratedMapper mapper,
            MerlinGeneratedMappingPlan mappingPlan)
        {
            SetDataObject(data, mapper, mappingPlan);
        }

        /// <summary>
        /// Runs before automatic property population. Return <see cref="AutoPopulateDecision.Skip"/>
        /// to leave the model unchanged for the current row.
        /// </summary>
        protected virtual AutoPopulateDecision OnBeforeAutoPopulate(in MerlinMappingContext context) =>
            AutoPopulateDecision.Continue;

        /// <summary>Runs after automatic property population completes successfully.</summary>
        protected virtual void OnAfterAutoPopulate(in MerlinMappingContext context)
        {
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

        private void PopulateNestedObject(
            IDataReader data,
            MerlinPropertyMetadata prop,
            MerlinOrdinalMap? ordinalMap)
        {
            if (prop.NestedObjectCreation == NestedObjectCreation.WhenAnyColumnHasValue &&
                (ordinalMap != null
                    ? !ordinalMap.HasAnyValue(data)
                    : !HasAnyNestedValue(data, prop.PropertyType, prop.MerlinPrefix)))
            {
                SetNestedProperty(prop, null);
                return;
            }

            if (prop.MerlinFactory == null)
            {
                throw new MerlinException("MERLIN-MAP-1031",
                    $"No factory defined for Merlin object '{prop.PropertyName}'.");
            }

            var instance = prop.MerlinFactory();

            if (instance is not IMerlinObject child)
            {
                throw new MerlinException("MERLIN-MAP-1032",
                    $"{prop.PropertyName} is not a valid Merlin object.");
            }

            if (ordinalMap != null && instance is MerlinModelBase model)
            {
                model.SetDataObject(data, ordinalMap);
            }
            else
            {
                child.SetDataObject(data, prop.MerlinPrefix);
            }

            if (prop.Setter == null)
            {
                throw new MerlinException(
                    "MERLIN-MAP-1037",
                    $"'{GetType().Name}.{prop.PropertyName}' does not have a setter.");
            }

            prop.Setter(this, instance);
        }

        private static bool HasAnyNestedValue(IDataReader data, Type modelType, string prefix)
        {
            foreach (var property in MerlinMetaCache.Get(modelType).MappedProperties.Values)
            {
                if (property.IsMerlinObject)
                {
                    if (HasAnyNestedValue(data, property.PropertyType, property.MerlinPrefix))
                    {
                        return true;
                    }

                    continue;
                }

                var columnName = prefix + property.ColumnName;

                try
                {
                    var value = data[columnName];
                    if (value != null && value != DBNull.Value)
                    {
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    throw new MerlinMissingColumnException(
                        "MERLIN-MAP-1028",
                        modelType.GetFriendlyName(),
                        columnName,
                        exception);
                }
            }

            return false;
        }

        private void SetNestedProperty(MerlinPropertyMetadata prop, object? value)
        {
            if (prop.Setter == null)
            {
                throw new MerlinException(
                    "MERLIN-MAP-1037",
                    $"'{GetType().Name}.{prop.PropertyName}' does not have a setter.");
            }

            prop.Setter(this, value);
        }
    }
}
