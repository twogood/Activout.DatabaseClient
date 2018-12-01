using System.Threading.Tasks;

namespace Activout.DatabaseClient
{
    public interface ITaskConverter
    {
        object ConvertReturnType<T>(Task<T> task) where T : class;
    }
}