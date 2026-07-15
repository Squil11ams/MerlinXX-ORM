using GridBeacon.Common.Models;
using GridBeacon.Databases.GRID;
using MerlinORM.Client;
using MerlinORM.Server.MySQL;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press [Enter] to start...");
            Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Starting");

            try
            {
                //OtherTest();
                
                Test();
            }
            catch (MerlinException me)
            {
                Console.WriteLine(me.ToString(true));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            

            Console.WriteLine("Done...");

            Console.ReadLine();
        }
        private static void OtherTest()
        {
            var qe = new QueryEngine("Default");

            var query = new GenericQuery("SELECT * FROM volt_db.v_user_list;");

            var list = qe.GetList<User>(query);

            foreach (var user in list)
            {
                Console.WriteLine(user.u_name + " " + user.client.client_name);
            }
        }


        private static void Test()
        {
            Console.WriteLine("Attempting to login..");
            var result = UserDB.Authenticate("mcgee.will@gmail.com", "127.0.0.1", "Grand Prairie, TX", "US", "NA");

            if(result.AuthResult == LoginStatusEnum.Denied)
            {
                Console.WriteLine("Access was denied..");
                return;
            }

            var user = UserDB.LoadUser(result);

            Console.WriteLine("Access should be good.");
            Console.WriteLine("ID:          " + user.ID);
            Console.WriteLine("Name:        " + user.Name);
            Console.WriteLine("Email:       " + user.Email);
            Console.WriteLine("Key:         " + user.Key);
            Console.WriteLine("Level:       " + user.Level);
            Console.WriteLine("Permissions: " + user.Permissions.Count);

            Console.WriteLine("Client Name: " + user.Client.Name);
            Console.WriteLine("Client ID:   " + user.Client.ID);

        }

        private bool SimpleChecks(LoginResponse loginResponse)
        {
            // CHECK IF LOOKUP WAS SUCCESSFUL
            if (loginResponse.AuthResult == LoginStatusEnum.Denied)
            {
                //Output = CurrentUser.EmptyUser();
                // DB AUTO LOGS THIS AS A FAILED LOGIN ATTEMPT, USER NOT FOUND
                return false;
            }

            // CHECK IF ACCOUNT IS ACTIVE OR NOT
            if (loginResponse.UserStatus == ActInacEnum.Inactive || loginResponse.ClientStatus == ActInacEnum.Inactive)
            {
                //Output = CurrentUser.EmptyUser();
                //LogAttempt(loginResponse, LoginStatusEnum.Denied, "User or Client is Inactive");
                return false;
            }

            return true;
        }
    }
}
