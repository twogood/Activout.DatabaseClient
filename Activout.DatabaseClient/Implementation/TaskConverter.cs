using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Activout.DatabaseClient.Implementation
{
    /*
     * Convert from Task<object> to Task<T> where T is the Type
     * 
     * Implemented by executing Task<object>.ContinueWith<T>(x => (T)x.Result) using reflection.
     */
    internal class TaskConverter : ITaskConverter
    {
        private static readonly Type ObjectTaskType = typeof(Task<object>);
        private readonly MethodInfo _continueWith;
        private readonly Delegate _lambda;

        public TaskConverter(Type actualReturnType)
        {
            _lambda = CreateLambda(actualReturnType);
            _continueWith = GetContinueWithMethod(actualReturnType);
        }

        public object ConvertReturnType<T>(Task<T> task) where T : class
        {
            return _continueWith.Invoke(task, new object[] {_lambda});
        }

        private static MethodInfo GetContinueWithMethod(Type actualReturnType)
        {
            // Inspired by https://stackoverflow.com/a/3632196/20444
            var baseFuncType = typeof(Func<,>);
            var continueWithMethod = ObjectTaskType.GetMethods()
                .Where(x => x.Name == nameof(Task.ContinueWith) && x.GetParameters().Length == 1)
                .Select(x => new {M = x, P = x.GetParameters()})
                .Where(x => x.P[0].ParameterType.IsGenericType &&
                            x.P[0].ParameterType.GetGenericTypeDefinition() == baseFuncType)
                .Select(x => new {x.M, A = x.P[0].ParameterType.GetGenericArguments()})
                .Where(x => x.A[0].IsGenericType
                            && x.A[0].GetGenericTypeDefinition() == typeof(Task<>))
                .Select(x => x.M)
                .SingleOrDefault();
            Debug.Assert(continueWithMethod != null, nameof(continueWithMethod) + " != null");
            return continueWithMethod.MakeGenericMethod(actualReturnType);
        }

        private static Delegate CreateLambda(Type actualReturnType)
        {
            var constantExpression = Expression.Parameter(ObjectTaskType);
            var propertyExpression = Expression.Property(constantExpression, "Result");
            var conversion = Expression.Convert(propertyExpression, actualReturnType);

            var lambdaMethod = GetLambdaMethod() ??
                               throw new NullReferenceException("Failed to get Expression.Lambda method");
            var lambda = InvokeLambdaMethod(lambdaMethod, conversion, constantExpression);
            return lambda.Compile();
        }

        private static LambdaExpression InvokeLambdaMethod(MethodBase lambdaMethod, UnaryExpression expression,
            ParameterExpression parameter)
        {
            return (LambdaExpression) lambdaMethod.Invoke(null, new object[] {expression, new[] {parameter}});
        }

        private static MethodInfo GetLambdaMethod()
        {
            return typeof(Expression)
                .GetMethods()
                .Single(x => x.Name == nameof(Expression.Lambda)
                             && !x.IsGenericMethod
                             && x.GetParameters().Length == 2
                             && x.GetParameters()[0].ParameterType == typeof(Expression)
                             && x.GetParameters()[1].ParameterType == typeof(ParameterExpression[]));
        }
    }
}