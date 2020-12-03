using System;
using System.Text;

namespace Sky.Data.Csv
{
    public abstract class CsvSettings
    {
        /// <summary>
        /// Specify the CSV field separator, default is comma (,)
        /// </summary>
        public Char Seperator { get; set; }
        /// <summary>
        /// Specify the Encoding for the CSV file, default is Encoding.Default
        /// </summary>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// Whether or not skip empty lines in a CSV file, default is false.
        /// If this option is set to true, and the data read or to be written is an empty list, or will be resolved to an empty list, it's ignored.
        /// If the CSV file has empty lines and you don't set this option, you need to handle them in your custom data resolver.
        /// </summary>
        public Boolean SkipEmptyLines { get; set; }
        /// <summary>
        /// Buffer size for reading or writing CSV files, default is 64K.
        /// Valid value should be between 4K and 4M. Any values smaller than 4K or larger than 4M will be automatically rounded.
        /// </summary>
        public Int32 BufferSize { get; set; }

        public CsvSettings()
        {
            this.Seperator = ',';
            this.SkipEmptyLines = false;
            this.Encoding = Encoding.Default;
            this.BufferSize = 64 * 1024;
        }
    }

    /// <summary>
    /// Specify some options to customize the CsvReader behaviors.
    /// </summary>
    public class CsvReaderSettings : CsvSettings
    {
        /// <summary>
        /// Whether or not to use cache when reading a CSV file, default is false.
        /// This value is useful when there are many duplicate records in the CSV file.
        /// </summary>
        public Boolean UseCache { get; set; }
        /// <summary>
        /// Whether to ignore errors when reading a CSV file, default is false.
        /// When CsvReader detects an error, if this option is set to true it will try to ignore the error and continue reading forward normally, otherwise it will throw an exception.
        /// </summary>
        public Boolean IgnoreErrors { get; set; }
        /// <summary>
        /// Specify the comment indicator for a line, default is null.
        /// If this option is not empty or null, any lines starts with the specified value will be ignored.
        /// </summary>
        public String CommentHint { get; set; }
        /// <summary>
        /// Whether the CSV file has a header, default is false.
        /// If true, the first not skipped line will be regarded to be the header of the CSV file and ignored.
        /// Refer to CommentHint and SkipEmptyLines for more details.
        /// </summary>
        public Boolean HasHeader { get; set; }
        public CsvReaderSettings()
        {
            this.UseCache = false;
            this.IgnoreErrors = false;
            this.CommentHint = null;
            this.HasHeader = false;
        }
    }

    /// <summary>
    /// Specify some options to customize the CsvWriter behaviors.
    /// </summary>
    public class CsvWriterSettings : CsvSettings
    {
        /// <summary>
        /// Whether or not append to the existing CSV file.
        /// AppendExisting and OverwriteExisting cannot be both true at the same time.
        /// If both values are false, and the specified file exists, an exception will be thrown.
        /// </summary>
        public Boolean AppendExisting { get; set; }
        /// <summary>
        /// Whether or not to overwrite the existing CSV file.
        /// AppendExisting and OverwriteExisting cannot be both true at the same time.
        /// If both values are false, and the specified file exists, an exception will be thrown.
        /// </summary>
        public Boolean OverwriteExisting { get; set; }
        public CsvWriterSettings()
        {
            this.AppendExisting = false;
            this.OverwriteExisting = false;
        }
    }
}