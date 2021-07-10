using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildTools
{
    class EntryPoint
    {
        static async Task Main(string[] args)
        {
            await Task.WhenAll(new List<Task>
            {
                MetaDataJsonFromXml.Build(),
                MetadataProtoFromXml.Build(),
                PhonePrefixData.Generate(),
                TimeZonesMapData.Generate()
            });
        }
    }

    internal class TimeZonesMapData
    {
        public static Task Generate()
        {
            throw new NotImplementedException();
        }
    }

    internal class MetadataProtoFromXml
    {
        public static Task Build()
        {
            throw new NotImplementedException();
        }
    }

    internal class MetaDataJsonFromXml
    {
        public static Task Build()
        {
            throw new NotImplementedException();
        }
    }
}