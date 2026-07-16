using BenchmarkConsole.Models;
using BenchmarkDotNet.Attributes;
using Dapper;
using MerlinORM.Server.MySQL;
using MySqlConnector;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BenchmarkConsole.Tests
{
    [MemoryDiagnoser]
    public class ComplexUserTest
    {
        private static readonly QueryEngine DB = new("Local");
        private static readonly string ConStr = "server=127.0.0.1;user id=root;password=lone99star;port=3306;database=merlin_bench;convertzerodatetime=True";

        [Benchmark(Baseline = true)]
        public async Task<int> Test1_Dapper()
        {
            using IDbConnection connection = new MySqlConnection(ConStr);

            string sql = "SELECT * FROM merlin_bench.users U LEFT JOIN merlin_bench.clients C ON U.user_client = C.client_id;";

            var result = await connection.QueryAsync<UserExtended, Client, UserExtended>(sql, map: (user, profile) => {
                user.Client = profile;
                return user;
            },
                param: new { },
                splitOn: "client_id" // Tells Dapper where the Profile object fields begin
            );

            return result.AsList().Count();
        }

        [Benchmark]
        public async Task<int> Test2_Merlin()
        {
            var q = new GenericQuery("SELECT * FROM merlin_bench.users U LEFT JOIN merlin_bench.clients C ON U.user_client = C.client_id;");

            var data = await DB.GetListAsync<UserExtended>(q);

            return data.Count;
        }
    }

}
