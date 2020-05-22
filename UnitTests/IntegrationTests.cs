using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsLogging.FileLogger;

namespace UnitTests
{
    [TestClass]
    public class IntegrationTests
    {
        private string logFile = "all.log";
        private int maxSize = 10_000;
        private string loggerName = "MyLogger";

        [TestInitialize]
        public void Setup()
        {
            DeleteLogFiles();
        }

        [TestCleanup]
        public void Cleanup()
        {
            DeleteLogFiles();
        }

        [Ignore]
        [DataTestMethod]
        [DataRow(1000000)]
        [DataRow(16384)]
        [DataRow(8192)]
        [DataRow(4096)]
        [DataRow(2048)]
        [DataRow(1024)]
        [DataRow(512)]
        [DataRow(256)]
        [DataRow(128)]
        [DataRow(64)]
        [DataRow(32)]
        [DataRow(1)]
        public void PerformanceTests(int maxBufferedMessages)
        {
            Action act = () => DoTest(maxBufferedMessages);

            act.Measure(20);
        }

        private void DoTest(int maxBufferedMessages)
        {
            var container = new ServiceCollection();
            container.AddLogging(builder => builder.AddFile(logFile, maxSize, maxBufferedMessages));
            const int numLines = 20000;

            using (var services = container.BuildServiceProvider())
            {
                var factory = services.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger(loggerName);

                for (var i = 0; i < numLines; i++)
                {
                    logger.LogInformation($"Message #{i}");
                }
            }

            // The last message must be in the current log file.
            var content = ReadAllText(logFile);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lastLine = lines.Last();
            lastLine.Should().Contain($"Message #{numLines - 1}");
        }

        private string ReadAllText(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void AssertNoFile()
        {
            File.Exists(logFile).Should().BeFalse();
        }

        private void AssertLogFileCreated()
        {
            File.Exists(logFile).Should().BeTrue();
        }

        private void AssertMultipleLogFiles()
        {
            var logs = GetAllLogs();
            logs.Length.Should().BeGreaterThan(1);
        }

        private string[] GetAllLogs()
        {
            var logDirectory = Path.GetDirectoryName(logFile);
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                logDirectory = Directory.GetCurrentDirectory();
            }

            var logs = Directory.GetFiles(logDirectory, "*.log");
            return logs;
        }

        private void DeleteLogFiles()
        {
            var allLogs = GetAllLogs();
            foreach (var filePath in allLogs)
            {
                File.Delete(filePath);
            }
        }
    }

    static class MeasureExtensions
    {
        public static void Measure(this Action action, int times)
        {
            long average = 0;
            for (var i = 0; i < times; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                action();

                average = (average + stopwatch.ElapsedMilliseconds) / 2;
            }

            Trace.WriteLine($"Average: {average} ms.");
        }
    }
}
