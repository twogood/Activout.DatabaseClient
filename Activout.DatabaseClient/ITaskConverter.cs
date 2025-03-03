using System.Threading.Tasks;

namespace Activout.DatabaseClient
{
    public interface ITaskConverter
    {
        object? ConvertReturnType(Task<object?> task);
    }
}