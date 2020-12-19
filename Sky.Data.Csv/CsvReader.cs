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
        private const String INVALID_DATA = "ERROR. LINE: {0}, POSITION: {1}, ROW NO.: {2}.";
        private const String INVALID_DATA_FILE = "ERROR. FILE: {0}, LINE: {1}, POSITION: {2}, ROW NO.: {3}.";

        private readonly Char[] mBuffer;
        private Int32 mBufferPosition = 0, mBufferCharCount = 0;
        private readonly StreamReader mReader;
        private readonly StringBuilder mCsvTextBuilder = new StringBuilder(256);
        private readonly CsvReaderSettings mCsvSettings;
        private readonly String mFilePath;

        private readonly IDataResolver<T> mDataResolver;
        private readonly Dictionary<String, List<String>> mCachedRows = new Dictionary<String, List<String>>(1024);
        private Boolean mFileHeaderAlreadySkipped = false;

        private void ThrowException(String rowText, Int32 rowIndex, Int32 chPos)
        {
            var errorMsg = !String.IsNullOrEmpty(this.mFilePath)
                    ? String.Format(INVALID_DATA_FILE, this.mFilePath, rowText, chPos, rowIndex)
                    : String.Format(INVALID_DATA, rowText, chPos, rowIndex);
            throw new InvalidDataException(errorMsg);
        }
        private Boolean EnsureBuffer()
        {
            if (this.mBufferPosition < this.mBufferCharCount)
                return true;

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
            var sepChar = this.mCsvSettings.Seperator;
            for (Int32 charPos = 0; charPos < textLen; ++charPos)
            {
                mCsvTextBuilder.Length = 0;
                var firstChar = oneRowText[charPos];

                #region Non-Quoted CSV Cell Value Processor
                if (firstChar != '\"')
                {
                    for (var c = firstChar; c != sepChar; c = oneRowText[charPos])
                    {
                        if (c == '\"' && !this.mCsvSettings.IgnoreErrors)
                            ThrowException(oneRowText, this.LineIndex, charPos);

                        mCsvTextBuilder.Append(c);
                        if (++charPos >= textLen) break;
                    }
                    recordInfo.Add(mCsvTextBuilder.ToString());
                }
                #endregion
                #region Quoted CSV Cell Value Processor
                else //This is a quoted cell value
                {
                    if (textLen < charPos + 1 && !this.mCsvSettings.IgnoreErrors)
                        ThrowException(oneRowText, this.LineIndex, charPos);

                    for (++charPos; charPos < textLen; ++charPos)
                    {
                        Char theChar = oneRowText[charPos], nextChar;

                        if (theChar != '\"')
                            mCsvTextBuilder.Append(theChar);
                        else if ((textLen <= charPos + 1) || (nextChar = oneRowText[charPos + 1]) == sepChar)
                        {
                            ++charPos;
                            recordInfo.Add(mCsvTextBuilder.ToString());
                            break;
                        }
                        else if (nextChar == '\"')
                            mCsvTextBuilder.Append(oneRowText[charPos = charPos + 1]);
                        else if (!this.mCsvSettings.IgnoreErrors)
                            ThrowException(oneRowText, this.LineIndex, charPos);
                        else
                            mCsvTextBuilder.Append(theChar);
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
        protected static void CheckFilePath(String filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentException("filePath is invalid", "filePath");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File does not exist", filePath);
        }
        protected CsvReader(Stream stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            this.mDataResolver = dataResolver;
            this.mCsvSettings = settings = settings ?? new CsvReaderSettings();
            this.mCsvSettings.UseCache = settings.UseCache || settings.SkipDuplicates;
            EnsureParameters(stream, settings, dataResolver);
            settings.BufferSize = Math.Min(4096 * 1024, Math.Max(settings.BufferSize, 4096));
            this.mReader = new StreamReader(stream, settings.Encoding, false, settings.BufferSize);
            this.mBuffer = new Char[settings.BufferSize];
        }
        protected CsvReader(String filePath, CsvReaderSettings settings, IDataResolver<T> dataResolver)
            : this(File.OpenRead(filePath), settings, dataResolver)
        {
            this.mFilePath = filePath;
        }

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
            var commentHint = this.mCsvSettings.CommentHint;
            while (true)
            {
                if (this.mBufferPosition >= this.mBufferCharCount)
                    if (!this.EnsureBuffer()) return null;

                #region Read one real CSV record line
                var quoted = false;
                mCsvTextBuilder.Length = 0;
                while (this.mBufferPosition < this.mBufferCharCount)
                {
                    var firstChar = this.mBuffer[this.mBufferPosition++];

                    if (firstChar == '\r')
                    {
                        if (this.mBufferPosition >= this.mBufferCharCount)
                            if (!this.EnsureBuffer()) break;

                        if (this.mBuffer[this.mBufferPosition] == '\n')
                            ++this.mBufferPosition;

                        //for macintosh csv format, it uses \r as line break
                        break;
                    }
                    //else mCsvTextBuilder.Append(firstChar);
                    else if (!quoted && firstChar == '\n') break;
                    else
                    {
                        mCsvTextBuilder.Append(firstChar);
                        if (firstChar == '\"')
                        {
                            if (!quoted) quoted = true;
                            else
                            {
                                if (this.mBufferPosition >= this.mBufferCharCount)
                                    if (!this.EnsureBuffer()) break;

                                if (this.mBuffer[this.mBufferPosition] == '\"')
                                    mCsvTextBuilder.Append(this.mBuffer[this.mBufferPosition++]);
                                else
                                    quoted = false;
                            }
                        }
                    }

                    //if there is no line break, we should read to the end of file.
                    if (this.mBufferPosition >= this.mBufferCharCount)
                        if (!this.EnsureBuffer()) break;
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
        /// <summary>
        /// Dispose the current CsvReader and close the opened file.
        /// </summary>
        public void Dispose() { this.mReader.Close(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
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