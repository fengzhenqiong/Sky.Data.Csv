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
        static void TestGenericWriter()
        {
            var dataResolver = new StudentResolver();
            var csvFiles = new[] {
                @"..\..\TestData.Csv\csv-students.csv",
            };
            foreach (var csvFile in csvFiles)
            {
                var startTime = DateTime.Now;
                using (var reader = CsvReader<Student>.Create(csvFile, dataResolver))
                {
                    foreach (var data in reader)
                    {
                    }
                }
                var endTime = DateTime.Now;
                var ellapsed = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine("Time: {0}ms", ellapsed);
                Console.WriteLine("======================");
            }
        }
        static void TestSpecificWriter()
        {
            var csvFiles = new[] {
                @"..\..\TestData.Csv\csv-ms-dos.csv",
                @"..\..\TestData.Csv\csv-macintosh.csv",
                @"..\..\TestData.Csv\csv-comma-delimited.csv",
                //@"..\..\TestData.Csv\csv-bigdata.csv",
                //@"..\..\TestData.Csv\csv-students.csv",
                //@"..\..\TestData.Csv\longrowdata.csv",
            };

            foreach (var csvFile in csvFiles)
            {
                var folder = Path.GetDirectoryName(csvFile);
                var fileName = Path.GetFileNameWithoutExtension(csvFile);
                var dumpFileName = String.Format("{0}-dumpped-sky.csv", fileName);
                var dumpFile = Path.Combine(folder, dumpFileName);

                var startTime = DateTime.Now;
                using (var reader = CsvReader.Create(csvFile))
                {
                    foreach (var data in reader)
                    {
                    }
                }
                var endTime = DateTime.Now;
                var ellapsed = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine("Time: {0}ms", ellapsed);
                Console.WriteLine("======================");
            }
        }

        static void Main(string[] args)
        {
            TestSpecificWriter();
            TestGenericWriter();
        }
    }
}