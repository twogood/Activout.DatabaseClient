using System;

namespace Activout.DatabaseClient.Implementation
{
    public class TaskConverterFactory : ITaskConverterFactory
    {
        public ITaskConverter CreateTaskConverter(Type actualReturnType)
        {
            return actualReturnType == typeof(void) ? null : new TaskConverter(actualReturnType);
        }
    }
}