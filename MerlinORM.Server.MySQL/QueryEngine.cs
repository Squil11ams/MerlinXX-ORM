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
                throw new MerlinException("MERLIN-QEP-1034");
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

        private static T Execute<T>(Func<T> action, string errorCode = "MERLIN-QEP-1000")
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
                throw new MerlinException(errorCode, ex);
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string errorCode = "MERLIN-QEP-10001")
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
                throw new MerlinException(errorCode, ex);
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
            }, "MERLIN-QEP-1004");
        }

        public async Task<List<T>> GetListAsync<T>(IMerlinProvider queryObj, CancellationToken cancellationToken = default) where T : IMerlinObject, new()
        {
            return await ExecuteAsync(async () =>
            {
                var data = new List<T>();

                await using var conn = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var temp = new T();
                    temp.SetDataObject(reader);
                    data.Add(temp);
                }

                return data;
            }, "MERLIN-QEP-1005").ConfigureAwait(false);
        }
        #endregion


        #region GetObject<T> where T : ISqlObject
        /// <summary>
        /// Return Single Object of T
        /// </summary>
        /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
        /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
        /// <returns>Single Object of T</returns>
        public T? GetObject<T>(IMerlinProvider queryObj) where T : IMerlinObject, new()
        {
            return Execute(() =>
            {
                T data = new T();

                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return default;
                }

                data.SetDataObject(reader);

                return data;
            }, "MERLIN-QEP-1006");
        }

        public async Task<T?> GetObjectAsync<T>(IMerlinProvider queryObj, CancellationToken cancellationToken = default) where T : IMerlinObject, new()
        {
            return await ExecuteAsync(async () =>
            {
                T data = new T();

                await using var conn = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return default;
                }

                data.SetDataObject(reader);

                return data;
            }, "MERLIN-QEP-1007").ConfigureAwait(false);
        }
        #endregion


        #region GetSimpleList
        /// <summary>
        /// Returns a list of T 
        /// Casts first column of query to T
        /// 
        /// T must be a castable object from SQL
        /// </summary>
        /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
        /// <returns>List of T</returns>
        public List<T> GetSimpleList<T>(IMerlinProvider queryObj)
        {
            return Execute(() =>
            {
                var data = new List<T>();

                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);
                using var reader = cmd.ExecuteReader();

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));
                
                while (reader.Read())
                {
                    try
                    {
                        data.Add((T)converter(reader[0])!);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unable to convert '{reader[0]?.GetType().Name}' to '{typeof(T).Name}' ";

                        throw new MerlinException("MERLIN-QEP-1002", msg, e);
                    }
                }

                return data;
            }, "MERLIN-QEP-1008");
        }

        public async Task<List<T>> GetSimpleListAsync<T>(IMerlinProvider queryObj, CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                var data = new List<T>();

                await using var conn = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));

                while (await reader.ReadAsync(cancellationToken))
                {
                    try
                    {
                        var raw = reader[0];

                        data.Add((T)converter(raw)!);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unable to convert '{reader[0]?.GetType().Name}' to '{typeof(T).Name}' ";

                        throw new MerlinException("MERLIN-QEP-1003", msg, e);
                    }
                }

                return data;
            }, "MERLIN-QEP-1009").ConfigureAwait(false);
        }
        #endregion


        #region GetSimple
        public T GetSimple<T>(IMerlinProvider queryObj)
        {
            return Execute(() =>
            {
                using var con = CreateConnection();
                using var cmd = CreateCommand(queryObj, con);
                using var rdr = cmd.ExecuteReader();

                if (!rdr.Read())
                {
                    throw new MerlinException("MERLIN-QEP-1025");
                }

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));

                try
                {
                    return ((T)converter(rdr[0])!);
                }
                catch (Exception e)
                {
                    var msg = $"Unable to convert '{rdr[0]?.GetType().Name}' to '{typeof(T).Name}' ";

                    throw new MerlinException("MERLIN-QEP-1023", msg, e);
                }
            }, "MERLIN-QEP-1010");
        }

        public async Task<T> GetSimpleAsync<T>(IMerlinProvider queryObj, CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                await using var con = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, con);
                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await rdr.ReadAsync(cancellationToken))
                {
                    throw new MerlinException("MERLIN-QEP-1024");
                }

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));

                try
                {
                    return ((T)converter(rdr[0])!);
                }
                catch (Exception e)
                {
                    var msg = $"Unable to convert '{rdr[0]?.GetType().Name}' to '{typeof(T).Name}' ";

                    throw new MerlinException("MERLIN-QEP-1022", msg, e);
                }
            }, "MERLIN-QEP-1011");
        }
        #endregion


        #region GetSimpleArray
        public T[] GetSimpleArray<T>(IMerlinProvider queryObj, int columns)
        {
            return Execute(() =>
            {
                var data = new T[columns];

                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    throw new MerlinException("MERLIN-QEP-1021");
                }

                if (reader.FieldCount < columns)
                {
                    var msg = $"Query returned {reader.FieldCount} columns, expected {columns}";

                    throw new MerlinException("MERLIN-QEP-1020", msg);
                }

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));

                for (int i = 0; i < columns; i++)
                {
                    var value = reader[i];

                    try
                    {
                        data[i] = (T)converter(value)!;
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unable to convert Column({i}) '{value?.GetType().Name ?? "NULL"}' to '{typeof(T).Name}'.";

                        throw new MerlinException("MERLIN-QEP-1019", msg, e);
                    }
                }

                return data;
            }, "MERLIN-QEP-1012");
        }

        public async Task<T[]> GetSimpleArrayAsync<T>(IMerlinProvider queryObj, int columns, CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                var data = new T[columns];

                await using var con = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, con);
                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await rdr.ReadAsync(cancellationToken))
                {
                    throw new MerlinException("MERLIN-QEP-1018");
                }

                if (rdr.FieldCount < columns)
                {
                    var msg = $"Query returned {rdr.FieldCount} columns, expected {columns}";

                    throw new MerlinException("MERLIN-QEP-1017", msg);
                }

                var converter = MerlinPropertyMetadata.CreateConverter(typeof(T));

                for (int i = 0; i < columns; i++)
                {
                    var value = rdr[i];

                    try
                    {
                        data[i] = (T)converter(value)!;
                    }
                    catch (Exception e)
                    {
                        var msg = $"Unable to convert Column({i}) '{value?.GetType().Name ?? "NULL"}' to '{typeof(T).Name}'.";

                        throw new MerlinException("MERLIN-QEP-1016", msg, e);
                    }
                }

                return data;
            }, "MERLIN-QEP-1013");
        }
        #endregion


        #region NO-RETURN
        public int ExecuteNonQuery(IMerlinProvider queryObj)
        {
            return Execute(() =>
            {
                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);

                return cmd.ExecuteNonQuery();
            }, "MERLIN-QEP-1014");
        }

        public async Task<int> ExecuteNonQueryAsync(IMerlinProvider queryObj, CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                await using var conn = await CreateConnectionAsync(cancellationToken);
                await using var cmd = CreateCommand(queryObj, conn);

                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }, "MERLIN-QEP-1015");
        }
        #endregion
    }
}
