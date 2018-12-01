using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClient<T> : DynamicObject where T : class
    {
        private readonly DatabaseClientContext _context;
        private readonly Type _type;

        private readonly IDictionary<MethodInfo, MethodHandler> _methodHandlers =
            new ConcurrentDictionary<MethodInfo, MethodHandler>();

        public DatabaseClient(DatabaseClientContext context)
        {
            _type = typeof(T);
            _context = context;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var method = _type.GetTypeInfo()
                .GetDeclaredMethods(binder.Name)
                .SingleOrDefault(mi => mi.GetParameters().Length == args.Length);
            if (method == null)
            {
                result = null;
                return false;
            }

            if (!_methodHandlers.TryGetValue(method, out var methodHandler))
            {
                var sqlAttribute = method.GetCustomAttribute<AbstractSqlAttribute>();
                if (sqlAttribute == null)
                {
                    result = null;
                    return false;
                }

                methodHandler = new MethodHandler(method, sqlAttribute, _context);
                _methodHandlers[method] = methodHandler;
            }

            result = methodHandler.Call(args);
            return true;
        }
    }
}