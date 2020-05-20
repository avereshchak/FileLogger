using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using MsLogging.FileLogger.Implementation;

namespace MsLogging.FileLogger
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly ILogFile logFile;
        private readonly BlockingCollection<string> messages;
        private readonly ConcurrentDictionary<string, Implementation.FileLogger> loggers;
        private readonly Thread messagesWriter;
        private bool disposed;

        public FileLoggerProvider(ILogFile logFile)
        {
            this.logFile = logFile ?? throw new ArgumentNullException(nameof(logFile));
            messages = new BlockingCollection<string>();
            loggers = new ConcurrentDictionary<string, Implementation.FileLogger>();

            messagesWriter = new Thread(ThreadFunc) {IsBackground = true};
            messagesWriter.Start();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            messages.CompleteAdding();
            messagesWriter.Join();
            messages.Dispose();
            logFile.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new Implementation.FileLogger(name, messages));
        }

        private void ThreadFunc()
        {
            foreach (var message in messages.GetConsumingEnumerable())
            {
                logFile.Write(message, messages.Count == 0);
            }
        }
    }
}