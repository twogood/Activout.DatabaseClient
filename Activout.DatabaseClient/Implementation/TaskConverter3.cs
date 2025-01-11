using System.Diagnostics;
using System.Threading.Tasks;

namespace Activout.DatabaseClient.Implementation;

/*
 * Convert from Task<object> to Task<T> where T is the Type
 */
public class TaskConverter3<T> : ITaskConverter
{
    [StackTraceHidden]
    public object ConvertReturnType(Task<object> task)
    {
        return ConvertReturnTypeImpl(task);
    }

    [StackTraceHidden]
    private static async Task<T> ConvertReturnTypeImpl(Task<object> task)
    {
        return (T)await task;
    }
}