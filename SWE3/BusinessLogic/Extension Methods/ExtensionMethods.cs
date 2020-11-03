using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.FileIO;
using SWE3.BusinessLogic.Entities;

namespace SWE3
{
    public static class ExtensionMethods
    {
        private const string TABLE = "table_";
        private const string CUSTOM = "custom_";
        private const string ENUMERABLE = "enumerable_";
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
                    NotNull = Attribute.IsDefined(property,typeof(NotNullable)),
                    PrimaryKey = Attribute.IsDefined(property,typeof(PrimaryKeyAttribute)),
                };
                table.Columns.Add(column);
            }

            return table;
        }

        private static string GetTypeForSql(this PropertyInfo property)
        {
            if(typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                //TODO: Continue here :) return underlying type for sql
                return TABLE + ENUMERABLE + "";
            }
            
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
                _ => TABLE + fieldType
            };
        }
    }
}