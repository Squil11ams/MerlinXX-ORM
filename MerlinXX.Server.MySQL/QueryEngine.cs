using MerlinXX.Client;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Server.MySQL
{
    public class QueryEngine
    {
        #region CONFIGURATION
        private static string _connectionString;

        public static void SetConfig(string connection)
        {
            _connectionString = connection;
        }
        #endregion

        #region UTILITY FUNCTIONS
        public static MySqlConnection OpenConnection(bool AutoOpen = true)
        {
            if(_connectionString == "") { throw new MerlinException("QE-100", "No configuration has been set."); }

            return OpenConnection(_connectionString, AutoOpen);
        }

        public static MySqlConnection OpenConnection(string connectionString, bool AutoOpen = true)
        {
            var connection = new MySqlConnection(connectionString);

            if (AutoOpen)
            {
                connection.Open();
            }

            return connection;
        }

        public static MySqlCommand CreateCommand(IMerlinProvider provider, MySqlConnection connection, bool AutoParams = true)
        {
            var cmd = new MySqlCommand(provider.Query, connection);

            if (AutoParams)
            {
                cmd.Parameters.AddRange(provider.Parameters.ToArray());
            }

            return cmd;
        }
        #endregion

        public class MerlinObject
        {
            #region GetList<T> where T : ISqlObject
            /// <summary>
            /// Returns a list of T
            /// </summary>
            /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>List of T</returns>
            public static List<T> GetList<T>(IMerlinProvider queryObj) where T : IMerlinObject, new()
            {
                using (MySqlConnection connection = OpenConnection())
                {
                    return GetList<T>(queryObj, connection);
                }
            }

            /// <summary>
            /// Returns a list of T
            /// </summary>
            /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>List of T</returns>
            public static List<T> GetList<T>(IMerlinProvider queryObj, MySqlConnection con) where T : IMerlinObject, new()
            {
                try
                {
                    var data = new List<T>();

                    using (MySqlCommand cmd = CreateCommand(queryObj, con))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T temp = new T();
                            temp.SetDataObject(reader, 0);
                            data.Add(temp);
                        }
                    }

                    return data;
                }
                catch (TargetInvocationException t)
                {
                    if (t.InnerException != null)
                    {
                        if (t.InnerException is MerlinException)
                        {
                            throw t.InnerException;
                        }
                        else
                        {
                            throw new MerlinException("QE-101", t.InnerException.GetType().Name, t);
                        }
                    }
                    else
                    {
                        throw new MerlinException("QE-102", t.Message, t);
                    }
                }
                catch (MerlinException)
                {
                    throw;
                }
                catch (MySqlException me)
                {
                    throw new MerlinException("QE-103", me.ToString(), me);
                }
                catch (Exception ex)
                {
                    throw new MerlinException("QE-104", ex.Message, ex);
                }
            }
            #endregion

            #region GetObject<T> where T : ISqlObject
            /// <summary>
            /// Return Single Object of T
            /// </summary>
            /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>Single Object of T</returns>
            public static T GetObject<T>(IMerlinProvider queryObj) where T : IMerlinObject, new()
            {
                using (MySqlConnection connection = OpenConnection())
                {
                    return GetObject<T>(queryObj, connection);
                }
            }

            /// <summary>
            /// Return Single Object of T
            /// </summary>
            /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>Single Object of T</returns>
            public static T GetObject<T>(IMerlinProvider queryObj, MySqlConnection connection) where T : IMerlinObject, new()
            {
                try
                {
                    T data = new T();

                    using (MySqlCommand cmd = CreateCommand(queryObj, connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            bool run1 = reader.Read();

                            data.SetDataObject(reader, 1);
                        }
                        else
                        {
                            return default(T);
                        }
                    }

                    return data;
                }
                catch (TargetInvocationException t)
                {
                    if (t.InnerException != null)
                    {
                        if (t.InnerException is MerlinException)
                        {
                            throw t.InnerException;
                        }
                        else
                        {
                            throw new MerlinException("QE-105", t.InnerException.GetType().Name, t);
                        }
                    }
                    else
                    {
                        throw new MerlinException("QE-106", t.Message, t);
                    }
                }
                catch (MerlinException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw new MerlinException("QE-107", "[IN] " + ex.InnerException.Message, ex);
                    }
                    else
                    {
                        throw new MerlinException("QE-108", "[OUT] " + ex.Message, ex);
                    }
                }
            }
            #endregion
        }

        public class SimpleType
        {
            #region GetListSimple
            /// <summary>
            /// Returns a list of T 
            /// Casts first column of query to T
            /// 
            /// T must be a castable object from SQL
            /// </summary>
            /// <typeparam name="T">Type must implement ISqlObject interface</typeparam>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>List of T</returns>
            public static List<T> GetListSimple<T>(IMerlinProvider queryObj)
            {
                using (MySqlConnection connection = OpenConnection())
                {
                    return GetListSimple<T>(queryObj, connection);
                }
            }

            /// <summary>
            /// Returns a list of T 
            /// Casts first column of query to T
            /// 
            /// T must be a castable object from SQL
            /// </summary>
            /// <param name="queryObj">Sql Query Object implements ISqlProvider</param>
            /// <returns>List of T</returns>
            public static List<T> GetListSimple<T>(IMerlinProvider queryObj, MySqlConnection con)
            {
                try
                {
                    var data = new List<T>();

                    using (MySqlCommand cmd = CreateCommand(queryObj, con))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                data.Add((T)reader[0]);
                            }
                        }
                        catch (Exception e)
                        {
                            Type t = typeof(T);
                            var m = string.Format("Unable to cast {1} to {0}", t.ToString(), reader[0].GetType().ToString());

                            throw new MerlinException("QE-109", m, e);
                        }
                    }


                    return data;
                }
                catch (MerlinException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new MerlinException("QE-110", "", ex);
                }
            }
            #endregion


            #region GetSingleReturn
            /// <summary>
            /// Use for single Row, single Column query for simple types.
            /// 
            /// IE, String or Int. Not for any other use case!
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="queryObj">The query object.</param>
            /// <returns>T.</returns>
            public static T GetSingleReturn<T>(IMerlinProvider queryObj)
            {
                using (MySqlConnection connection = OpenConnection())
                {
                    return GetSingleReturn<T>(queryObj, connection);
                }
            }

            /// <summary>
            /// Use for single Row, single Column query for simple types.
            /// 
            /// IE, String or Int. Not for any other use case!
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="queryObj">The query object.</param>
            /// <returns>T.</returns>
            public static T GetSingleReturn<T>(IMerlinProvider queryObj, MySqlConnection connection)
            {
                try
                {
                    T data = default(T);

                    using (MySqlCommand cmd = CreateCommand(queryObj, connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();

                            try
                            {
                                data = (T)reader[0];
                            }
                            catch (Exception e)
                            {
                                Type t = typeof(T);
                                var m = string.Format("Unable to cast {1} to {0}", t.ToString(), reader[0].GetType().ToString());

                                throw new MerlinException("QE-112", m, e);
                            }
                        }
                        else
                        {
                            throw new MerlinException("QE-113", "No rows returned");
                        }
                    }
                    return data;
                }
                catch (Exception e)
                {
                    throw new MerlinException("QE-114", e.Message, e);
                }
            }
            #endregion


            #region GetSimpleReturn

            /// <summary>
            /// Use for single Row, multi Column query for simple types.
            /// IE, String or Int. Not for any other use case!
            /// 
            /// EACH COLUMN MUST BE SAME TYPE
            /// 
            /// </summary>
            /// <typeparam name="T">Simple type, String/Int/etc.</typeparam>
            /// <param name="queryObj">The query object.</param>
            /// <param name="columns">Number of columns returned</param>
            /// <returns>T</returns>
            public static T[] GetSimpleReturn<T>(IMerlinProvider queryObj, int columns)
            {
                using (MySqlConnection connection = OpenConnection())
                {
                    return GetSimpleReturn<T>(queryObj, columns, connection);
                }
            }


            /// <summary>
            /// Use for single Row, multi Column query for simple types.
            /// IE, String or Int. Not for any other use case!
            /// 
            /// EACH COLUMN MUST BE SAME TYPE
            /// 
            /// </summary>
            /// <typeparam name="T">Simple type, String/Int/etc.</typeparam>
            /// <param name="queryObj">The query object.</param>
            /// <param name="columns">Number of columns returned</param>
            /// <param name="connection"></param>
            /// <returns>T</returns>
            public static T[] GetSimpleReturn<T>(IMerlinProvider queryObj, int columns, MySqlConnection connection)
            {
                try
                {
                    T[] data = new T[columns];

                    using (MySqlCommand cmd = CreateCommand(queryObj, connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();

                            for (int i = 0; i < columns; i++)
                            {
                                data[i] = (T)reader[i];
                            }
                        }
                        else
                        {
                            throw new MerlinException("QE-115", "No rows returned");
                        }
                    }

                    return data;
                }
                catch (Exception e)
                {
                    throw new MerlinException("QE-116", "", e);
                }
            }
            #endregion
        }

        public class NoReturn
        {
            #region ExecuteNonQuery
            /// <summary>
            /// Executes a query without returning any data
            /// </summary>
            /// <param name="queryObj">Sql Query Object implements IQueryProvider</param>
            public static void ExecuteNonQuery(IMerlinProvider queryObj)
            {
                using (var connection = OpenConnection())
                {
                    ExecuteNonQuery(queryObj, connection);
                }
            }

            /// <summary>
            /// Executes a query without returning any data
            /// </summary>
            /// <param name="queryObj">Sql Query Object implements IQueryProvider</param>
            /// <param name="connection">An Open Connection to Database</param>
            public static void ExecuteNonQuery(IMerlinProvider queryObj, MySqlConnection connection)
            {
                try
                {
                    using (MySqlCommand cmd = CreateCommand(queryObj, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw new MerlinException("QE-118", e.Message, e);
                }
            }
            #endregion
        }
    }
}
