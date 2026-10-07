using System;

namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Base class for attributes that attach SQL to a DAO interface method.
/// </summary>
/// <param name="sql">The SQL to execute when the method is called.</param>
[AttributeUsage(AttributeTargets.Method)]
public abstract class AbstractSqlAttribute(string sql) : Attribute
{
    /// <summary>The SQL to execute when the method is called.</summary>
    public string Sql { get; } = sql;
}
