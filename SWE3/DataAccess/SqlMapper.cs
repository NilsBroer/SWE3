using System;
using System.Linq;
using SWE3.BusinessLogic;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class SqlMapper : ISqlMapper
    {
        private readonly IDataHelper dataHelper;

        public SqlMapper(IDataHelper dataHelper)
        {
            this.dataHelper = dataHelper;
        }

        public void CreateSqlTable(TableObject tableObject)
        {
            var commandText = "CREATE TABLE " + tableObject.tableName + "(";
            commandText = tableObject.columns.Aggregate(commandText,
                (str, column) => str + column.name + " " + column.type + ", ");

            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        public void InsertIntoSqlTable(object instance)
        {
            var properties = instance.GetType().GetProperties();
            var propertyValues = properties.Select(property => property.GetValue(instance)).ToList();

            var tableObject = instance.ToTableObject();
            var columnNames = tableObject.columns.Select(column => column.name).ToList();

            var commandText = "INSERT INTO " + tableObject.tableName + " (";
            commandText = columnNames.Aggregate(commandText,
                (str, columnName) => str + columnName + ", ");
            commandText = commandText.Substring(0, commandText.Length - 2) + ")" +
                          "\n" + "VALUES (";

            commandText = propertyValues.Aggregate(commandText,
                (str, value) => str + "'" + (value ?? "null") + "', ");
            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            Console.WriteLine(commandText);

            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        //TODO: Parameterization, how to handle functions, primary key
    }
}