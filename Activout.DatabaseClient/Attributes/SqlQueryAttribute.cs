namespace Activout.DatabaseClient.Attributes
{
    public class SqlQueryAttribute : AbstractSqlAttribute
    {
        public SqlQueryAttribute(string sql) : base(sql)
        {
        }
    }
}