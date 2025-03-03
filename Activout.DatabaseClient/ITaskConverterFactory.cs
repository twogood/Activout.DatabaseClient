using System;

namespace Activout.DatabaseClient;

public interface ITaskConverterFactory
{
    ITaskConverter? CreateTaskConverter(Type actualReturnType);
}