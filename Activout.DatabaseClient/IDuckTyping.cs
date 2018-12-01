namespace Activout.DatabaseClient
{
    public interface IDuckTyping
    {
        TInterface DuckType<TInterface>(object originalDynamic) where TInterface : class;
    }
}