using System;
using System.IO;

namespace Sky.Data.Csv.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var csvFiles = new[] {
                @"..\..\TestData.Csv\csv-ms-dos.csv",
                @"..\..\TestData.Csv\csv-macintosh.csv",
                @"..\..\TestData.Csv\csv-comma-delimited.csv",
                //@"..\..\TestData.Csv\csv-bigdata.csv",
                //@"..\..\TestData.Csv\longrowdata.csv",
            };

            foreach (var csvFile in csvFiles)
            {
                var startTime = DateTime.Now;

                using (var reader = CsvReader.Create(csvFile))
                {
                    var folder = Path.GetDirectoryName(csvFile);
                    var fileName = Path.GetFileNameWithoutExtension(csvFile);
                    var dumpFileName = String.Format("{0}-dumpped-sky.csv", fileName);
                    var dumpFile = Path.Combine(folder, dumpFileName);

                    using (var writer = CsvWriter.Create(dumpFile))
                    {
                        foreach (var row in reader)
                        {
                            writer.WriteRow(row);
                        }
                    }
                }

                var endTime = DateTime.Now;
                Console.WriteLine("Time: {0}ms", (endTime - startTime).TotalMilliseconds);
                Console.WriteLine("======================");
            }
        }
    }
}