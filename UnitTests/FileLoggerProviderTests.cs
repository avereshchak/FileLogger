using System;
using System.Threading;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsLogging.FileLogger;
using MsLogging.FileLogger.Implementation;

namespace UnitTests
{
    [TestClass]
    public class FileLoggerProviderTests
    {
        private ILogFile logFile;

        [TestInitialize]
        public void Setup()
        {
            logFile = A.Fake<ILogFile>();
        }

        [TestMethod]
        public void Ctor_NullFile_Exception()
        {
            Action act = () => new FileLoggerProvider(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            var sut = new FileLoggerProvider(logFile);

            sut.Dispose();
            Action act = () => sut.Dispose();
            
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_ManyLogMessages_WaitsUntilAllWritten()
        {
            // ARRANGE
            var sut = new FileLoggerProvider(logFile);
            var logger = sut.CreateLogger("name");
            var blockWrite = true;
            
            // Imitate a file write delay.
            A.CallTo(() => logFile.Write(A<string>._, A<bool>._)).Invokes(() =>
            {
                while (blockWrite)
                {
                }
            });

            logger.LogInformation("some message");
            var blocked = true;

            // ACT
            // Dispose the provider in background thread and ensure it remains
            // blocked because the file operation is not yet complete.
            ThreadPool.QueueUserWorkItem(state =>
            {
                sut.Dispose();
                blocked = false;
            });

            // ASSERT
            Thread.Sleep(100);
            blocked.Should().BeTrue("The file operation is not yet complete, Dispose() must not return");

            // ACT
            // Now complete the file operation. Since it was the last operation,
            // the Dispose must return as its' background thread has completed everything.
            blockWrite = false;
            Thread.Sleep(100);
            blocked.Should().BeFalse();
        }

        [TestMethod]
        public void Dispose_LogFileDisposed()
        {
            var sut = new FileLoggerProvider(logFile);

            sut.Dispose();

            A.CallTo(() => logFile.Dispose()).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public void CreateLogger_FirstTime_NewLoggerCreated()
        {
            var sut = new FileLoggerProvider(logFile);

            var logger = sut.CreateLogger("name");

            logger.Should().NotBeNull();
        }

        [TestMethod]
        public void CreateLogger_MultipleTimesWithTheSameName_ExistingReturned()
        {
            var sut = new FileLoggerProvider(logFile);
            const string loggerName = "name";

            var logger1 = sut.CreateLogger(loggerName);
            var logger2 = sut.CreateLogger(loggerName);

            logger1.Should().Be(logger2);
        }

        [TestMethod]
        public void CreateLogger_WithName_LoggerNameIsLogged()
        {
            var sut = new FileLoggerProvider(logFile);
            const string loggerName = "my logger name";
            var logger = sut.CreateLogger(loggerName);

            logger.LogInformation("Hello world!");

            A.CallTo(() => logFile.Write(A<string>.That.Contains(loggerName), A<bool>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}
