using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation
{
    public class MethodHandler
    {
        private readonly MethodInfo _method;
        private readonly AbstractSqlAttribute _sqlAttribute;
        private readonly DatabaseClientContext _context;
        private readonly bool _isResultEnumerable;
        private readonly bool _isUpdate;
        private readonly Type _effectiveType;

        public MethodHandler(MethodInfo method, AbstractSqlAttribute sqlAttribute, DatabaseClientContext context)
        {
            _method = method;
            _sqlAttribute = sqlAttribute;
            _isUpdate = _sqlAttribute is SqlUpdateAttribute;
            _context = context;

            var returnType = _method.ReturnType;

            _isResultEnumerable = returnType.IsGenericType &&
                                  returnType.Namespace == "System.Collections.Generic" &&
                                  returnType.Name == "IEnumerable`1";

            if (_isResultEnumerable)
            {
                _effectiveType = returnType.GenericTypeArguments[0];
            }
            else
            {
                _effectiveType = returnType;
            }
        }

        public object Call(object[] args)
        {
            var statement = new SqlStatement(_context.Connection)
            {
                Sql = _sqlAttribute.Sql,
                EffectiveType = _effectiveType
            };

            AddSqlStatementParameters(args, statement);

            if (_isUpdate)
            {
                return _context.Gateway.ExecuteAsync(statement).Result;
            }

            if (_isResultEnumerable)
            {
                return _context.Gateway.QueryAsync(statement).Result;
            }

            return _context.Gateway.QueryFirstOrDefaultAsync(statement).Result;
        }

        private void AddSqlStatementParameters(IReadOnlyList<object> args, SqlStatement statement)
        {
            var parameters = _method.GetParameters();
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var value = args[index];

                var bind = parameter.GetCustomAttribute<BindAttribute>();
                var parameterName = bind?.ParameterName ?? parameter.Name;

                var bindProperties = parameter.GetCustomAttribute<BindPropertiesAttribute>();
                if (bindProperties == null)
                {
                    statement.Parameters.Add(new QueryParameter(parameterName, value));
                }
                else
                {
                    var properties = value.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        var propertyBind = property.GetCustomAttribute<BindAttribute>();
                        var propertyName = propertyBind?.ParameterName ?? property.Name;

                        var propertyValue = property.GetValue(value);
                        statement.Parameters.Add(new QueryParameter(propertyName, propertyValue));
                        statement.Parameters.Add(new QueryParameter($"{parameterName}_{propertyName}", propertyValue));
                    }
                }
            }
        }
    }
}