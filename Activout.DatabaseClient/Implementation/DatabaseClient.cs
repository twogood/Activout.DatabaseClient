using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation;

public class DatabaseClient<T>(DatabaseClientContext context) : DynamicObject
    where T : class
{
    private readonly Type _type = typeof(T);
    private readonly ConcurrentDictionary<MethodInfo, MethodHandler> _methodHandlers = new();

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        args ??= [];
        var method = _type.GetTypeInfo()
            .GetDeclaredMethods(binder.Name)
            .Single(mi => mi.GetParameters().Length == args.Length);

        if (!_methodHandlers.TryGetValue(method, out var methodHandler))
        {
            var sqlAttribute = method.GetCustomAttribute<AbstractSqlAttribute>();
            if (sqlAttribute == null)
            {
                result = null;
                return false;
            }

            methodHandler = new MethodHandler(method, sqlAttribute, context);
            _methodHandlers[method] = methodHandler;
        }

        result = methodHandler.Call(args);
        return true;
    }
}