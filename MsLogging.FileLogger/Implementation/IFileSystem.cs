using System.IO;

namespace MsLogging.FileLogger.Implementation
{
    internal interface IFileSystem
    {
        Stream OpenWrite(string name);
        void Archive(string name);
    }
}