using System;

namespace Activout.DatabaseClient.Implementation;

public class TaskConverter3Factory : ITaskConverterFactory
{
    public ITaskConverter CreateTaskConverter(Type actualReturnType)
    {
        return actualReturnType == typeof(void) ? null : 
            (ITaskConverter) Activator.CreateInstance(typeof(TaskConverter3<>).MakeGenericType(actualReturnType));
    }
}