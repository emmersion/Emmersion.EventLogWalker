using System.IO;

namespace Emmersion.EventLogWalker.Consumer
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
            File.WriteAllText(fileName, data);
        }

        public void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }
    }
}
