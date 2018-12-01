using System;

namespace Activout.DatabaseClient.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class AbstractSqlAttribute : Attribute
    {
        public string Sql { get; }

        protected AbstractSqlAttribute(string sql)
        {
            Sql = sql;
        }
    }
}