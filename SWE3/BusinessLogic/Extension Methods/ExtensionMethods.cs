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
        private const string CUSTOM = "#custom#";
        private const string MULTIPLE = "#multiple#";
        private const string DEFAULT_SYSTEM_TYPE = "CommonLanguageRuntimeLibrary";
        private const string SYSTEM = "System";
        private const string MICROSOFT = "Microsoft";

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

        public static bool IsDefaultSystemType(this Type type)
        {
            return 
                type.Module.ScopeName == DEFAULT_SYSTEM_TYPE ||
                type.Module.ScopeName.StartsWith(SYSTEM) ||
                (type.Namespace ?? "").StartsWith(SYSTEM) ||
                (type.Namespace ?? "").StartsWith(MICROSOFT);
        }

        public static bool IsIEnumerable(this Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        public static Type GetUnderlyingType(this Type type)
        {
            if (type == null) return null;
            if (type.IsEnum) return typeof(Enum);
            if (type.IsIEnumerable())
            {
                type = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                type = GetUnderlyingType(type); //In case it's wrapped in multiple layers
            }
            type = Nullable.GetUnderlyingType(type!) ?? type;
            return type;
        }
        
        private static string GetTypeForSql(this PropertyInfo property)
        {
            return toSqlString(property.PropertyType);
        }

        private static string toSqlString(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum) return "smallint";
            
            var typeString = type.ToString();
            
            return typeString switch
            {
                "System.Int16" => "smallint",
                "System.Int32" => "int",
                "System.Int64" => "bigint",
                "System.Double" => "decimal(35,20)",
                "System.String" => "nvarchar(255)",
                "System.DateTime" => "datetime",
                "System.Boolean" => "bit",
                _ => nestedOrCustomToSqlString(type)
            };
        }

        private static string nestedOrCustomToSqlString(Type type)
        {
            if(typeof(IEnumerable).IsAssignableFrom(type))
            {
                type = type.GetUnderlyingType();
                return MULTIPLE + toSqlString(type);
            }
            if (!type.IsDefaultSystemType())
            {
                return CUSTOM + type;
            }

            return "sql_variant";
        }
    }
}