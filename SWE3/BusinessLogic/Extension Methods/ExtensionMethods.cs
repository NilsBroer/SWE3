using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWE3.BusinessLogic;

namespace SWE3
{
    public static class ExtensionMethods
    {
        public static TableObject ToTableObject(this object classObject)
        {
            var table = new TableObject
            {
                tableName = classObject.GetType().Name, columns = new List<(string, string)>()
            };
            //TODO: to ctor
            foreach (var property in classObject.GetType().GetProperties())
            {
                table.columns.Add((property.Name, property.GetTypeForSql()));
            }
            table.primaryKey = table.columns.FirstOrDefault()!.Item1;

            return table;
        }

        private static string GetTypeForSql(this PropertyInfo property)
        {
            var fieldType = property.PropertyType.ToString();
            return fieldType switch
            {
                "System.Int16" => "smallint",
                "System.Int32" => "int",
                "System.Int64" => "bigint",
                "System.Double" => "decimal(35,20",
                "System.String" => "nvarchar(255)",
                "System.DateTime" => "datetime",
                "System.Boolean" => "bit",
                _ => "sql_variant"
            };
        }
    }
}