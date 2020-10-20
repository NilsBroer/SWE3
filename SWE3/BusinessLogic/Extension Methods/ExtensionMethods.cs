using System;
using System.Collections.Generic;
using System.Reflection;
using SWE3.BusinessLogic.Entities;

namespace SWE3
{
    public static class ExtensionMethods
    {
        public static Table ToTable(this object classObject)
        {
            var table = new Table
            {
                Name = classObject.GetType().Name,
                Columns = new List<Column>()
            };

            foreach (var property in classObject.GetType().GetProperties())
            {
                var column = new Column
                {
                    Name = property.Name,
                    Type = property.GetTypeForSql(),
                    NotNull = Attribute.IsDefined(property,typeof(NotNullAttribute)),
                    PrimaryKey = Attribute.IsDefined(property,typeof(PrimaryKeyAttribute)),
                    SecondaryKey = Attribute.IsDefined(property,typeof(SecondaryKeyAttribute))
                };
                table.Columns.Add(column);
            }

            return table;
        }

        private static string GetTypeForSql(this PropertyInfo property) //TODO: Expand this list
        {
            var fieldType = property.PropertyType.ToString();
            return fieldType switch
            {
                "System.Int16" => "smallint",
                "System.Int32" => "int",
                "System.Int64" => "bigint",
                "System.Double" => "decimal(35,20)",
                "System.String" => "nvarchar(255)",
                "System.DateTime" => "datetime",
                "System.Boolean" => "bit",
                _ => "sql_variant"
            };
        }
    }
}