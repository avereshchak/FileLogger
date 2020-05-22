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

        public FileLoggerProvider(ILogFile logFile, int maxBufferedMessages)
        {
            this.logFile = logFile ?? throw new ArgumentNullException(nameof(logFile));
            messages = new BlockingCollection<string>(maxBufferedMessages);
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
                var flush = messages.Count == 0;

                try
                {
                    logFile.Write(message, flush);
                }
                catch
                {
                    // The write operation outcome is unknown here, i.e. we cannot say
                    // the message was written or not. Thus do not retry the operation.
                    // On the other hand, it could be a transient error and the logging must not stop.
                }
            }
        }
    }
}