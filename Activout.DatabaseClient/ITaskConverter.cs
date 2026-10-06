using System.Threading.Tasks;

namespace Activout.DatabaseClient;

internal interface ITaskConverter
{
    object? ConvertReturnType(Task<object?> task);
}