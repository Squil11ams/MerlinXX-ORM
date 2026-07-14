using MerlinORM.Server.MySQL;
using MerlinORM.Client;
namespace Testing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var q = new GenericQuery("SELECT * FROM volt_db.tester;");

                var qe = new QueryEngine("Default");

                var data = qe.GetList<TestModelB>(q);


                foreach (var a in data)
                {
                    Console.WriteLine($"{a.a} || {a.AutoPop} || {a.c} || {a.d} || {a.e} || {a.f} || {a.g} || {a.h} || {a.i} || {a.j} || {a.k} || {a.l} || {a.m} || {a.n} || {a.o} || {a.p} || {a.q} || {a.r} || {a.s} || {a.t} || {a.u} || {a.v}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();
        }
    }
}
