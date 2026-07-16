using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Hold Class Extension Methods
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Converts nullable types to more user friendly name ie int? instead of NULLABLE`1
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFriendlyName(this Type type)
        {
            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            {
                return $"{underlyingType.GetFriendlyName()}?";
            }

            if (type.IsGenericType)
            {
                var genericName = type.Name[..type.Name.IndexOf('`')];
                var arguments = string.Join(", ",
                    type.GetGenericArguments()
                        .Select(x => x.GetFriendlyName()));

                return $"{genericName}<{arguments}>";
            }

            return type.Name switch
            {
                "Int32" => "int",
                "Int64" => "long",
                "Boolean" => "bool",
                "String" => "string",
                "Double" => "double",
                "Single" => "float",
                "Decimal" => "decimal",
                _ => type.Name
            };
        }
    }
}
