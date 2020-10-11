using System.IO;
using System.Reflection;

namespace Sequel.Tests
{
    public static class TestContext
    {
        public static string ProjectFolder => Path.GetDirectoryName(typeof(TestContext).GetTypeInfo().Assembly.Location)!;
        public static string FilesFolder => Path.Combine(ProjectFolder, "Files");

        public static string ReadFile(string name) => File.ReadAllText(Path.Combine(FilesFolder, name));
    }
}
