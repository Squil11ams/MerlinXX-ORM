using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Used to provide a query and parameters for database operations.
    /// </summary>
    public interface IMerlinProvider
    {
        /// <summary>
        /// SQL query or command text to be executed against the database.
        /// </summary>
        string Query { get; }

        /// <summary>
        /// List of parameters to be used with the query, allowing for parameterized queries and preventing SQL injection.
        /// </summary>
        IEnumerable<IDataParameter> Parameters { get; }

        /// <summary>
        /// Specifies how the provider should interpret <see cref="Query"/>.
        /// Existing implementations default to a text command.
        /// </summary>
        CommandType CommandType => CommandType.Text;

        /// <summary>
        /// Controls how missing result columns are handled. Defaults to strict mapping
        /// so existing query-provider implementations retain their current behavior.
        /// </summary>
        MappingStrictness MappingStrictness => MappingStrictness.Strict;
    }
}
