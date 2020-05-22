using System;
using System.IO;

namespace MsLogging.FileLogger.Implementation
{
    internal class LogFile : ILogFile
    {
        private readonly IFileSystem fileSystem;
        private readonly string name;
        private readonly int maxSize;
        private StreamWriter currentWriter;
        private Stream currentStream;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="name"></param>
        /// <param name="maxSize">File size limit. Set 0 to make it unlimited.</param>
        public LogFile(IFileSystem fileSystem, string name, int maxSize)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            
            this.name = name;
            this.maxSize = maxSize;
        }

        public void Write(string message, bool flush)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var writer = GetFileWriter();

            if (maxSize > 0 && currentStream.Length + message.Length > maxSize)
            {
                ArchiveCurrent();
                writer = GetFileWriter();
            }
            
            writer.WriteLine(message);

            if (flush)
            {
                writer.Flush();
            }
        }

        public void Dispose()
        {
            FlushAndClose();
        }

        private void FlushAndClose()
        {
            if (currentWriter != null)
            {
                // Dispose() method is also flushing the writer's buffer.
                currentWriter.Dispose();
                currentWriter = null;
            }

            if (currentStream != null)
            {
                // Stream is flushed automatically by writer.
                currentStream.Dispose();
                currentStream = null;
            }
        }

        private StreamWriter GetFileWriter()
        {
            if (currentWriter == null)
            {
                currentStream = fileSystem.OpenWrite(name);
                currentWriter = new StreamWriter(currentStream)
                {
                    AutoFlush = false
                };
            }

            return currentWriter;
        }

        private void ArchiveCurrent()
        {
            FlushAndClose();

            fileSystem.Archive(name);
        }
    }
}
