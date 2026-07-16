using BenchmarkConsole.Models;
using MerlinORM.Server.MySQL;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkConsole.Tests
{
    internal class SimpleTests
    {
        private static readonly QueryEngine DB = new("Local");

        public static async Task<List<User>> UsersBasic()
        {
            try
            {
                var q = new GenericQuery("SELECT * FROM `merlin_bench`.`users`;");

                var data = await DB.GetListAsync<User>(q);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new List<User>();
        }

        public static async Task<List<Client>> ClientBasic()
        {
            try
            {
                var q = new GenericQuery("SELECT * FROM merlin_bench.clients;");

            var data = await DB.GetListAsync<Client>(q);

            return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new List<Client>();
        }

        public static async Task<List<UserExtended>> UserComplex()
        {
            try
            {
                var q = new GenericQuery("SELECT * FROM merlin_bench.users U LEFT JOIN merlin_bench.clients C ON U.user_client = C.client_id;");

            var data = await DB.GetListAsync<UserExtended>(q);

            return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new List<UserExtended>();
        }
    }
}
