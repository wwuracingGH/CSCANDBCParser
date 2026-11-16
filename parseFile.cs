using System.Text;

class FileData {
    private static DataStream GetStreamOfType(DBCObj.SignalInfo info) {
        switch(info.type)
        {
            case DBCObj.SignalType.SignedInteger: return new SignedStream(info);
            case DBCObj.SignalType.UnsignedInteger: return new UnsignedStream(info);
            case DBCObj.SignalType.Float32: return new FloatStream(info);
            case DBCObj.SignalType.Float64: return new DoubleStream(info);
        }
        return null; 
    } 

    private static Type GetStreamType(DBCObj.SignalType type) {
        switch(type)
        {
            case DBCObj.SignalType.SignedInteger: return typeof(SignedStream);
            case DBCObj.SignalType.UnsignedInteger: return typeof(UnsignedStream);
            case DBCObj.SignalType.Float32: return typeof(FloatStream);
            case DBCObj.SignalType.Float64: return typeof(DoubleStream);
        }
        return null; 
    } 

    Dictionary<string, DataStream> channeldata;
    DBCObj dbc;

    private void AddToStream(DataStream ds, DBCObj.DataContainer container, ulong timestamp) {
        switch (container.datatype) {
            case DBCObj.SignalType.SignedInteger: 
                ds.append(timestamp, container.long_data);
            break;
            case DBCObj.SignalType.UnsignedInteger:
                ds.append(timestamp, container.ulong_data);
            break;
            case DBCObj.SignalType.Float32: 
                ds.append(timestamp, container.float_data);
            break;
            case DBCObj.SignalType.Float64: 
                ds.append(timestamp, container.double_data);
            break;
            default: return;
        }
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

                    var parsed = dbc.parseSignalsFromFramedata((int)id, data);
                    Console.WriteLine(parsed.Count);
                    foreach (DBCObj.DataContainer c in parsed) {
                        DataStream ds = channeldata[c.signal_name];
                        AddToStream(ds, c, timestamp); 
                    }
                }
            }
        }
    }

    private FileData(string filepath, DBCObj obj) {
        dbc = obj;
        channeldata = new Dictionary<string, DataStream>();
    
        foreach (var x in dbc.getAllSignalInfo())
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
            foreach (var ts in s.Value.getTimestamps())
            {
                Console.Write(ts + ": ");
                Console.WriteLine(s.Value.strvalueof(v));
                v++;
            }
        }
    }

    public void WriteToCSVFile(string filepath, int hz) {
        DataStream timestamp_base;
        if (!channeldata.TryGetValue("Latitude", out timestamp_base)) {
            var e = channeldata.GetEnumerator();
            while(e.Current.Value.getLength() == 0)
            {
                e.MoveNext();
            }
            timestamp_base = e.Current.Value;
        }
        ulong start = timestamp_base.getTimestamps().First();
        ulong end = timestamp_base.getTimestamps().Last();
        
        List<ulong> new_timestamps = new List<ulong>();
        for (ulong i = start; i <= end; i += (ulong)(1000000/hz)) {
            new_timestamps.Add(i);
        }

        List<string> allchannelnames = new List<string>();
        List<Array> allchannels = new List<Array>();
        foreach (var d in channeldata) {
            string channelname = d.Key;

            allchannelnames.Append(channelname);
            allchannels.Append(d.Value.resampleraw(new_timestamps.ToArray()));
        }
        using (StreamWriter wr = new StreamWriter(filepath, false)) {
            for (int i = 0; i < allchannelnames.Count; i++) {
                wr.Write(allchannelnames[i]);
                if (i != allchannelnames.Count - 1) {
                    wr.Write(',');
                }
            }

            wr.WriteLine();
            for (int i = 0; i < new_timestamps.Count; i++) {
                for (int j = 0; j < allchannels.Count; j++)
                {
                    wr.Write(allchannels[j].GetValue(i).ToString());
                    if (j != allchannels.Count - 1) {
                        wr.Write(',');
                    }
                } 
            }
        }
    }
}