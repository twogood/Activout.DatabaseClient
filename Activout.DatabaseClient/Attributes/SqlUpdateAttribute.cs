namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Executes a statement that does not return rows, such as <c>INSERT</c>, <c>UPDATE</c> or DDL.
/// The method returns <c>void</c> or <c>Task</c>, or the number of affected rows as <c>int</c> or <c>Task&lt;int&gt;</c>.
/// </summary>
public class SqlUpdateAttribute : AbstractSqlAttribute
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="sql">The statement to execute.</param>
    public SqlUpdateAttribute(string sql) : base(sql)
    {
    }
}
