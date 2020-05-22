using System;
using System.IO;

 namespace MsLogging.FileLogger.Implementation
{
    internal class FileSystem : IFileSystem
    {
        public Stream OpenWrite(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            
            var stream = new FileStream(name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            stream.Seek(0, SeekOrigin.End);
            return stream;
        }

        public void Archive(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            if (!File.Exists(name))
                return;

            var archiveName = BuildArchiveName(name);
            File.Move(name, archiveName);
        }

        internal string BuildArchiveName(string sourceName)
        {
            var timestamp = DateTime.UtcNow.ToString("o")
                .Replace('-', '_')
                .Replace(':', '_');

            var directoryName = Path.GetDirectoryName(sourceName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sourceName);
            var archiveExtension = Path.GetExtension(sourceName);

            var archiveName = $"{nameWithoutExtension}_{timestamp}{archiveExtension}";
            var archivePath = Path.Combine(directoryName, archiveName);
            return archivePath;
        }
    }
}