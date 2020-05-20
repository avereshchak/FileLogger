using System;
using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsLogging.FileLogger.Implementation;

namespace UnitTests
{
    [TestClass]
    public class FileLoggerTests
    {
        private BlockingCollection<string> logMessages;
        private FileLogger sut;
        private const string Name = "name";

        [TestInitialize]
        public void Setup()
        {
            logMessages = new BlockingCollection<string>();
            sut = new FileLogger(Name, logMessages);
        }

        [TestMethod]
        public void Ctor_NullMessagesCollection_Throws()
        {
            Action act = () => new FileLogger(Name, null);

            act.Should().Throw<ArgumentNullException>("*logMessages*");
        }

        [DataTestMethod]
        [DataRow(LogLevel.None, false)]
        [DataRow(LogLevel.Trace, true)]
        [DataRow(LogLevel.Debug, true)]
        [DataRow(LogLevel.Information, true)]
        [DataRow(LogLevel.Warning, true)]
        [DataRow(LogLevel.Error, true)]
        [DataRow(LogLevel.Critical, true)]
        public void IsEnabled_TrueExceptNone(LogLevel level, bool expected)
        {
            var actual = sut.IsEnabled(level);

            actual.Should().Be(expected);
        }

        [TestMethod]
        public void Log_Message_Queued()
        {
            const string message = "custom message";

            TestLog(message, message);
        }

        [TestMethod]
        public void Log_MessageWithOrdinalFormat_Queued()
        {
            TestLog("Message from user", "Message from {0}", "user");
        }

        [TestMethod]
        public void Log_StructuredMessage_Queued()
        {
            TestLog("Message from user", "Message from {user}", "user");
        }

        [TestMethod]
        public void Log_WithException_Queued()
        {
            sut.LogError(new Exception("exception message"), "my error");

            var logged = logMessages.TryTake(out var actual, TimeSpan.Zero);
            logged.Should().BeTrue();
            actual.Should().Contain("exception message").And.Contain("my error");
        }

        [TestMethod]
        public void Log_MixOfStructuredAndOrdinal_Queued()
        {
            TestLog("Message from user with email@domain.com with 1000 USD", "Message from {0} with {email} with {1} USD", "user", "email@domain.com", 1000);
        }

        [TestMethod]
        public void Log_AddingCompleted_NothingLogged()
        {
            logMessages.CompleteAdding();

            sut.LogInformation("Shouldn't be logged");

            logMessages.IsCompleted.Should().BeTrue();
            logMessages.Count.Should().Be(0);
        }

        [TestMethod]
        public void Log_CollectionDisposed_NoLoggerExceptions()
        {
            logMessages.Dispose();

            sut.LogInformation("Shouldn't be logged");
        }

        [TestMethod]
        public void BeginScope_ReturnsDisposable()
        {
            using (var disposable = sut.BeginScope(new object()))
            {
                disposable.Should().NotBeNull();
            }
        }
        
        private void TestLog(string expected, string message, params object[] args)
        {
            sut.LogInformation(message, args);

            var logged = logMessages.TryTake(out var actual, TimeSpan.Zero);
            logged.Should().BeTrue();

            actual.Should().Contain(expected);
        }
    }
}
