using System;
using System.Collections.Generic;

namespace Sky.Data.Csv
{
    public interface IDataResolver<TData>
    {
        List<String> Serialize(TData data);
        TData Deserialize(params String[] data);
        TData Deserialize(List<String> data);
    }

    public abstract class AbstractDataResolver<TData> : IDataResolver<TData>
    {
        public TData Deserialize(params String[] data)
        {
            return Deserialize(new List<String>(data));
        }

        public abstract TData Deserialize(List<String> data);

        public abstract List<String> Serialize(TData data);
    }

    internal class RawDataResolver : AbstractDataResolver<List<String>>
    {
        public override List<String> Deserialize(List<String> data)
        {
            return data;
        }

        public override List<String> Serialize(List<String> data)
        {
            return data;
        }
    }
}
