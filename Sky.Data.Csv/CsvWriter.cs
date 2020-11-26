//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sky.Data.Csv
{
    public class CsvWriter<T> : IDisposable
    {
        private readonly StreamWriter mWriter;
        private readonly CsvWriterSettings mCsvSettings;
        private readonly Char[] needQuoteChars;

        private readonly IDataResolver<T> dataResolver;

        private static void EnsureParameters(Stream stream, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new ArgumentException("stream is not writable", "stream");

            if (settings.Encoding == null)
                throw new ArgumentNullException("settings.Encoding");

            if (dataResolver == null)
                throw new ArgumentNullException("dataResolver");
        }
        protected static void CheckFilePath(String filePath, CsvWriterSettings settings)
        {
            settings = settings ?? new CsvWriterSettings();

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentException("Parameter is not valid", "filePath");
            var parentFolder = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(parentFolder))
                throw new FileNotFoundException("Cannot find specified folder", filePath);

            if (File.Exists(filePath) && !settings.OverwriteExisting)
                throw new Exception(String.Format("The file {0} exists", filePath));
        }
        protected CsvWriter(Stream stream, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            this.dataResolver = dataResolver;
            this.mCsvSettings = settings = settings ?? new CsvWriterSettings();
            EnsureParameters(stream, settings, dataResolver);
            settings.BufferSize = Math.Min(4096 * 1024, Math.Max(settings.BufferSize, 4096));
            needQuoteChars = new Char[] { '\n', '\"', settings.Seperator };
            this.mWriter = new StreamWriter(stream, settings.Encoding, settings.BufferSize);
            if (settings.OverwriteExisting && stream is FileStream)
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
            }
        }

        #region Public Static Methods for creating instance
        public static CsvWriter<T> Create(String filePath, IDataResolver<T> dataResolver)
        {
            return Create(filePath, new CsvWriterSettings(), dataResolver);
        }
        public static CsvWriter<T> Create(String filePath, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            CheckFilePath(filePath, settings);
            return Create(File.OpenWrite(filePath), settings, dataResolver);
        }
        public static CsvWriter<T> Create(Stream stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvWriterSettings(), dataResolver);
        }
        public static CsvWriter<T> Create(Stream stream, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            return new CsvWriter<T>(stream, settings, dataResolver);
        }
        #endregion

        public Int32 RowIndex { get; private set; }

        public CsvWriter<T> WriteRows(params T[] data)
        {
            return this.WriteRows(new List<T>(data));
        }
        public CsvWriter<T> WriteRows(IEnumerable<T> data)
        {
            foreach (var obj in data) this.WriteRow(obj);
            return this;
        }
        public CsvWriter<T> WriteRow(T data)
        {
            return this.WriteRow(this.dataResolver.Serialize(data));
        }
        public CsvWriter<T> WriteRow(IEnumerable<String> data)
        {
            ++RowIndex;

            var rowLine = new StringBuilder(64);
            var seperator = this.mCsvSettings.Seperator;

            foreach (var originalCellValueString in data)
            {
                var valueString = originalCellValueString ?? String.Empty;

                if (Array.Exists(needQuoteChars, c => valueString.IndexOf(c) >= 0))
                {
                    valueString = String.Format("\"{0}\"",
                        valueString.Replace("\"", "\"\"").Replace("\r\n", "\r"));
                }

                rowLine.Append(valueString).Append(seperator);
            }

            if (rowLine.Length > 0)
                rowLine.Remove(rowLine.Length - 1, 1);

            this.mWriter.WriteLine(rowLine);

            return this;
        }
        public CsvWriter<T> WriteRow(params String[] data)
        {
            return this.WriteRow(new List<String>(data));
        }

        public void Dispose() { this.mWriter.Close(); }
    }

    public class CsvWriter : CsvWriter<List<String>>
    {
        private CsvWriter(Stream stream, CsvWriterSettings settings)
            : base(stream, settings, new RawDataResolver())
        {
        }

        #region Public Static Methods for creating instance
        public static CsvWriter Create(String filePath)
        {
            return Create(filePath, new CsvWriterSettings());
        }
        public static CsvWriter Create(String filePath, CsvWriterSettings settings)
        {
            CheckFilePath(filePath, settings);
            return Create(File.OpenWrite(filePath), settings);
        }
        public static CsvWriter Create(Stream stream)
        {
            return Create(stream, new CsvWriterSettings());
        }
        public static CsvWriter Create(Stream stream, CsvWriterSettings settings)
        {
            return new CsvWriter(stream, settings);
        }
        #endregion
    }
}