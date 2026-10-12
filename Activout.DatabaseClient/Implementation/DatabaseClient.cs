using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Reflection;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation;

/// <summary>
/// The proxy that implements DAO interfaces. Use <see cref="DatabaseClientBuilder"/> to create one.
/// </summary>
internal class DatabaseClient : DispatchProxy
{
    private readonly ConcurrentDictionary<MethodInfo, MethodHandler> _methodHandlers = new();
    private IDatabaseGateway _gateway = null!;

    internal static T Create<T>(IDatabaseGateway gateway) where T : class
    {
        var proxy = Create<T, DatabaseClient>();
        var client = (DatabaseClient)(object)proxy;
        client._gateway = gateway;
        client.ValidateMethods(typeof(T));
        return proxy;
    }

    private void ValidateMethods(Type daoType)
    {
        foreach (var type in new[] { daoType }.Concat(daoType.GetInterfaces()))
        {
            foreach (var method in type.GetMethods())
            {
                if (method.GetCustomAttribute<AbstractSqlAttribute>() != null)
                {
                    GetHandler(method);
                }
            }
        }
    }

    private MethodHandler GetHandler(MethodInfo method) =>
        _methodHandlers.GetOrAdd(method, m =>
            new MethodHandler(m,
                m.GetCustomAttribute<AbstractSqlAttribute>() ??
                throw new NotSupportedException($"{m.Name} has no SQL attribute"),
                _gateway));

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.DeclaringType == typeof(IWithTransactions))
        {
            return _gateway.BeginTransaction((IsolationLevel)args![0]!);
        }

        var handler = GetHandler(targetMethod);
        return handler.Call(args ?? []);
    }
}
