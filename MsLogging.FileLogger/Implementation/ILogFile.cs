using System;

namespace MsLogging.FileLogger.Implementation
{
    public interface ILogFile : IDisposable
    {
        void Write(string message, bool flush);
    }
}