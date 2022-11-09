# MerlinXX-ORM

https://github.com/Squil11ams/MerlinXX-ORM

This is a simple ORM that can auto populate data models with data from a database.

See the Wiki for instructions on how to use this package.
https://github.com/Squil11ams/MerlinXX-ORM/wiki


## Basic Setup

There are two steps required to use this ORM.

Step 1 - Model 

In your model setup your fields to match the columns and datatype of the database and extend your model with MerlinObjectBase.

```C#
class MyModel : MerlinObjectBase

{
        public int col_id { get; set; }
        public string col_name { get; set; }
        
        [AutoPopSettings("col_status")]
        public string status { get; set; }

        [Exclude]
        public string NotInDB { get; set; }

        [MerlinObject]
        public MyModel SomeOtherModel { get; set; }
}
```
Step 2 - Query the Database

```C#
class MyDbClass
{
    public static List<MyModel> GetMyModel(string status)
    {
        QueryEngine.SetConfig("MySQL_Connection_String");

        var query = new GenericQuery("SELECT * FROM mytable WHERE col_status = @myParam");

        query.AddParameter("@myParam", status);

        return QueryEngine.MerlinObject.GetList<MyModel>(query);
    }
}
```
