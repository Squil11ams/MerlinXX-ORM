using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Text;

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

        public virtual void SetDataObject(IDataReader data, string prefix = "")
        {
            foreach (var prop in Metadata.MappedProperties.Values)
            {

                if (prop.IsMerlinObject)
                {
                    var instance = prop.MerlinFactory();

                    if (instance is IMerlinObject)
                    {
                        var temp = (IMerlinObject)instance;
                        temp.SetDataObject(data, prop.MerlinPrefix);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Property '{prop.PropertyName}' is marked as a Merlin object but does not implement IMerlinObject.");
                    }

                    prop.Setter(this, instance);
                }
                else
                {
                    var columnName = prefix + prop.ColumnName;
                    object? value = data[columnName];

                    try
                    {
                        var val = prop.Converter(value);

                        prop.Setter(this, val);
                    }
                    catch(Exception ex)
                    {
                        if(prop.ThrowError)
                        {
                            throw new MerlinMappingException(this, prop, "Col", "Col:Type", ex);
                        }

                        try
                        {
                            prop.Setter(this, prop.DefaultValue);
                        }
                        catch (Exception BackupFailedEx)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("Failed to set original property, and fallback default also failed!");
                            sb.AppendLine($"Error setting property '{prop.PropertyName}' from column '{prefix + prop.ColumnName}'");
                            sb.AppendLine(BackupFailedEx.Message);
                            throw new Exception(sb.ToString(), ex);
                        }
                    }
                }
            }
        }


        public virtual void SetDataObject2(IDataReader data, string prefix = "")
        {
            foreach (var prop in Metadata.MappedProperties.Values)
            {
                if (prop.IsMerlinObject)
                {
                    var instance = prop.MerlinFactory!();

                    if (instance is not IMerlinObject child)
                        throw new MerlinMappingException(
                            $"{prop.PropertyName} is not a valid Merlin object.");

                    child.SetDataObject(data, prop.MerlinPrefix);

                    prop.Setter(this, instance);
                    continue;
                }

                var columnName = prefix + prop.ColumnName;

                try
                {
                    object? value = data[columnName];

                    value = prop.Converter(value);

                    prop.Setter(this, value);
                }
                catch (Exception ex)
                {
                    if (prop.ThrowError)
                    {
                        throw new MerlinMappingException( this, prop,"Col", "Col:Type", ex);
                    }

                    prop.Setter(this, prop.DefaultValue);
                }
            }
        }
    }
}
