using MerlinORM.Client;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace MerlinORM.Server.MySQL
{
    public class QueryEngine
    {
        #region FIELDS
        private string _connectionString;
        #endregion

        #region CONSTRUCTORS

        public QueryEngine(string ConnectionStringKey, string AppSettings = "appsettings.json") 
        {
            _connectionString = MerlinConfig.GetConnectionString(ConnectionStringKey, AppSettings);

            if(string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new Exception("ConnectionString is Null, TODO Verify Config if this already happens? ");
            }


        }
        #endregion

        #region UTILITIES
        private MySqlConnection CreateConnection()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private async Task<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            return conn;
        }

        private MySqlCommand CreateCommand(IMerlinProvider provider, MySqlConnection conn, bool AutoParams = true)
        {
            var cmd = new MySqlCommand(provider.Query, conn);

            if (AutoParams)
            {
                cmd.Parameters.AddRange(provider.Parameters.ToArray());
            }

            return cmd;
        }

        private static T Execute<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (MerlinException)
            {
                throw;
            }
            catch (MySqlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MerlinException(ex.Message, ex);
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (MerlinException)
            {
                throw;
            }
            catch (MySqlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MerlinException(ex.Message, ex);
            }
        }
        #endregion

        #region GetList<T> where T : ISqlObject
        /// <summary>
        /// Returns a list of T
        /// </summary>
        /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
        /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
        /// <returns>List of T</returns>
        public List<T> GetList<T>(IMerlinProvider queryObj) where T : IMerlinObject, new()
        {
            return Execute(() =>
            {
                var data = new List<T>();

                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var temp = new T();
                    temp.SetDataObject(reader);
                    data.Add(temp);
                }

                return data;
            });
        }

        public async Task<List<T>> GetListAsync<T>(IMerlinProvider queryObj, CancellationToken cancellationToken = default) where T : IMerlinObject, new()
        {
            return await ExecuteAsync(async () =>
            {
                var data = new List<T>();

                await using var conn = await CreateConnectionAsync();
                await using var cmd = CreateCommand(queryObj, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var temp = new T();
                    temp.SetDataObject(reader);
                    data.Add(temp);
                }

                return data;
            });
        }

        #endregion
    }
}
