using System;
using System.Collections.Concurrent;
using System.Reflection;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation;

public class DatabaseClient : DispatchProxy
{
    private readonly ConcurrentDictionary<MethodInfo, MethodHandler> _methodHandlers = new();
    private IDatabaseGateway _gateway = null!;

    internal static T Create<T>(IDatabaseGateway gateway) where T : class
    {
        var proxy = Create<T, DatabaseClient>();
        ((DatabaseClient)(object)proxy)._gateway = gateway;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var handler = _methodHandlers.GetOrAdd(targetMethod!, method =>
            new MethodHandler(method,
                method.GetCustomAttribute<AbstractSqlAttribute>() ??
                throw new NotSupportedException($"{method.Name} has no SQL attribute"),
                _gateway));
        return handler.Call(args ?? []);
    }
}
