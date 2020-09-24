using System;
using System.Text;

namespace Sky.Data.Csv
{
    public abstract class CsvSettings
    {
        public Int32 BufferSize { get; set; }
        public Encoding Encoding { get; set; }

        public CsvSettings()
        {
            this.BufferSize = 64 * 1024;
            this.Encoding = Encoding.UTF8;
        }
    }

    public class CsvReaderSettings : CsvSettings
    {
        public Boolean UseCache { get; set; }
        public Boolean IgnoreErrors { get; set; }
        public CsvReaderSettings()
        {
            this.UseCache = false;
            this.IgnoreErrors = false;
        }
    }

    public class CsvWriterSettings : CsvSettings
    {
        public Boolean OverwriteExisting { get; set; }
        public CsvWriterSettings()
        {
            this.OverwriteExisting = true;
        }
    }
}