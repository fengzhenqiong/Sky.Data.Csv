﻿//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sky.Data.Csv
{
    /// <summary>
    /// Generic version of CsvWriter with which you can write objects of any data type into a CSV file.
    /// A custom data resolver will be working as the data converter between CSV records and data objects.
    /// </summary>
    /// <typeparam name="T">The generic type of which objects will be written.</typeparam>
    public class CsvWriter<T> : IDisposable
    {
        #region Private constants to remove magic values
        private const Int32 BUFFER_SZMIN = 4 * 1024;
        private const Int32 BUFFER_SZMAX = 4 * 1024 * 1024;
        private const Int32 ROW_CAP = 1024;
        private const Int32 ROW_CAPMAX = 16 * 1024 * 1024;
        #endregion

        private readonly StreamWriter mWriter;
        private readonly CsvWriterSettings mCsvSettings;
        private readonly Char[] mNeedQuoteChars;

        private readonly IDataResolver<T> mDataResolver;

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

        /// <summary>
        /// Check whether the provided <paramref name="filePath"/> points to a valid file. If the specified file exists,
        /// while OverwriteExisting and AppendExisting are both false, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the specified file for writing</param>
        /// <param name="settings">The OverwriteExisting and AppendExisting properties will be used for checking the file specified</param>
        protected static void CheckFilePath(String filePath, CsvWriterSettings settings)
        {
            settings = settings ?? new CsvWriterSettings();

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentException("Parameter is not valid", "filePath");

            if (File.Exists(filePath) && !settings.OverwriteExisting && !settings.AppendExisting)
                throw new Exception(String.Format("The specified file {0} already exists", filePath));

            if (settings.OverwriteExisting && settings.AppendExisting)
                throw new ArgumentException("Overwrite and Append cannot both be true.");

            var parentFolder = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(parentFolder))
                throw new FileNotFoundException("Cannot find specified folder", filePath);
        }
        /// <summary>
        /// Initialize the current CsvWriter instance with provided information
        /// </summary>
        /// <param name="stream">A writable stream to which current writer will write data</param>
        /// <param name="settings">Configurable options customizing current CsvWriter instance</param>
        /// <param name="dataResolver">A customer data resolver converting objects to raw CSV values</param>
        protected CsvWriter(Stream stream, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            this.mDataResolver = dataResolver;
            this.mCsvSettings = settings = settings ?? new CsvWriterSettings();
            EnsureParameters(stream, settings, dataResolver);
            settings.BufferSize = Math.Min(BUFFER_SZMAX, Math.Max(settings.BufferSize, BUFFER_SZMIN));
            mNeedQuoteChars = new Char[] { '\r', '\n', '\"', settings.Seperator };
            this.mWriter = new StreamWriter(stream, settings.Encoding, settings.BufferSize);
            if (stream.CanSeek)
            {
                if (settings.AppendExisting) stream.Seek(0, SeekOrigin.End);
                if (settings.OverwriteExisting) stream.SetLength(0);
            }
        }

        #region Public methods for writing CSV data
        /// <summary>
        /// Indicating the index of the current written row, including any non-skipped lines.
        /// For what kind of lines will be skipped, refer to SkipEmptyLines of CsvWriterSettings.
        /// </summary>
        public Int32 RowIndex { get; private set; }
        /// <summary>
        /// Write a collection of objects to the current CSV file.
        /// The specified data will be serialized to list of strings with the specified data resolver.
        /// </summary>
        /// <param name="data">Collection of objects to be written.</param>
        /// <returns>The current CsvWriter instance.</returns>
        public CsvWriter<T> WriteRows(params T[] data)
        {
            return this.WriteRows(new List<T>(data));
        }
        /// <summary>
        /// Write a collection of objects to the current CSV file.
        /// The specified data will be serialized to list of strings with the specified data resolver.
        /// </summary>
        /// <param name="data">Collection of objects to be written.</param>
        /// <returns>The current CsvWriter instance.</returns>
        public CsvWriter<T> WriteRows(IEnumerable<T> data)
        {
            foreach (var obj in data) this.WriteRow(obj);
            return this;
        }
        /// <summary>
        /// Write a single object to the current CSV file.
        /// The specified data will be serialized to list of strings with the specified data resolver.
        /// </summary>
        /// <param name="data">The data object to be written.</param>
        /// <returns>The current CsvWriter instance.</returns>
        public CsvWriter<T> WriteRow(T data)
        {
            return this.WriteRow(this.mDataResolver.Serialize(data));
        }
        /// <summary>
        /// Write a list of String values as a CSV record to the current CSV file.
        /// </summary>
        /// <param name="data">A list of String values to be written.</param>
        /// <returns>The current CsvWriter instance.</returns>
        public CsvWriter<T> WriteRow(IEnumerable<String> data)
        {
            if (this.mDisposed)
                throw new ObjectDisposedException("writer", "The writer is disposed");

            var seperator = this.mCsvSettings.Seperator;
            var rowLine = new StringBuilder(ROW_CAP, ROW_CAPMAX);

            foreach (var originalCellValueString in data)
            {
                var vString = originalCellValueString ?? String.Empty;

                if (Array.Exists(mNeedQuoteChars, c => vString.IndexOf(c) >= 0))
                {
                    vString = String.Format("\"{0}\"", vString.Replace("\"", "\"\""));
                }

                rowLine.Append(vString).Append(seperator);
            }

            if (rowLine.Length > 0)
                rowLine.Remove(rowLine.Length - 1, 1);

            if (rowLine.Length > 0 || !this.mCsvSettings.SkipEmptyLines)
            {
                this.mWriter.WriteLine(rowLine); ++this.RowIndex;
            }

            return this;
        }
        /// <summary>
        /// Write a list of String values as a CSV record to the current CSV file.
        /// </summary>
        /// <param name="data">A collection of String values to be written.</param>
        /// <returns>The current CsvWriter instance.</returns>
        public CsvWriter<T> WriteRow(params String[] data)
        {
            return this.WriteRow(new List<String>(data));
        }
        #endregion

        #region Public Static Methods for creating instance
        /// <summary>
        /// Create an instance of CsvWriter with a specified file path and a custom data resolver.
        /// If the path already exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be written.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter<T> Create(String filePath, IDataResolver<T> dataResolver)
        {
            return Create(filePath, new CsvWriterSettings(), dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified file path, a CsvWriterSettings object and a custom data resolver.
        /// If the path already exists and AppendExisting and OverwriteExisting of settings are both false, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be written.</param>
        /// <param name="settings">Specify the options to control behavior of CsvWriter.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter<T> Create(String filePath, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            CheckFilePath(filePath, settings);
            return Create(File.OpenWrite(filePath), settings, dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified writable stream and a custom data resolver.
        /// </summary>
        /// <param name="stream">A writable strem to be written into.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter<T> Create(Stream stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvWriterSettings(), dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified writable stream, a CsvWriterSettings object and a custom data resolver.
        /// </summary>
        /// <param name="stream">A writable strem to be written into.</param>
        /// <param name="settings">Specify the options to control behavior of CsvWriter.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter<T> Create(Stream stream, CsvWriterSettings settings, IDataResolver<T> dataResolver)
        {
            return new CsvWriter<T>(stream, settings, dataResolver);
        }
        #endregion

        #region Implementing IDisposable & IEnumerable
        private Boolean mDisposed = false;
        /// <summary>
        /// Dispose the current CsvWriter instance.
        /// </summary>
        public void Close() { Dispose(); }
        /// <summary>
        /// Dispose the current CsvWriter instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose current CsvWriter instance, releasing resources
        /// </summary>
        /// <param name="disposing">Whether this method is called intentionally</param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (this.mDisposed) return;

            this.mWriter.Close();

            this.mDisposed = true;
        }
        /// <summary>
        /// Finalizer of current CsvWriter instance
        /// </summary>
        ~CsvWriter() { Dispose(false); }
        #endregion
    }

    /// <summary>
    /// CsvWriter with which you can write lists of String values into a CSV file.
    /// </summary>
    public class CsvWriter : CsvWriter<List<String>>
    {
        private CsvWriter(Stream stream, CsvWriterSettings settings)
            : base(stream, settings, new RawDataResolver())
        {
        }

        #region Public Static Methods for creating instance
        /// <summary>
        /// Create an instance of CsvWriter with a specified file path. If the path already exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be written.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter Create(String filePath)
        {
            return Create(filePath, new CsvWriterSettings());
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified file path and settings.
        /// If the path already exists and AppendExisting and OverwriteExisting of settings are both false, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be written.</param>
        /// <param name="settings">Specify the options to control behavior of CsvWriter.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter Create(String filePath, CsvWriterSettings settings)
        {
            CheckFilePath(filePath, settings);
            return Create(File.OpenWrite(filePath), settings);
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified writable stream.
        /// </summary>
        /// <param name="stream">A writable strem to be written into.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter Create(Stream stream)
        {
            return Create(stream, new CsvWriterSettings());
        }
        /// <summary>
        /// Create an instance of CsvWriter with a specified writable stream and settings.
        /// </summary>
        /// <param name="stream">A writable strem to be written into.</param>
        /// <param name="settings">Specify the options to control behavior of CsvWriter.</param>
        /// <returns>A CsvWriter instance.</returns>
        public static CsvWriter Create(Stream stream, CsvWriterSettings settings)
        {
            return new CsvWriter(stream, settings);
        }
        #endregion
    }
}