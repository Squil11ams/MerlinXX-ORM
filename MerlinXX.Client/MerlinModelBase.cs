using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace MerlinXX.Client
{
    [DataContract]
    public class MerlinModelBase : IMerlinObject
    {
        #region CONSTRUCTOR
        public MerlinModelBase() { }
        #endregion

        #region MAIN FUNCTION - OVERRIDABLE
        public virtual void SetDataObject(IDataReader data, int flag = 0, string prefix = "")
        {
            // LOOP THROUGH EACH PROPERTY IN INHERITIED CLASS
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                // GET LIST OF ALL ATTRIBUTES TAGGED TO PROPERTY
                var attributes = prop.GetCustomAttributes(false);


                // CHECK IF ANY ATTRIBUTES ARE PRESENT ON PROPERTY
                if (attributes.Length > 0)
                {
                    // FOUND ATTRIBUTES - 
                    _ProcessAttributes(prop, attributes, data, flag, prefix);
                }
                else
                {
                    PopulateProperty(prop, data, flag, prefix);
                }
            }
        }
        #endregion

        #region MAIN FUNCTION HELPERS
        private void _ProcessAttributes(PropertyInfo property, object[] attributes, IDataReader data, int flag = 0, string prefix = "")
        {
            var results = _ScanAttributes(attributes);

            if (results.Item1 > 0) // ITEM IS EXCLUDED
            {
                // EXCLUDE DO NOTHING
            }
            else if (results.Item2 > 0) // ITEM HAS AUTOPOP SETTINGS
            {
                PopulateProperty_AutoPopSettings(property, data, flag, prefix);
            }
            else if (results.Item3 > 0) // ITEM IS MERLINOBJECT
            {
                PopulateProperty_MerlinObject(property, data, flag, results.Item4);
            }
            else // ITEM HAS NO VALID ATTRIBUTES
            {
                PopulateProperty(property, data, flag, prefix);
            }
        }

        private Tuple<int, int, int, MerlinObject> _ScanAttributes(object[] attributes)
        {
            int exclude = 0;
            int autoPop = 0;
            int sqlObject = 0;

            MerlinObject temp = null;

            foreach (var attribute in attributes)
            {
                if (attribute is Exclude) { exclude++; }
                if (attribute is AutoPopSettings) { autoPop++; }
                if (attribute is MerlinObject) { temp = (MerlinObject)attribute; sqlObject++; }
            }

            return new Tuple<int, int, int, MerlinObject>(exclude, autoPop, sqlObject, temp);
        }
        #endregion

        #region PROPERTY SETTERS
        private void PopulateProperty(PropertyInfo prop, IDataReader data, int Flag = 0, string Prefix = "")
        {
            try
            {
                /* RETURN IF NOT WRITABLE */
                if (!prop.CanWrite) { return; }

                var type = prop.PropertyType;

                var baseObjectType = this.GetType();
                var decodeMethod = baseObjectType.BaseType.GetMethod("CastAutoPrefix", BindingFlags.Instance | BindingFlags.NonPublic);
                var decodeMethodInvokable = decodeMethod.MakeGenericMethod(new Type[] { prop.PropertyType });

                // CALL DECODE
                var result = decodeMethodInvokable.Invoke(this, new object[] { data, prop.Name, Prefix });

                // SET PROPERTY TO DECODE RESULT
                prop.SetValue(this, result);
            }
            catch(MerlinException)
            {
                throw;
            }
            catch (Exception e)
            {
                Type obj = this.GetType();

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Error Populating Data Object");
                sb.AppendLine("Type: " + obj.ToString());
                sb.AppendLine("Property: " + prop.Name);

                throw new MerlinException("AC-011", sb.ToString(), e);
            }
        }

        private void PopulateProperty_AutoPopSettings(PropertyInfo prop, IDataReader data, int Flag = 0, string Prefix = "")
        {
            /* RETURN IF NOT WRITABLE */
            if (!prop.CanWrite) { return; }

            var AutoPop = prop.GetCustomAttribute<AutoPopSettings>();

            var type = prop.PropertyType;
            var x = this.GetType();
            var y = x.BaseType.GetMethod("Cast", BindingFlags.Instance | BindingFlags.NonPublic);
            var z = y.MakeGenericMethod(new Type[] { prop.PropertyType });

            var result = new Object();

            if (AutoPop.throwError)
            {
                result = z.Invoke(this, new object[] { data, AutoPop.key, true, null, Prefix });
            }
            else
            {
                result = z.Invoke(this, new object[] { data, AutoPop.key, AutoPop.throwError, AutoPop.defaultValue, Prefix });
            }

            prop.SetValue(this, result);
        }

        private void PopulateProperty_MerlinObject(PropertyInfo prop, IDataReader data, int Flag = 0, MerlinObject attr = null)
        {
            try
            {
                object instance = Activator.CreateInstance(prop.PropertyType);

                if (instance is IMerlinObject)
                {
                    var temp = (IMerlinObject)instance;
                    temp.SetDataObject(data, attr.flag, attr.prefix);
                }

                prop.SetValue(this, instance);
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Error Populating Property");
                sb.AppendLine("Property: " + prop.Name);
                sb.AppendLine("Type: " + prop.PropertyType.FullName);
                sb.AppendLine("");
                sb.AppendLine(e.ToString());

                throw new MerlinException("AC-012", sb.ToString(), e);
            }
        }
        #endregion

        #region AUTO TYPE CASTERS
        protected T CastAuto<T>(IDataReader Row, string Key)
        {
            if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), Row[Key].ToString());
            }

            return Cast<T>(Row, Key, true, default(T));
        }

        protected T CastAutoPrefix<T>(IDataReader Row, string Key, string Prefix)
        {
            if (typeof(T).IsEnum)
            {
                var realKey = (Prefix == "" ? Key : Prefix + Key);

                return (T)Enum.Parse(typeof(T), Row[Key].ToString());
            }

            return Cast<T>(Row, Key, true, default(T));
        }

        protected T Cast<T>(IDataReader row, string Key, bool throwError = true, T defaultValue = default(T), string Prefix = "")
        {
            var realKey = (Prefix == "" ? Key : Prefix + Key);

            try
            {
                // PERFORM TYPE MATCHING OF REQUESTED TYPE
                if (typeof(T).FullName == typeof(int).FullName)
                {
                    return (T)(object)_CastToInt(row, realKey);
                }

                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), row[realKey].ToString());
                }

                return (T)row[realKey];
            }
            catch (Exception e)
            {
                if (throwError)
                {
                    Type target = typeof(T);
                    Type original = row[realKey].GetType();

                    Type obj = this.GetType();

                    var m = string.Format("[Obj: {0}]Unable to cast column[{1}] From {2} to {3}",
                        obj.ToString(),
                        Key, 
                        original.ToString(),
                        target.ToString());

                    throw new MerlinException("AC-010", m, e);
                }

                return defaultValue;
            }
        }
        #endregion

        #region CAST OPERATORS
        
        private int _CastToInt(IDataReader row, string Key)
        {
            var fieldType = row[Key].GetType().FullName;


            if (fieldType == typeof(int).FullName)
            {
                return (int)row[Key];
            }
            else if (fieldType == typeof(long).FullName)
            {
                var field = (long)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(Int64).FullName)
            {
                var field = (Int64)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(Int16).FullName)
            {
                var field = (Int16)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(decimal).FullName)
            {
                var field = (decimal)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(short).FullName)
            {
                var field = (short)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(sbyte).FullName)
            {
                var field = (sbyte)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(byte).FullName)
            {
                var field = (byte)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(ushort).FullName)
            {
                var field = (ushort)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(ulong).FullName)
            {
                var field = (ulong)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(UInt16).FullName)
            {
                var field = (UInt16)row[Key];

                return Convert.ToInt32(field);
            }
            else if (fieldType == typeof(UInt64).FullName)
            {
                var field = (UInt64)row[Key];

                return Convert.ToInt32(field);
            }
            else
            {
                return Convert.ToInt32(row[Key]);
            }
        }
        
        #endregion
    }
}
