using System;

namespace Activout.DatabaseClient.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public abstract class AbstractSqlAttribute(string sql) : Attribute
{
    public string Sql { get; } = sql;
}