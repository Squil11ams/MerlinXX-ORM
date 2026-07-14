using MerlinORM.Server.MySQL;
using MerlinORM.Client;
namespace Testing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var q = new GenericQuery("SELECT * FROM volt_db.assets;");

            var qe = new QueryEngine("Default");

            var data = qe.GetList<Asset>(q);


            foreach (var a in data)
            {
                Console.WriteLine($"{a.asset_id} - {a.asset_name} -- {a.asset_uuid}");
            }

            Console.ReadLine();
        }
    }
}
