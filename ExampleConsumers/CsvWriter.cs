using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExampleConsumers
{
    public interface ICsvWriter
    {
        void WriteAll<T>(string fileName, string headerRow, List<T> records);
    }

    public class CsvWriter : ICsvWriter
    {
        public void WriteAll<T>(string fileName, string headerRow, List<T> records)
        {
            File.WriteAllLines(fileName, new[] {headerRow});
            File.AppendAllLines(fileName, records.Select(x => x.ToString()));
        }
    }
}
