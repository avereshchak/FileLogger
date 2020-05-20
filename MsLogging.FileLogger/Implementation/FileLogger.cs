using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MsLogging.FileLogger.Implementation
{
    internal class FileLogger : ILogger
    {
        private readonly string name;
        private readonly BlockingCollection<string> logMessages;

        public FileLogger(string name, BlockingCollection<string> logMessages)
        {
            this.name = name;
            this.logMessages = logMessages ?? throw new ArgumentNullException(nameof(logMessages));
        }

        public void Log<TState>(
            LogLevel logLevel, 
            EventId eventId, 
            TState state, 
            Exception exception, 
            Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            try
            {
                if (logMessages.IsAddingCompleted)
                    return;

                var formatted = formatter(state, exception);

                var builder = new StringBuilder();
         
                builder.Append(DateTime.Now.ToString("s"))
                    .Append("|")
                    .Append(logLevel)
                    .Append("|")
                    .Append(name)
                    .Append("|")
                    .Append(formatted);

                if (exception != null)
                {
                    builder.Append("|").Append(exception);
                }

                var message = builder.ToString();
                logMessages.Add(message);
            }
            catch (InvalidOperationException)
            {
                // Adding is no longer allowed or the collection has been just disposed.
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        private class NullDisposable : IDisposable
        {
            private NullDisposable()
            {
            }

            public static NullDisposable Instance { get; } = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}