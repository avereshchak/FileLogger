using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsLogging.FileLogger.Implementation;

namespace UnitTests
{
    [TestClass]
    public class FileSystemTests
    {
        private FileSystem sut;

        [TestInitialize]
        public void Setup()
        {
            sut = new FileSystem();
        }

        [TestMethod]
        public void BuildArchiveName_NameWithoutExtension()
        {
            TestBuildArchiveName("just-name", null);
            var actual = sut.BuildArchiveName("just-name");

            var regex = MakeRegex("just-name", null);
            regex.IsMatch(actual).Should().BeTrue();
        }

        [TestMethod]
        public void BuildArchiveName_NameWithExtension()
        {
            TestBuildArchiveName("some-name", "log");
        }

        [TestMethod]
        public void BuildArchiveName_AbsolutePathWithoutExtension()
        {
            TestBuildArchiveName("c:\\temp\\all", null);
        }

        [TestMethod]
        public void BuildArchiveName_AbsolutePathWithExtension()
        {
            TestBuildArchiveName("c:\\temp\\all", "log");
        }

        [TestMethod]
        public void BuildArchiveName_RelativePathWithoutExtension()
        {
            TestBuildArchiveName(".\\logs\\all", null);
        }

        [TestMethod]
        public void BuildArchiveName_RelativePathWithExtension()
        {
            TestBuildArchiveName(".\\logs\\all", "log");
        }

        private void TestBuildArchiveName(string name, string extension)
        {
            var fullName = name;
            if (!string.IsNullOrWhiteSpace(extension))
            {
                fullName += ".";
                fullName += extension;
            }

            var actual = sut.BuildArchiveName(fullName);

            var regex = MakeRegex(name, extension);
            regex.IsMatch(actual).Should().BeTrue(actual + " should be a valid file path");
        }

        private Regex MakeRegex(string name, string extension)
        {
            var fileName = Path.GetFileNameWithoutExtension(name);
            var expression = $"{fileName}_\\d{{4}}_\\d{{2}}_\\d{{2}}T\\d{{2}}_\\d{{2}}_\\d{{2}}\\.\\d{{7}}Z";
            
            if (!string.IsNullOrWhiteSpace(extension))
            {
                expression += $"\\.{extension}";
            }

            expression += "$";

            return new Regex(expression);
        }
    }
}
