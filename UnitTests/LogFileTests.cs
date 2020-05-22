using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsLogging.FileLogger.Implementation;

namespace UnitTests
{
    [TestClass]
    public class LogFileTests
    {
        private IFileSystem fileSystem;
        private string fileName;
        private int maxSize;
        private LogFile sut;
        private Logs logs;

        [TestInitialize]
        public void Setup()
        {
            logs = new Logs();
            fileSystem = A.Fake<IFileSystem>();
            A.CallTo(() => fileSystem.OpenWrite(A<string>._)).ReturnsLazily(logs.CreateNewStream);
            fileName = "error.log";
            maxSize = 100;

            sut = new LogFile(fileSystem, fileName, maxSize);
        }

        [TestMethod]
        public void Ctor_NoFileSystem_Exception()
        {
            Action act = () => new LogFile(null, fileName, maxSize);

            act.Should().Throw<ArgumentNullException>("*fileSystem*");
        }

        [TestMethod]
        public void Write_NoMessage_NothingLogged()
        {
            sut.Write(null, false);

            A.CallTo(() => fileSystem.OpenWrite(A<string>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public void Write_FirstOperation_FileOpen()
        {
            sut.Write("message", false);

            A.CallTo(() => fileSystem.OpenWrite(fileName)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public void Write_ExceedsLogSize_OldArchivedNewCreated()
        {
            var message = "message";
            var total = 0;

            while (total < maxSize + 1)
            {
                total += message.Length;
                sut.Write("message", true);
            }

            A.CallTo(() => fileSystem.OpenWrite(fileName)).MustHaveHappened()
                .Then(A.CallTo(() => fileSystem.Archive(fileName)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fileSystem.OpenWrite(fileName)).MustHaveHappened());
        }

        class Logs
        {
            private readonly List<MemoryStream> streams = new List<MemoryStream>();

            public Stream CreateNewStream()
            {
                var stream = new MemoryStream();
                streams.Add(stream);
                return stream;
            }

            public IReadOnlyList<string> AllLines
            {
                get
                {
                    var allLines = new List<string>();
                    foreach (var stream in streams)
                    {
                        var bytes = stream.ToArray();
                        var content = Encoding.UTF8.GetString(bytes);
                        var lines = content.Split('\r', '\n');
                        allLines.AddRange(lines);
                    }
                    return allLines;
                }
            }
        }
    }
}
