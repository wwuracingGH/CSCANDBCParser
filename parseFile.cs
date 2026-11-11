using System.Text;

class FileData {
    abstract class DataStream {
        public List<ulong> timestamp;
        protected DataStream() {
            timestamp = new List<ulong>();
        }
    }

    class FloatStream: DataStream {
        public List<float> data;
        public FloatStream() : base()
        {
            data = new List<float>();
        }
    }

    class DoubleStream: DataStream {
        public List<double> data;
        public DoubleStream() : base()
        {
            data = new List<double>();
        }
    }

    class SignedStream: DataStream {
        public List<long> data;
        public SignedStream() : base()
        {
            data = new List<long>();
        }
    }

    class UnsignedStream: DataStream {
        public List<ulong> data;
        public UnsignedStream() : base()
        {
            data = new List<ulong>();
        }
    }

    private static DataStream GetStreamOfType(DBCObj.ChannelType type) {
        switch(type)
        {
            case DBCObj.ChannelType.SignedInteger: return new SignedStream();
            case DBCObj.ChannelType.UnsignedInteger: return new UnsignedStream();
            case DBCObj.ChannelType.Float32: return new FloatStream();
            case DBCObj.ChannelType.Float64: return new DoubleStream();
        }
        return null; 
    } 

    private static Type GetStreamType(DBCObj.ChannelType type) {
        switch(type)
        {
            case DBCObj.ChannelType.SignedInteger: return typeof(SignedStream);
            case DBCObj.ChannelType.UnsignedInteger: return typeof(UnsignedStream);
            case DBCObj.ChannelType.Float32: return typeof(FloatStream);
            case DBCObj.ChannelType.Float64: return typeof(DoubleStream);
        }
        return null; 
    } 

    Dictionary<string, DataStream> channeldata;
    DBCObj dbc;

    private void AddToStream(DataStream ds, DBCObj.DataContainer container, ulong timestamp) {
        switch (container.datatype) {
            case DBCObj.ChannelType.SignedInteger: 
                (ds as SignedStream).data.Add(container.long_data);
            break;
            case DBCObj.ChannelType.UnsignedInteger:
                (ds as UnsignedStream).data.Add(container.ulong_data);
            break;
            case DBCObj.ChannelType.Float32: 
                (ds as FloatStream).data.Add(container.float_data);
            break;
            case DBCObj.ChannelType.Float64: 
                (ds as DoubleStream).data.Add(container.double_data);
            break;
            default: return;
        }

        ds.timestamp.Add(timestamp);
    }

    private void parseFilepath(string filepath) {
        if (!File.Exists(filepath))
        {
            Console.WriteLine("FILE NOT FOUND!");
        }
        
        Console.WriteLine("h0");
        using (var stream = File.Open(filepath, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream, Encoding.Default, false))
            {
                /* read header if it exists */
                /* check that this doesn't break anything */ 
                while(stream.Position < stream.Length) {
                    ulong fullheader = reader.ReadUInt64();
                    Console.WriteLine(string.Format("{0:X}", fullheader));
                    if ((fullheader & 0xFC) != 0xB0) {
                        /* data is corrupt */
                        Console.WriteLine(String.Format("DATA CORRUPTION! {0:X} is not right", fullheader));
                        break;
                    }

                    ulong timestamp = fullheader >> 12;
                    uint bus = (uint)(fullheader >> 1) & 0x1;
                    bool extid = (fullheader & 0x1) != 0;
                    uint dlc = (uint)(fullheader >> 8) & 0xF;
                    uint id = 0;

                    if (extid) {
                        id = reader.ReadUInt32();
                    } else {
                        id = reader.ReadUInt16();
                    }

                    ulong data;
                    data = BitConverter.ToUInt64(reader.ReadBytes(Math.Min((int)dlc, 8)));

                    ushort crc = reader.ReadUInt16();
                    bool crc_valid = true;
                    /* TODO: crc validation */
                    if (!crc_valid) {
                        /* crc is wrong, throw out data */
                        continue;
                    }

                    var parsed = dbc.parseChannels((int)id, data);
                    Console.WriteLine(parsed.Count);
                    foreach (DBCObj.DataContainer c in parsed) {
                        DataStream ds = channeldata[c.channel];
                        AddToStream(ds, c, timestamp); 
                    }
                }
            }
        }
    }

    private FileData(string filepath, DBCObj obj) {
        dbc = obj;
        channeldata = new Dictionary<string, DataStream>();
    
        foreach (var x in dbc.getChannels())
        {
            channeldata.Add(x.Key, GetStreamOfType(x.Value));
        }

        parseFilepath(filepath);
    }

    public static FileData FromPath(string filepath, DBCObj obj) {
        return new FileData(filepath, obj); 
    }

    public void Print() {
        foreach (var s in channeldata)
        {
            Console.WriteLine(s.Key + ":");
            int v = 0;
            foreach (var ts in s.Value.timestamp)
            {
                Console.Write(ts + ": ");
                if ((s.Value as SignedStream) != null)
                    Console.WriteLine((s.Value as SignedStream).data[v]);
                if ((s.Value as UnsignedStream) != null)
                    Console.WriteLine((s.Value as UnsignedStream).data[v]);
                if ((s.Value as FloatStream) != null)
                    Console.WriteLine((s.Value as FloatStream).data[v]);
                if ((s.Value as DoubleStream) != null)
                    Console.WriteLine((s.Value as DoubleStream).data[v]);
                v++;
            }
        }
    }
}