//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sky.Data.Csv
{
    public class CsvReader<T> : IEnumerable<T>, IDisposable
    {
        private const String INVALID_DATA = "ERROR. LINE: {0}, POSITION: {1}, ROW NO.: {2}.";
        private const String INVALID_DATA_FILE = "ERROR. FILE: {0}, LINE: {1}, POSITION: {2}, ROW NO.: {3}.";

        private readonly Char[] mBuffer;
        private Int32 mBufferPosition = 0, mBufferCharCount = 0;
        private readonly StreamReader mReader;
        private readonly StringBuilder mCellValueBuilder = new StringBuilder(128);
        private readonly CsvReaderSettings mCsvSettings;
        private readonly String mFilePath;

        private readonly IDataResolver<T> dataResolver;
        private readonly Dictionary<String, List<String>> cachedRows = new Dictionary<String, List<String>>();

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
            ++this.RowIndex;

            var textLen = oneRowText.Length;
            var sepChar = this.mCsvSettings.Seperator;

            if (textLen == 0) return new List<String>();
            if (this.mCsvSettings.UseCache && cachedRows.ContainsKey(oneRowText))
                return cachedRows[oneRowText];

            var result = new List<String>(16);
            for (Int32 charPos = 0; charPos < textLen; ++charPos)
            {
                mCellValueBuilder.Length = 0;
                var firstChar = oneRowText[charPos];

                #region Non-Quoted CSV Cell Value Processor
                if (firstChar != '\"')
                {
                    for (var c = firstChar; c != sepChar; c = oneRowText[charPos])
                    {
                        if (c == '\"' && !this.mCsvSettings.IgnoreErrors)
                            ThrowException(oneRowText, this.RowIndex, charPos);

                        mCellValueBuilder.Append(c);
                        if (++charPos >= textLen) break;
                    }
                    result.Add(mCellValueBuilder.ToString());
                }
                #endregion
                #region Quoted CSV Cell Value Processor
                else //This is a quoted cell value
                {
                    if (textLen < charPos + 1 && !this.mCsvSettings.IgnoreErrors)
                        ThrowException(oneRowText, this.RowIndex, charPos);

                    for (++charPos; charPos < textLen; ++charPos)
                    {
                        Char theChar = oneRowText[charPos], nextChar;

                        if (theChar != '\"')
                            mCellValueBuilder.Append(theChar);
                        else if ((textLen <= charPos + 1) || (nextChar = oneRowText[charPos + 1]) == sepChar)
                        {
                            ++charPos;
                            result.Add(mCellValueBuilder.ToString());
                            break;
                        }
                        else if (nextChar == '\"')
                            mCellValueBuilder.Append(oneRowText[charPos = charPos + 1]);
                        //Code should not hit this point, it indicates an error
                        else if (!this.mCsvSettings.IgnoreErrors)
                            ThrowException(oneRowText, this.RowIndex, charPos);
                    }
                }
                #endregion
            }
            if (oneRowText[textLen - 1] == sepChar)
                result.Add(String.Empty);

            if (this.mCsvSettings.UseCache && !cachedRows.ContainsKey(oneRowText))
                cachedRows[oneRowText] = result;

            return result;
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
            this.dataResolver = dataResolver;
            this.mCsvSettings = settings = settings ?? new CsvReaderSettings();
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


        public Int32 RowIndex { get; private set; }
        public List<String> ReadRow()
        {
            if (!this.EnsureBuffer()) return null;

            var oneRowText = new StringBuilder();
            while (this.mBufferPosition < this.mBufferCharCount)
            {
                var firstChar = this.mBuffer[this.mBufferPosition++];

                if (firstChar == '\r')
                {
                    if (!this.EnsureBuffer())
                        return ParseOneRow(oneRowText.ToString());

                    var secondChar = this.mBuffer[this.mBufferPosition++];

                    //for macintosh csv format, it uses \r as line break
                    if (secondChar != '\n') --this.mBufferPosition;

                    return ParseOneRow(oneRowText.ToString());
                }
                else oneRowText.Append(firstChar);

                this.EnsureBuffer();
            }

            if (this.mReader.EndOfStream && oneRowText.Length == 0)
                return null;

            return ParseOneRow(oneRowText.ToString());
        }

        #region Public Static Methods for creating instance
        public static CsvReader<T> Create(String filePath, IDataResolver<T> dataResolver)
        {
            return Create(filePath, new CsvReaderSettings(), dataResolver);
        }
        public static CsvReader<T> Create(String filePath, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            CheckFilePath(filePath);
            return new CsvReader<T>(filePath, settings, dataResolver);
        }
        public static CsvReader<T> Create(Byte[] stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvReaderSettings(), dataResolver);
        }
        public static CsvReader<T> Create(Byte[] stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            return Create(new MemoryStream(stream), settings, dataResolver);
        }
        public static CsvReader<T> Create(Stream stream, IDataResolver<T> dataResolver)
        {
            return Create(stream, new CsvReaderSettings(), dataResolver);
        }
        public static CsvReader<T> Create(Stream stream, CsvReaderSettings settings, IDataResolver<T> dataResolver)
        {
            return new CsvReader<T>(stream, settings, dataResolver);
        }
        #endregion

        #region Implementing IDisposable & IEnumerable
        public void Dispose() { this.mReader.Close(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public IEnumerator<T> GetEnumerator()
        {
            for (var row = this.ReadRow(); row != null; row = this.ReadRow())
            {
                yield return this.dataResolver.Deserialize(row);
            };
        }
        #endregion
    }

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
        public static CsvReader Create(String filePath)
        {
            return Create(filePath, new CsvReaderSettings());
        }
        public static CsvReader Create(String filePath, CsvReaderSettings settings)
        {
            CheckFilePath(filePath);
            return new CsvReader(filePath, settings);
        }
        public static CsvReader Create(Byte[] stream)
        {
            return Create(stream, new CsvReaderSettings());
        }
        public static CsvReader Create(Byte[] stream, CsvReaderSettings settings)
        {
            return Create(new MemoryStream(stream), settings);
        }
        public static CsvReader Create(Stream stream)
        {
            return Create(stream, new CsvReaderSettings());
        }
        public static CsvReader Create(Stream stream, CsvReaderSettings settings)
        {
            return new CsvReader(stream, settings);
        }
        #endregion
    }
}