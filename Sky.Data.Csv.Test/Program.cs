//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Sky.Data.Csv.Test
{
    public class Student
    {
        public String Name { get; set; }
        public String Address { get; set; }

        public Int32 Height { get; set; }
        public DateTime Birthday { get; set; }
    }

    public class StudentResolver : AbstractDataResolver<Student>
    {
        public override Student Deserialize(List<String> data)
        {
            var culture = CultureInfo.InvariantCulture;
            return new Student
            {
                Name = data[0],
                Height = Int32.Parse(data[1]),
                Birthday = DateTime.ParseExact(data[2], "yyyy-MM-dd", culture),
                Address = data[3],
            };
        }

        public override List<String> Serialize(Student data)
        {
            return new List<String>
            {
                data.Name,
                data.Height.ToString(),
                data.Birthday.ToString("yyyy-MM-dd"),
                data.Address,
            };
        }
    }

    class Program
    {
        private static readonly double MB = 1024 * 1024.0;

        private static String ProcessCsvFile(String filePath)
        {
#if NETCORE
            return filePath;
#else
            return String.Format("../{0}", filePath);
#endif
        }
        static void TestGenericReader()
        {
            var csvFiles = new String[] {
                @"../TestData.Csv/csv-students.csv",
            };
            var csvReaderSettings = new CsvReaderSettings();
            csvReaderSettings.Encoding = System.Text.Encoding.UTF8;
            var dataResolver = new StudentResolver();
            foreach (var csvFile in csvFiles)
            {
                var recordCount = 0;
                var csvFilePath = ProcessCsvFile(csvFile);

                var startTime = DateTime.Now;
                using (var reader = CsvReader<Student>.Create(csvFilePath, csvReaderSettings, dataResolver))
                {
                    foreach (var student in reader)
                    {
                        ++recordCount;
                    }
                }
                var ellapsed = (DateTime.Now - startTime).TotalSeconds;

                Console.WriteLine("Count: {0}\tTime: {1}ms", recordCount, ellapsed);
                Console.WriteLine("======================");
            }
        }
        static void TestSpecificReader()
        {
            var csvFiles = new String[] {
                //@"../TestData.Csv/csv-bigdata.csv",
                @"../TestData.Csv/csv-comma-delimited.csv",
                //@"../TestData.Csv/csv-lumentest2.csv",
                //@"../TestData.Csv/csv-lumentest3.csv",
                @"../TestData.Csv/csv-macintosh.csv",
                @"../TestData.Csv/csv-ms-dos.csv",
                @"../TestData.Csv/csv-ms-dos-complex.csv",
                //@"../TestData.Csv/csv-students.csv",
                //@"../TestData.Csv/csv-longrowdata.csv",
            };

            var csvReaderSettings = new CsvReaderSettings();
            csvReaderSettings.IgnoreErrors = true;
            csvReaderSettings.Encoding = System.Text.Encoding.UTF8;
            csvReaderSettings.SkipEmptyLines = true;

            foreach (var csvFile in csvFiles)
            {
                var csvFilePath = ProcessCsvFile(csvFile);

                Int32 recordCount = 0, cellCount = 0;
                var fileSize = new FileInfo(csvFilePath).Length;

                var startTime = DateTime.Now;
                using (var reader = CsvReader.Create(csvFilePath, csvReaderSettings))
                {
                    foreach (var data in reader)
                    {
                        ++recordCount;
                        cellCount += data.Count;
                    }
                }
                var ellapsed = (DateTime.Now - startTime).TotalSeconds;

                ellapsed = ellapsed == 0 ? 0.000003 : ellapsed;
                var speed = (fileSize / MB / ellapsed).ToString("0.00");
                Console.WriteLine("RC: {0,-9}CC: {1,-10}T(s): {2,-11}S:{3}M/s",
                    recordCount, cellCount, ellapsed, speed);
                Console.WriteLine("======================");
            }
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            TestSpecificReader();
            //TestGenericReader();
        }
    }
}