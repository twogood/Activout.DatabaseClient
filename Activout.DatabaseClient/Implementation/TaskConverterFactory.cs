using System;

namespace Activout.DatabaseClient.Implementation
{
    [Obsolete("Use TaskConverterFactory3 instead")]
    public class TaskConverterFactory : ITaskConverterFactory
    {
        public ITaskConverter CreateTaskConverter(Type actualReturnType)
        {
            return actualReturnType == typeof(void) ? null : new TaskConverter(actualReturnType);
        }
    }
}