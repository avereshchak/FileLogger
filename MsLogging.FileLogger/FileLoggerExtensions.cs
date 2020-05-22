using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsLogging.FileLogger.Implementation;

namespace MsLogging.FileLogger
{
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// Add file logger.
        /// </summary>
        /// <param name="builder">A reference to logging builder.</param>
        /// <param name="name">A desired file name. Could also be a relative or absolute file path, with or without extension.</param>
        /// <param name="maxSize">A max log file size after which the current log is archived and new one is created. Use 0 for unlimited size.</param>
        /// <param name="maxBufferedMessages">How many messages could be buffered in memory before throttling the log clients.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string name, int maxSize, int maxBufferedMessages = 1024)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (maxSize < 0) throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be a non-negative number.");
            if (maxBufferedMessages < 1) throw new ArgumentOutOfRangeException(nameof(maxBufferedMessages), "Must be a positive number.");
            
            var fileSystem = new FileSystem();
            var logFile = new LogFile(fileSystem, name, maxSize);
            builder.Services.AddSingleton<ILoggerProvider>(c => new FileLoggerProvider(logFile, maxBufferedMessages));

            return builder;
        }
    }
}
