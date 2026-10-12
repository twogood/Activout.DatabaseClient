using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;

namespace Activout.DatabaseClient.Implementation;

/// <summary>Executes the SQL for one DAO interface method.</summary>
internal class MethodHandler
{
    private readonly MethodInfo _method;
    private readonly AbstractSqlAttribute _sqlAttribute;
    private readonly IDatabaseGateway _gateway;
    private readonly bool _isResultEnumerable;
    private readonly bool _isUpdate;
    private readonly Type _effectiveType;
    private readonly bool _isAsync;
    private readonly Func<Task<object?>, object>? _taskConverter;
    private readonly int _transactionIndex;
    private readonly bool _isArray;
    private readonly Type _resultType;

    private static readonly HashSet<Type> SupportedCollectionDefinitions =
    [
        typeof(IEnumerable<>), typeof(IReadOnlyCollection<>), typeof(IReadOnlyList<>),
        typeof(ICollection<>), typeof(IList<>), typeof(List<>)
    ];

    /// <summary>Creates a handler for <paramref name="method"/>.</summary>
    /// <param name="method">The DAO interface method.</param>
    /// <param name="sqlAttribute">The SQL attribute on the method.</param>
    /// <param name="gateway">The gateway that executes the SQL.</param>
    public MethodHandler(MethodInfo method, AbstractSqlAttribute sqlAttribute, IDatabaseGateway gateway)
    {
        Type resultType;
        _method = method;
        _sqlAttribute = sqlAttribute;
        _isUpdate = _sqlAttribute is SqlUpdateAttribute;
        _gateway = gateway;
        _transactionIndex = Array.FindIndex(method.GetParameters(),
            p => typeof(IDbTransaction).IsAssignableFrom(p.ParameterType));

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

        if (_isAsync)
        {
            _taskConverter = resultType == typeof(void)
                ? null
                : TaskConverter.Create(resultType);
        }

        var elementType = GetElementType(resultType, out _isArray);
        _isResultEnumerable = elementType != null;
        _resultType = resultType;
        _effectiveType = elementType ?? resultType;
    }

    private static Type? GetElementType(Type resultType, out bool isArray)
    {
        isArray = false;
        if (resultType.IsArray)
        {
            if (resultType.GetArrayRank() != 1 || resultType == typeof(byte[])) return null;
            isArray = true;
            return resultType.GetElementType();
        }

        if (!resultType.IsGenericType) return null;

        var definition = resultType.GetGenericTypeDefinition();
        if (SupportedCollectionDefinitions.Contains(definition))
        {
            return resultType.GenericTypeArguments[0];
        }

        if (resultType.Namespace is { } ns && ns.StartsWith("System.Collections", StringComparison.Ordinal) &&
            resultType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            throw new NotSupportedException(
                $"Unsupported collection return type {resultType}. Supported types are IEnumerable<T>, " +
                "IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>, List<T> and T[].");
        }

        return null;
    }

    /// <summary>Executes the SQL with the given method arguments.</summary>
    /// <param name="args">The method arguments.</param>
    /// <returns>The method return value.</returns>
    public object? Call(object?[] args)
    {
        var statement = new SqlStatement
        {
            Sql = _sqlAttribute.Sql,
            EffectiveType = _effectiveType,
            Transaction = _transactionIndex >= 0 ? (IDbTransaction?)args[_transactionIndex] : null
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
            throw e.InnerException ?? e;
        }
    }

    private object? QueryFirstOrDefault(SqlStatement statement)
    {
        var task = _gateway.QueryFirstOrDefaultAsync(statement);
        return _isAsync ? _taskConverter!(task) : task.Result;
    }

    private object? Query(SqlStatement statement)
    {
        var task = QueryAsync(statement);
        return _isAsync ? _taskConverter!(task) : task.Result;
    }

    private async Task<object?> QueryAsync(SqlStatement statement)
    {
        return CastEnumerable(await _gateway.QueryAsync(statement).ConfigureAwait(false));
    }

    private object? CastEnumerable(IEnumerable<object> enumerable)
    {
        var castMethod = typeof(Enumerable).GetMethod("Cast")!.MakeGenericMethod(_effectiveType);
        var cast = castMethod.Invoke(null, [enumerable])!;
        if (_resultType.IsGenericType && _resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return cast;
        }

        var materialize = _isArray ? "ToArray" : "ToList";
        return typeof(Enumerable).GetMethod(materialize)!.MakeGenericMethod(_effectiveType)
            .Invoke(null, [cast]);
    }

    private object Execute(SqlStatement statement)
    {
        var task = _gateway.ExecuteAsync(statement);
        return _isAsync ? task : task.Result;
    }

    private void AddSqlStatementParameters(object?[] args, SqlStatement statement)
    {
        var parameters = _method.GetParameters();
        for (var index = 0; index < parameters.Length; index++)
        {
            if (index == _transactionIndex) continue;

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
        return bind?.ParameterName ??
               parameter.Name ??
               throw new InvalidOperationException("Parameter name is null.");
    }

    private static string GetName(MemberInfo member)
    {
        var bind = member.GetCustomAttribute<BindAttribute>();
        return bind?.ParameterName ?? member.Name;
    }
}