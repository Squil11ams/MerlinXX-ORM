using MerlinORM.Client;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace MerlinORM.Server.MySQL
{
    /// <summary>
    /// QueryEngine is used to actually interact with the database.
    /// 
    /// TODO: Probably should either use Interface to support different databases or renamed to better indicate MySQL Only Support.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class QueryEngine
    {
        #region FIELDS
        private string _connectionString;
        #endregion


        #region CONSTRUCTORS
        /// <summary>
        /// Builds new instance of QueryEngine and loads connection string.
        /// </summary>
        /// <param name="ConnectionStringKey"></param>
        /// <param name="AppSettings"></param>
        /// <exception cref="MerlinException"></exception>
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
        /// Executes the specified query and maps the returned rows to a list of model objects.
        /// </summary>
        /// <typeparam name="T">
        /// The model type to populate. The type must implement <see cref="IMerlinObject"/>
        /// and provide a parameterless constructor.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the SQL statement and parameters to execute.
        /// </param>
        /// <returns>
        /// A <see cref="List{T}"/> containing the mapped objects returned by the query.
        /// Returns an empty list if the query returns no rows.
        /// </returns>
        /// <exception cref="MerlinException">
        /// Thrown when the query result cannot be mapped to the requested model type.
        /// </exception>
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



        /// <summary>
        /// Executes the specified query asynchronously and maps the returned rows to a list of model objects.
        /// </summary>
        /// <typeparam name="T">
        /// The model type to populate. The type must implement <see cref="IMerlinObject"/>
        /// and provide a parameterless constructor.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the SQL statement and parameters to execute.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the database operation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The result contains a
        /// <see cref="List{T}"/> of mapped objects returned by the query.
        /// Returns an empty list if the query returns no rows.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through the provided <paramref name="cancellationToken"/>.
        /// </exception>
        /// <exception cref="MerlinException">
        /// Thrown when the query result cannot be mapped to the requested model type.
        /// </exception>
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
        /// Executes the specified query and maps the first returned row to a model object.
        /// </summary>
        /// <typeparam name="T">
        /// The model type to populate. The type must implement <see cref="IMerlinObject"/>
        /// and provide a parameterless constructor.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the SQL statement and parameters to execute.
        /// </param>
        /// <returns>
        /// The mapped object from the first row returned by the query;
        /// otherwise <c>null</c> if no rows are returned.
        /// </returns>
        /// <exception cref="MerlinException">
        /// Thrown when the query result cannot be mapped to the requested model type.
        /// </exception>
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

        /// <summary>
        /// Executes the specified query asynchronously and maps the first returned row to a model object.
        /// </summary>
        /// <typeparam name="T">
        /// The model type to populate. The type must implement <see cref="IMerlinObject"/>
        /// and provide a parameterless constructor.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the database operation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The result contains the mapped
        /// object from the first row returned by the query; otherwise <c>null</c> if no rows
        /// are returned.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through the provided
        /// <paramref name="cancellationToken"/>.
        /// </exception>
        /// <exception cref="MerlinException">
        /// Thrown when the query result cannot be mapped to the requested model type.
        /// </exception>
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
        /// Executes the specified query and converts the first column of each returned row
        /// into a list of simple values.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. The database value from the first column of each row
        /// must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <returns>
        /// A <see cref="List{T}"/> containing the converted values from the first column
        /// of each returned row.
        /// Returns an empty list if the query returns no rows.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single column of simple values,
        /// such as integers, strings, dates, or other directly convertible types.
        /// It does not perform object mapping and should not be used for model types
        /// implementing <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when a returned database value cannot be converted to the requested type.
        /// </exception>
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

        /// <summary>
        /// Executes the specified query and converts the first column of each returned row
        /// into a list of simple values.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. The database value from the first column of each row
        /// must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the database operation.
        /// </param>
        /// <returns>
        /// A <see cref="List{T}"/> containing the converted values from the first column
        /// of each returned row.
        /// Returns an empty list if the query returns no rows.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single column of simple values,
        /// such as integers, strings, dates, or other directly convertible types.
        /// It does not perform object mapping and should not be used for model types
        /// implementing <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when a returned database value cannot be converted to the requested type.
        /// </exception>
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
        /// <summary>
        /// Executes the specified query and converts the first column of the first returned row
        /// into a simple value.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. The database value must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <returns>
        /// The converted value from the first column of the first returned row.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single row with a single column,
        /// such as retrieving a count, identifier, status value, or other scalar result.
        /// It does not perform object mapping and should not be used for types implementing
        /// <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when no rows are returned or when the returned value cannot be converted
        /// to the requested type.
        /// </exception>
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

        /// <summary>
        /// Executes the specified query and converts the first column of the first returned row
        /// into a simple value.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. The database value must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token used to cancel database operation.
        /// </param>
        /// <returns>
        /// The converted value from the first column of the first returned row.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single row with a single column,
        /// such as retrieving a count, identifier, status value, or other scalar result.
        /// It does not perform object mapping and should not be used for types implementing
        /// <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when no rows are returned or when the returned value cannot be converted
        /// to the requested type.
        /// </exception>
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
        /// <summary>
        /// Executes the specified query and converts the values from the first returned row
        /// into an array of simple values.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. Each returned column value must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <param name="columns">
        /// The number of columns expected in the returned row.
        /// </param>
        /// <returns>
        /// An array containing the converted values from the first returned row.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single row with multiple columns
        /// of the same convertible type.
        ///
        /// For example, a query returning:
        /// <code>
        /// SELECT voltage_a, voltage_b, voltage_c FROM measurements;
        /// </code>
        /// can be returned as:
        /// <code>
        /// double[] voltages = queryEngine.GetSimpleArray&lt;double&gt;(query, 3);
        /// </code>
        ///
        /// This method does not perform object mapping and should not be used for types
        /// implementing <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when no rows are returned, fewer columns are returned than requested,
        /// or a column value cannot be converted to the requested type.
        /// </exception>
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

        /// <summary>
        /// Executes the specified query and converts the values from the first returned row
        /// into an array of simple values.
        /// </summary>
        /// <typeparam name="T">
        /// The target value type. Each returned column value must be convertible to this type.
        /// </typeparam>
        /// <param name="queryObj">
        /// The query provider containing the query statement and parameters to execute.
        /// </param>
        /// <param name="columns">
        /// The number of columns expected in the returned row.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token used to cancel database operation.
        /// </param>
        /// <returns>
        /// An array containing the converted values from the first returned row.
        /// </returns>
        /// <remarks>
        /// This method is intended for queries returning a single row with multiple columns
        /// of the same convertible type.
        ///
        /// For example, a query returning:
        /// <code>
        /// SELECT voltage_a, voltage_b, voltage_c FROM measurements;
        /// </code>
        /// can be returned as:
        /// <code>
        /// double[] voltages = queryEngine.GetSimpleArray&lt;double&gt;(query, 3);
        /// </code>
        ///
        /// This method does not perform object mapping and should not be used for types
        /// implementing <see cref="IMerlinObject"/>.
        /// </remarks>
        /// <exception cref="MerlinException">
        /// Thrown when no rows are returned, fewer columns are returned than requested,
        /// or a column value cannot be converted to the requested type.
        /// </exception>
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
        /// <summary>
        /// Executes a SQL command that does not return a result set.
        /// </summary>
        /// <param name="queryObj">
        /// SQL query provider containing the command text and parameters to execute.
        /// </param>
        /// <returns>
        /// The number of rows affected by the command.
        /// </returns>
        public int ExecuteNonQuery(IMerlinProvider queryObj)
        {
            return Execute(() =>
            {
                using var conn = CreateConnection();
                using var cmd = CreateCommand(queryObj, conn);

                return cmd.ExecuteNonQuery();
            }, "MERLIN-QEP-1014");
        }

        /// <summary>
        /// Asynchronously executes a SQL command that does not return a result set.
        /// </summary>
        /// <param name="queryObj">
        /// SQL query provider containing the command text and parameters to execute.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the number of rows affected by the command.
        /// </returns>
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
