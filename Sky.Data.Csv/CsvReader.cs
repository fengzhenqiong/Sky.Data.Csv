//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sky.Data.Csv
{
    /// <summary>
    /// Generic version of CsvReader with which you can directly read typed objects from a CSV file.
    /// A custom data resolver will be working as the data converter between CSV records and data objects.
    /// </summary>
    /// <typeparam name="T">The generic type of which objects will be read.</typeparam>
    public class CsvReader<T> : IEnumerable<T>, IDisposable
    {
        #region Private constants to remove magic values
        private const Int32 BUFFER_SZMIN = 4 * 1024;
        private const Int32 BUFFER_SZMAX = 4 * 1024 * 1024;
        private const Int32 ROW_CAP = 1024;
        private const Int32 ROW_CAPMAX = 16 * 1024 * 1024;
        private const Int32 CACHE_SZMIN = 1024;

        private const String INVALID_DATA = "ERROR. ROW NO.: {0}, POSITION: {1}, LINE: {2}.";
        private const String INVALID_DATA_FILE = "ERROR. FILE: {0}, ROW NO.: {1}, POSITION: {2}, LINE: {3}.";
        #endregion

        private readonly Char[] mBuffer;
        private Int32 mBufferPosition, mBufferCharCount;
        private readonly StreamReader mReader;
        private readonly StringBuilder mCsvTextBuilder = new StringBuilder(ROW_CAP, ROW_CAPMAX);
        private readonly CsvReaderSettings mCsvSettings;
        private readonly String mFilePath;

        private readonly IDataResolver<T> mDataResolver;
        private readonly Dictionary<String, List<String>> mCachedRows = new Dictionary<String, List<String>>(CACHE_SZMIN);
        private Boolean mFileHeaderAlreadySkipped = false;

        private void ThrowException(String rowText, Int32 rowIndex, Int32 chPos)
        {
            var errorMsg = !String.IsNullOrEmpty(this.mFilePath)
                    ? String.Format(INVALID_DATA_FILE, this.mFilePath, rowIndex + 1, chPos, rowText)
                    : String.Format(INVALID_DATA, rowIndex, chPos, rowText);
            throw new InvalidDataException(errorMsg);
        }
        private Boolean EnsureBuffer()
        {
            if (this.mReader.EndOfStream)
                return false;

            this.mBufferCharCount = this.mReader.Read(this.mBuffer, 0, this.mBuffer.Length);
            this.mBufferPosition = 0;

            return this.mBufferCharCount > 0;
        }
        private List<String> ParseOneRow(String oneRowText)
        {
            ++this.RecordIndex;

            var textLen = oneRowText.Length;
            if (textLen == 0) return new List<String>();

            var recordInfo = new List<String>(16);
            var ignoreErrors = this.mCsvSettings.IgnoreErrors;
            var sepChar = this.mCsvSettings.Seperator;

            for (Int32 charPos = 0; charPos < textLen; ++charPos)
            {
                var startPos = charPos;
                var firstChar = oneRowText[charPos];

                #region Non-Quoted CSV Cell Value Processor
                if (firstChar != '\"')
                {
                    for (; firstChar != sepChar; firstChar = oneRowText[charPos])
                    {
                        if (firstChar == '\"' && !ignoreErrors)
                            ThrowException(oneRowText, this.LineIndex, charPos);

                        if (++charPos >= textLen) break;
                    }
                    var cellValue = oneRowText.Substring(startPos, charPos - startPos);
                    recordInfo.Add(cellValue);
                }
                #endregion
                #region Quoted CSV Cell Value Processor
                else if (firstChar == '\"')
                {
                    if (textLen <= charPos + 1 && !ignoreErrors)
                        ThrowException(oneRowText, this.LineIndex, charPos);

                    for (++charPos; charPos < textLen; ++charPos)
                    {
                        Char theChar = oneRowText[charPos], nextCh;
                        if (theChar != '\"') continue;

                        if ((textLen <= charPos + 1) || (nextCh = oneRowText[charPos + 1]) == sepChar)
                        {
                            ++charPos;
                            var charCount = charPos - startPos - 2;
                            var cellValue = oneRowText.Substring(startPos + 1, charCount);
                            recordInfo.Add(cellValue.Replace("\"\"", "\""));
                            break;
                        }
                        else if (!ignoreErrors && nextCh != '\"')
                            ThrowException(oneRowText, this.LineIndex, charPos + 1);
                        else if (nextCh == '\"') ++charPos;
                    }
                }
                #endregion
            }
            if (oneRowText[textLen - 1] == sepChar)
                recordInfo.Add(String.Empty);

            return recordInfo;
        }

        private static void EnsureParameters(Stream stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("stream is not readable", "stream");

            if (settings.Encoding == null)
                throw new ArgumentNullException("settings.Encoding");

            if (dataResolver == null)
                throw new ArgumentNullException("dataResolver");
        }

        /// <summary>
        /// Check whether the provided <paramref name="filePath"/> points to a valid file
        /// </summary>
        /// <param name="filePath">The path of the specified file containing CSV data</param>
        protected static void CheckFilePath(String filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentException("filePath is invalid", "filePath");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File does not exist", filePath);
        }
        /// <summary>
        /// Initialize the current CsvReader instance with provided information
        /// </summary>
        /// <param name="stream">A readable stream from which current reader will read data</param>
        /// <param name="settings">Configurable options customizing current CsvReader instance</param>
        /// <param name="dataResolver">A customer data resolver converting raw CSV values to objects</param>
        protected CsvReader(Stream stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            this.mDataResolver = dataResolver;
            this.mCsvSettings = settings = settings ?? new CsvReaderSettings();
            this.mCsvSettings.UseCache = settings.UseCache || settings.SkipDuplicates;
            EnsureParameters(stream, settings, dataResolver);
            settings.BufferSize = Math.Min(BUFFER_SZMAX, Math.Max(settings.BufferSize, BUFFER_SZMIN));
            this.mReader = new StreamReader(stream, settings.Encoding, false, settings.BufferSize);
            this.mBuffer = new Char[settings.BufferSize];
        }
        /// <summary>
        /// Initialize the current CsvReader instance with provided information
        /// </summary>
        /// <param name="filePath">A readable file from which current reader will read data</param>
        /// <param name="settings">Configurable options customizing current CsvReader instance</param>
        /// <param name="dataResolver">A customer data resolver converting raw CSV values to objects</param>
        protected CsvReader(String filePath, CsvReaderSettings settings, IDataResolver<T> dataResolver)
            : this(File.OpenRead(filePath), settings, dataResolver)
        {
            this.mFilePath = filePath;
        }

        #region Public methods for reading CSV data
        /// <summary>
        /// Indicates the index of the current read CSV row. Only non-skipped rows will be counted.
        /// </summary>
        public Int32 RowIndex { get; private set; }
        /// <summary>
        /// Indicates the index of the current read CSV record.
        /// Only non-skipped and non-header rows will be resolved to a valid record and counted.
        /// </summary>
        public Int32 RecordIndex { get; private set; }
        /// <summary>
        /// Indicates the index of the current read CSV line, any line in the CSV file will be counted.
        /// </summary>
        public Int32 LineIndex { get; private set; }
        /// <summary>
        /// The most basic method to read a CSV record as a list of String values.
        /// </summary>
        /// <returns>A list of String values read.</returns>
        public List<String> ReadRow()
        {
            if (this.mDisposed)
                throw new ObjectDisposedException("reader", "The reader is disposed");

            var commentHint = this.mCsvSettings.CommentHint;
            while (true)
            {
                if (this.mBufferPosition >= this.mBufferCharCount)
                    if (!this.EnsureBuffer()) return null;

                #region Read one real CSV record line
                var quoted = false;
                var startPosition = this.mBufferPosition;
                mCsvTextBuilder.Length = 0;
                while (this.mBufferPosition < this.mBufferCharCount)
                {
                    var firstChar = this.mBuffer[this.mBufferPosition++];

                    if (quoted && firstChar == '\"')
                    {
                        if (this.mBufferPosition >= this.mBufferCharCount)
                        {
                            var charCount = this.mBufferPosition - startPosition;
                            mCsvTextBuilder.Append(this.mBuffer, startPosition, charCount);

                            if (!this.EnsureBuffer()) break;
                            startPosition = this.mBufferPosition;
                        }

                        if (this.mBuffer[this.mBufferPosition] == '\"')
                            ++this.mBufferPosition;
                        else
                            quoted = false;
                    }
                    else if (!quoted && firstChar == '\"') quoted = true;
                    else if (!quoted && (firstChar == '\r' || firstChar == '\n'))
                    {
                        var charCount = this.mBufferPosition - 1 - startPosition;
                        mCsvTextBuilder.Append(this.mBuffer, startPosition, charCount);

                        if (firstChar == '\r')
                        {
                            if (this.mBufferPosition >= this.mBufferCharCount)
                                if (!this.EnsureBuffer()) break;

                            if (this.mBuffer[this.mBufferPosition] == '\n')
                                ++this.mBufferPosition;
                        }

                        //If not quoted and encountered \r or \n, it's a line break
                        break;
                    }

                    //if there is no line break, we should read to the end of file.
                    if (this.mBufferPosition >= this.mBufferCharCount)
                    {
                        var charCount = this.mBufferPosition - startPosition;
                        mCsvTextBuilder.Append(this.mBuffer, startPosition, charCount);

                        if (!this.EnsureBuffer()) break;
                        startPosition = this.mBufferPosition;
                    }
                }
                #endregion

                #region Processing header/empty lines/cache
                ++this.LineIndex;
                var oneRowText = mCsvTextBuilder.ToString();

                //the first non-skipped row will be treat as header or first record
                if (this.mCsvSettings.SkipEmptyLines && oneRowText.Length == 0)
                    continue;
                if (!String.IsNullOrEmpty(commentHint) && oneRowText.StartsWith(commentHint))
                    continue;

                ++this.RowIndex; //header is counted for row numbers
                if (!mFileHeaderAlreadySkipped && this.mCsvSettings.HasHeader)
                {
                    mFileHeaderAlreadySkipped = true;
                    continue;
                }

                //if a row is in cache, it's already read, process skip duplicates
                if (this.mCsvSettings.SkipDuplicates && mCachedRows.ContainsKey(oneRowText))
                    continue;

                //if use cache and the row is already read, use the existing value
                if (this.mCsvSettings.UseCache && mCachedRows.ContainsKey(oneRowText))
                    return mCachedRows[oneRowText];
                var temporaryData = this.ParseOneRow(oneRowText);
                //if use cache and the row is not read, add it to cache
                if (this.mCsvSettings.UseCache) mCachedRows[oneRowText] = temporaryData;
                #endregion

                return temporaryData;
            }
        }
        #endregion

        #region Public Static Methods for creating instance
        /// <summary>
        /// Create an instance of CsvReader with a specified file path and a custom data resolver.
        /// If the path does not exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be read.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(String filePath, IDataResolver<T> dataResolver)
        {
            return Create(filePath, new CsvReaderSettings(), dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified file path, a CsvReaderSettings object and a custom data resolver.
        /// If the path does not exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be read.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(String filePath, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            CheckFilePath(filePath);
            return new CsvReader<T>(filePath, settings, dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified byte array and a custom data resolver.
        /// </summary>
        /// <param name="stream">A Byte array from which the CsvReader will read content.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(Byte[] stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvReaderSettings(), dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified byte array, a CsvReaderSettings object and a custom data resolver.
        /// </summary>
        /// <param name="stream">A Byte array from which the CsvReader will read content.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(Byte[] stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            return Create(new MemoryStream(stream), settings, dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified readable stream and a custom data resolver.
        /// </summary>
        /// <param name="stream">A readable stream from which the CsvReader will read content.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(Stream stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvReaderSettings(), dataResolver);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified readable stream, a CsvReaderSettings object and a custom data resolver.
        /// </summary>
        /// <param name="stream">A readable stream from which the CsvReader will read content.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <param name="dataResolver">A custom data resolver used to serialize and deserialize data.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader<T> Create(Stream stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            return new CsvReader<T>(stream, settings, dataResolver);
        }
        #endregion

        #region Implementing IDisposable & IEnumerable
        private Boolean mDisposed = false;
        /// <summary>
        /// Dispose the current CsvReader and close the opened CSV file/stream.
        /// </summary>
        public void Close() { Dispose(); }
        /// <summary>
        /// Dispose the current CsvReader and close the opened CSV file/stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose current CsvReader instance, releasing resources
        /// </summary>
        /// <param name="disposing">Whether this method is called intentionally</param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (this.mDisposed) return;

            this.mReader.Close();

            this.mDisposed = true;
        }
        /// <summary>
        /// Finalizer of current CsvReader instance
        /// </summary>
        ~CsvReader() { Dispose(false); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        /// <summary>
        /// Get an enumerator which iterates CSV records from current CsvReader instance
        /// </summary>
        /// <returns>An enumerator which iterates CSV records</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var row = this.ReadRow(); row != null; row = this.ReadRow())
            {
                yield return this.mDataResolver.Deserialize(row);
            };
        }
        #endregion
    }

    /// <summary>
    /// CsvReader with which you can read lists of String values from a CSV file.
    /// </summary>
    public class CsvReader : CsvReader<List<String>>
    {
        private CsvReader(Stream stream, CsvReaderSettings settings)
            : base(stream, settings, new RawDataResolver())
        {
        }
        private CsvReader(String filePath, CsvReaderSettings settings)
            : base(filePath, settings, new RawDataResolver())
        {
        }

        #region Public Static Methods for creating instance
        /// <summary>
        /// Create an instance of CsvReader with a specified file path. If the path does not exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be read.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(String filePath)
        {
            return Create(filePath, new CsvReaderSettings());
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified file path, and a CsvReaderSetting object.
        /// If the path does not exists, an exception will be thrown.
        /// </summary>
        /// <param name="filePath">The path of the CSV file to be read.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(String filePath, CsvReaderSettings settings)
        {
            CheckFilePath(filePath);
            return new CsvReader(filePath, settings);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified byte array as data source.
        /// </summary>
        /// <param name="stream">A Byte array from which the CsvReader will read content.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(Byte[] stream)
        {
            return Create(stream, new CsvReaderSettings());
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified byte array as data source, and a CsvReaderSetting object.
        /// </summary>
        /// <param name="stream">A Byte array from which the CsvReader will read content.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(Byte[] stream, CsvReaderSettings settings)
        {
            return Create(new MemoryStream(stream), settings);
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified readable stream as data source.
        /// </summary>
        /// <param name="stream">A readable stream from which the CsvReader will read content.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(Stream stream)
        {
            return Create(stream, new CsvReaderSettings());
        }
        /// <summary>
        /// Create an instance of CsvReader with a specified readable stream as data source, and a CsvReaderSetting object.
        /// </summary>
        /// <param name="stream">A readable stream from which the CsvReader will read content.</param>
        /// <param name="settings">Specify the options to control behavior of CsvReader.</param>
        /// <returns>A CsvReader instance.</returns>
        public static CsvReader Create(Stream stream, CsvReaderSettings settings)
        {
            return new CsvReader(stream, settings);
        }
        #endregion
    }
}