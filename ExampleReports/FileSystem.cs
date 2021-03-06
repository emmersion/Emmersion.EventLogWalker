using System.IO;

namespace ExampleReports
{
    public interface IFileSystem
    {
        string ReadFile(string fileName);
        void WriteFile(string fileName, string data);
        void DeleteFile(string fileName);
    }

    public class FileSystem : IFileSystem
    {
        public string ReadFile(string fileName)
        {
            try
            {
                return File.ReadAllText(fileName);
            }
            catch
            {
                return null;
            }
        }

        public void WriteFile(string fileName, string data)
        {
            var dir = Path.GetDirectoryName(fileName);
            Directory.CreateDirectory(dir);

            File.WriteAllText(fileName, data);
        }

        public void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }
    }
}
