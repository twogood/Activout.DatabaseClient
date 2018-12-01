namespace Activout.DatabaseClient.Attributes
{
    public class SqlUpdateAttribute : AbstractSqlAttribute
    {
        public SqlUpdateAttribute(string sql) : base(sql)
        {
        }
    }
}