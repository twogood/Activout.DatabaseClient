using ImpromptuInterface;

namespace Activout.DatabaseClient.Implementation;

public class DuckTyping : IDuckTyping
{
    public TInterface DuckType<TInterface>(object originalDynamic) where TInterface : class
    {
        return originalDynamic.ActLike<TInterface>();
    }
}