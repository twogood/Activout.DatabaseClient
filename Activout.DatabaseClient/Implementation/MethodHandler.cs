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

                var parameterName = GetName(parameter);

                var bindProperties = parameter.GetCustomAttribute<BindPropertiesAttribute>();
                if (bindProperties == null)
                {
                    statement.Parameters.Add(new QueryParameter(parameterName, value));
                }
                else
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(parameter.Name,
                            "Value of [BindProperties] parameter cannot be null.");
                    }

                    var properties = value.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        var propertyName = GetName(property);

                        var propertyValue = property.GetValue(value);
                        statement.Parameters.Add(new QueryParameter(propertyName, propertyValue));
                        statement.Parameters.Add(new QueryParameter($"{parameterName}_{propertyName}", propertyValue));
                    }
                }
            }
        }

        private static string GetName(ParameterInfo parameter)
        {
            var bind = parameter.GetCustomAttribute<BindAttribute>();
            return bind?.ParameterName ?? parameter.Name;
        }

        private static string GetName(MemberInfo member)
        {
            var bind = member.GetCustomAttribute<BindAttribute>();
            return bind?.ParameterName ?? member.Name;
        }
    }
}