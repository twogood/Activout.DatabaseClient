using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Activout.DatabaseClient.Implementation;

/*
 * Convert from Task<object?> to Task<T?> where T is the actual return type
 */
internal static class TaskConverter
{
    private static readonly MethodInfo ConvertMethod =
        typeof(TaskConverter).GetMethod(nameof(Convert), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static Func<Task<object?>, object> Create(Type actualReturnType) =>
        (Func<Task<object?>, object>)ConvertMethod.MakeGenericMethod(actualReturnType)
            .CreateDelegate(typeof(Func<Task<object?>, object>));

    // Binds to Func<Task<object?>, object> via return type covariance (Task<T?> is a reference type).
    [StackTraceHidden]
    private static async Task<T?> Convert<T>(Task<object?> task) => (T?)await task;
}
