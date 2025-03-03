using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        private readonly bool _isAsync;
        private readonly ITaskConverter _taskConverter;

        public MethodHandler(MethodInfo method, AbstractSqlAttribute sqlAttribute, DatabaseClientContext context)
        {
            Type resultType;
            _method = method;
            _sqlAttribute = sqlAttribute;
            _isUpdate = _sqlAttribute is SqlUpdateAttribute;
            _context = context;

            var returnType = _method.ReturnType;
            if (returnType == typeof(Task))
            {
                resultType = typeof(void);
                _isAsync = true;
            }
            else if (returnType.BaseType == typeof(Task) && returnType.IsGenericType)
            {
                resultType = returnType.GenericTypeArguments[0];
                _isAsync = true;
            }
            else
            {
                resultType = returnType;
                _isAsync = false;
            }

            _taskConverter = _context.TaskConverterFactory.CreateTaskConverter(resultType);

            _isResultEnumerable = resultType.IsGenericType &&
                                  resultType.Namespace == "System.Collections.Generic" &&
                                  resultType.Name == "IEnumerable`1";

            if (_isResultEnumerable)
            {
                _effectiveType = resultType.GenericTypeArguments[0];
            }
            else
            {
                _effectiveType = resultType;
            }
        }

        public object Call(object[] args)
        {
            var statement = new SqlStatement
            {
                Sql = _sqlAttribute.Sql,
                EffectiveType = _effectiveType
            };

            AddSqlStatementParameters(args, statement);

            try
            {
                if (_isUpdate)
                {
                    return Execute(statement);
                }

                if (_isResultEnumerable)
                {
                    return Query(statement);
                }

                return QueryFirstOrDefault(statement);
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        private object QueryFirstOrDefault(SqlStatement statement)
        {
            var task = _context.Gateway.QueryFirstOrDefaultAsync(statement);
            return _isAsync ? _taskConverter.ConvertReturnType(task) : task.Result;
        }

        private object Query(SqlStatement statement)
        {
            var task = QueryAsync(statement);
            return _isAsync ? _taskConverter.ConvertReturnType(task) : task.Result;
        }

        private async Task<object> QueryAsync(SqlStatement statement)
        {
            return CastEnumerable(await _context.Gateway.QueryAsync(statement).ConfigureAwait(false));
        }

        private object CastEnumerable(IEnumerable<object> enumerable)
        {
            var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(_effectiveType);
            return castMethod.Invoke(null, new object[] { enumerable });
        }

        private object Execute(SqlStatement statement)
        {
            var task = _context.Gateway.ExecuteAsync(statement);
            return _isAsync ? task : (object)task.Result;
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

                    foreach (var property in GetProperties(parameter))
                    {
                        var propertyName = GetName(property);

                        var propertyValue = property.GetValue(value);
                        statement.Parameters.Add(new QueryParameter(propertyName, propertyValue));
                        statement.Parameters.Add(new QueryParameter($"{parameterName}_{propertyName}", propertyValue));
                    }
                }
            }
        }

        [SuppressMessage("SonarCloud", "S1523")]
        private static IEnumerable<PropertyInfo> GetProperties(ParameterInfo parameter)
        {
            return parameter.ParameterType.GetProperties();
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