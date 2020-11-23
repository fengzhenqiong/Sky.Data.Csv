//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sky.Data.Csv
{
    public class CsvWriter : IDisposable
    {
        private readonly StreamWriter mWriter;
        private readonly CsvWriterSettings mCsvSettings;
        private readonly Char[] needQuoteChars;

        private CsvWriter(FileStream stream, CsvWriterSettings settings)
        {
            this.mCsvSettings = settings;
            settings.BufferSize = Math.Min(1024 * 1024 * 4, Math.Max(settings.BufferSize, 1024 * 4));
            needQuoteChars = new Char[] { '\n', '\"', settings.Seperator };
            this.mWriter = new StreamWriter(stream, settings.Encoding, settings.BufferSize);
            if (settings.OverwriteExisting)
            {
                stream.SetLength(0); stream.Seek(0, SeekOrigin.Begin);
            }
        }

        public static CsvWriter Create(String filePath)
        {
            return Create(filePath, new CsvWriterSettings());
        }
        public static CsvWriter Create(String filePath, CsvWriterSettings settings)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentException("Parameter is not valid", "filePath");
            var parentFolder = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(parentFolder))
                throw new FileNotFoundException("Cannot find specified folder", filePath);
            if (File.Exists(filePath) && !settings.OverwriteExisting)
                throw new Exception(String.Format("The file {0} exists", filePath));

            return Create(File.OpenWrite(filePath), settings);
        }
        public static CsvWriter Create(FileStream stream)
        {
            return Create(stream, new CsvWriterSettings());
        }
        public static CsvWriter Create(FileStream stream, CsvWriterSettings settings)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new ArgumentException("stream is not readable", "stream");
            if (settings.Encoding == null)
                throw new ArgumentNullException("encoding");
            if (settings.BufferSize <= 0)
                throw new ArgumentException("Invalid buffer size", "stream");

            return new CsvWriter(stream, settings);
        }

        public Int32 RowIndex { get; private set; }
        public CsvWriter WriteRows(params List<String>[] rows)
        {
            return this.WriteRows(new List<List<String>>(rows));
        }
        public CsvWriter WriteRows(IEnumerable<List<String>> rows)
        {
            foreach (var currentRow in rows) this.WriteRow(currentRow);
            return this;
        }
        public CsvWriter WriteRow(params String[] row)
        {
            return this.WriteRow(new List<String>(row));
        }
        public CsvWriter WriteRow(IEnumerable<String> row)
        {
            ++RowIndex;

            row = row ?? new List<String>();
            var seperatorChar = this.mCsvSettings.Seperator;
            var rowLine = new StringBuilder(64);

            foreach (var originalCellValueString in row)
            {
                var valueString = originalCellValueString ?? String.Empty;

                if (Array.Exists(needQuoteChars, c => valueString.IndexOf(c) >= 0))
                {
                    valueString = String.Format("\"{0}\"",
                        valueString.Replace("\"", "\"\"").Replace("\r\n", "\r"));
                }

                rowLine.Append(valueString).Append(seperatorChar);
            }

            if (rowLine.Length > 0)
                rowLine.Remove(rowLine.Length - 1, 1);

            this.mWriter.WriteLine(rowLine);

            return this;
        }

        public void Dispose() { this.mWriter.Close(); }
    }
}