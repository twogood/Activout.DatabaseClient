namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Runs a query. A method returning <c>IEnumerable&lt;T&gt;</c> (or a task of one) gets all rows,
/// any other return type gets the first row or <c>default</c>.
/// </summary>
public class SqlQueryAttribute : AbstractSqlAttribute
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="sql">The query to run.</param>
    public SqlQueryAttribute(string sql) : base(sql)
    {
    }
}
