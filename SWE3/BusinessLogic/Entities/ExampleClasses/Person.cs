using System;

namespace SWE3.BusinessLogic.Entities.ExampleClasses
{
    public class Person
    {
        public int personid;
        public string vorname;
        public string nachname;
        public DateTime geburtsdatum;
        /*
        public Person(string primaryKeyColumnName = null) : base()
        {
            //this.tableName = this.GetType().Name;
            foreach (var field in this.GetType().GetFields())
            {
                if (typeof(ATable).GetFields().Contains(field)) continue;
                
                var sqlType = field.GetTypeForSql();
                this.columnNamesAndTypes.Add(new Tuple<string, string>(field.Name,sqlType));
            }
            
            this.primaryKeyColumnName = primaryKeyColumnName ?? this.primaryKeyColumnName;

            if (!columnNamesAndTypes.Any(tuple => tuple.Item1.Equals(this.primaryKeyColumnName)))
            {
                Console.WriteLine("in: " + primaryKeyColumnName + " set: " + this.primaryKeyColumnName + " col1: " + columnNamesAndTypes.FirstOrDefault()!.Item1);
                //throw new Exception("Primary key column not found in column-list.");
            }

            Console.WriteLine("Table " + tableName + " has the following fields:");
            columnNamesAndTypes?.ForEach(tuple => Console.WriteLine(tuple.Item1 + " with type " + tuple.Item2));
        }
        */
    }
}