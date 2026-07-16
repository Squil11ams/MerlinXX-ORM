using BenchmarkConsole.Models;
using BenchmarkDotNet.Attributes;
using MerlinORM.Server.MySQL;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Text;

namespace BenchmarkConsole.Tests
{
    [MemoryDiagnoser]
    public class FlatUserTest
    {
        private static readonly QueryEngine DB = new("Local");
        private static readonly string ConStr = "server=127.0.0.1;user id=root;password=lone99star;port=3306;database=merlin_bench;convertzerodatetime=True";

        [Benchmark(Baseline = true)]
        public async Task<int> Test1_Dapper()
        {
            using IDbConnection connection = new MySqlConnection(ConStr);

            string sql = "SELECT * FROM `merlin_bench`.`users`;";

            var user = await connection.QueryAsync<User>(sql, new { });

            return user.ToList().Count();
        }

        [Benchmark]
        public async Task<int> Test2_Merlin()
        {
            var q = new GenericQuery("SELECT * FROM `merlin_bench`.`users`;");

            var data = await DB.GetListAsync2<User>(q);

            return data.Count;
        }
    }
}
