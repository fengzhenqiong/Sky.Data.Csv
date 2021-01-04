//@Author: Sky Feng(im.sky@foxmail.com).
using System;
using System.Collections.Generic;

namespace Sky.Data.Csv
{
    /// <summary>
    /// A data serializer and deserializer contract.
    /// You can create you own data resolver to make your data type operatable with generic version of CsvReader and CsvWriter.
    /// </summary>
    /// <typeparam name="TData">The generic type of which objects will be serialized and deserialized.</typeparam>
    public interface IDataResolver<TData>
    {
        /// <summary>
        /// Serialize the specified object to a list of String values.
        /// </summary>
        /// <param name="data">The data object to be serialized.</param>
        /// <returns>A list of String values.</returns>
        List<String> Serialize(TData data);
        /// <summary>
        /// Deserialize the specified list of String values to an object.
        /// </summary>
        /// <param name="data">The list of String values.</param>
        /// <returns>The deserialized object.</returns>
        TData Deserialize(params String[] data);
        /// <summary>
        /// Deserialize the specified list of String values to an object.
        /// </summary>
        /// <param name="data">The list of String values.</param>
        /// <returns>The deserialized object.</returns>
        TData Deserialize(IEnumerable<String> data);
        /// <summary>
        /// Deserialize the specified list of String values to an object.
        /// </summary>
        /// <param name="data">The list of String values.</param>
        /// <returns>The deserialized object.</returns>
        TData Deserialize(List<String> data);
    }

    /// <summary>
    /// Provide a basic implementation of IDataResolver.
    /// Usually you can create a data resolver by subclassing this class instead of doing a full implementation of IDataResolver.
    /// </summary>
    /// <typeparam name="TData">The generic type of which objects will be serialized and deserialized.</typeparam>
    public abstract class AbstractDataResolver<TData> : IDataResolver<TData>
    {
        /// <summary>
        /// Convert CSV raw data values to a typed object
        /// </summary>
        /// <param name="data">CSV raw data values</param>
        /// <returns>A typed object</returns>
        public TData Deserialize(IEnumerable<String> data)
        {
            return Deserialize((List<String>)data ?? new List<String>(data));
        }

        /// <summary>
        /// Convert CSV raw data values to a typed object
        /// </summary>
        /// <param name="data">CSV raw data values</param>
        /// <returns>A typed object</returns>
        public TData Deserialize(params String[] data)
        {
            return Deserialize(new List<String>(data));
        }

        /// <summary>
        /// Convert CSV raw data values to a typed object
        /// </summary>
        /// <param name="data">CSV raw data values</param>
        /// <returns>A typed object</returns>
        public abstract TData Deserialize(List<String> data);

        /// <summary>
        /// Convert a typed object to CSV raw data values
        /// </summary>
        /// <param name="data">A typed object</param>
        /// <returns>CSV raw data values</returns>
        public abstract List<String> Serialize(TData data);
    }

    /// <summary>
    /// A builtin data resolver which do nothing for lists of Strings and just return the original value.
    /// </summary>
    public class RawDataResolver : AbstractDataResolver<List<String>>
    {
        /// <summary>
        /// Convert CSV raw data values to a typed object
        /// </summary>
        /// <param name="data">CSV raw data values</param>
        /// <returns>A typed object</returns>
        public override List<String> Deserialize(List<String> data)
        {
            return data;
        }

        /// <summary>
        /// Convert a typed object to CSV raw data values
        /// </summary>
        /// <param name="data">A typed object</param>
        /// <returns>CSV raw data values</returns>
        public override List<String> Serialize(List<String> data)
        {
            return data;
        }
    }
}
