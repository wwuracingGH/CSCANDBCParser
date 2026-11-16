using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using DbcParserLib;
using DbcParserLib.Model;

class DBCObj
{
    private HashSet<string> dupedsignalnames;

    /* mirrors dbc channel type */
    public enum SignalType 
    {
        Unknown = -1,
        SignedInteger = 0,
        UnsignedInteger = 1,
        Float32 = 2,
        Float64 = 3,
    }

    private struct CanFrame
    {
        public string Name;
        public List<Signal> signals;
    }

    /* This should be a union but at least for now it doesn't really matter */
    public struct DataContainer
    {
        public string signal_name;
        public double double_data;
        public float float_data;
        public long long_data;
        public ulong ulong_data;
        public SignalType datatype;

        public override string ToString()
        {
            switch(datatype)
            {
                case SignalType.SignedInteger: return signal_name + ":\t" + long_data;
                case SignalType.UnsignedInteger: return signal_name + ":\t" + ulong_data;
                case SignalType.Float32: return signal_name + ":\t" + float_data;
                case SignalType.Float64: return signal_name + ":\t" + double_data;
            }
            return "";
        }
    }

    public struct SignalInfo {
        public double min;
        public double max;
        public SignalType type;
        public string comment; /* if you want the comment */
        public double scale;
    }

    private string Filepath;
    /* 
     * <id, framedata> 
     */
    private Dictionary<int, CanFrame> Messages;

    private DBCObj(string fp) {
        dupedsignalnames = new HashSet<string>();
        Messages = new Dictionary<int, CanFrame>();
        Filepath = fp;

        Dbc dbc = Parser.ParseFromPath(fp);
        HashSet<string> allsignalnames = new HashSet<string>();
        foreach (var m in dbc.Messages) {
            int msgid = (int)m.ID;
            CanFrame frame = new CanFrame {
                signals = new List<Signal>(),
                Name = m.Name
            };

            foreach (var s in m.Signals)
            {
                frame.signals.Add(s); 
                if (allsignalnames.Contains(s.Name))
                    dupedsignalnames.Add(s.Name);
                else
                    allsignalnames.Add(s.Name);
            }

            Messages.Add(msgid, frame);
        }
    }

    /* factory */
    public static DBCObj fromFile(string filepath)
    {
        DBCObj obj = new DBCObj(filepath);
        return obj;
    }

    public string getFilepath()
    {
        /* just to make it absolutely sure the filepath is read only */
        return new string(Filepath);
    }

    /* if theres a channel name duplicate it adds the first part of the message name as well like in sql */
    private string getCannonicalNameOfSignal(int id, int chnum) {
        if (dupedsignalnames.Contains(Messages[id].signals[chnum].Name)) {
            return string.Format("{0}.{1}", Messages[id].Name, Messages[id].signals[chnum].Name);
        } else {
            return Messages[id].signals[chnum].Name;
        }
    }

    /* returns a list of all signal names and their corresponding types */
    public Dictionary<string, SignalInfo> getAllSignalInfo()
    {
        Dictionary<string, SignalInfo> channels = new Dictionary<string, SignalInfo>();
        foreach(var msg in Messages)
        {
            for (int i = 0; i < msg.Value.signals.Count; i++)
            {
                Signal channel = msg.Value.signals[i];
                string name = getCannonicalNameOfSignal(msg.Key, i);
                SignalInfo info = new SignalInfo {
                    type = (SignalType)channel.ValueType,
                    min = channel.Minimum,
                    max = channel.Maximum,
                    comment = channel.Comment,
                    scale = channel.Factor
                };
                channels.Add(name, info);
            }
        }

        return channels;
    }

    public List<DataContainer> parseSignalsFromFramedata(int id, ulong data)
    {
        List<DataContainer> parsedData = new List<DataContainer>();
        
        if (!Messages.ContainsKey(id)) 
            return [new DataContainer() {
                signal_name = "Unknown_" + id, 
                datatype = SignalType.UnsignedInteger, 
                ulong_data = data
            }];

        CanFrame m = Messages[id];

        int i = 0;
        foreach (Signal s in m.signals)
        {
            DataContainer dc = new DataContainer {
                datatype = (SignalType)s.ValueType
            };

            ulong binarydata = data >> s.StartBit;
            ulong mask = s.Length >= 64 ? 0xFFFFFFFFFFFFFFFF : (1UL << s.Length) - 1;
            binarydata &= mask;
            dc.signal_name = getCannonicalNameOfSignal(id, i);

            switch(dc.datatype)
            {
                case SignalType.SignedInteger:
                    if (((1UL << (s.Length - 1)) & binarydata) != 0 && s.Length < 64) {
                        binarydata |= 0xFFFFFFFFFFFFFFFF << s.Length;
                    }
                    dc.long_data = (long)binarydata;
                break;
                case SignalType.UnsignedInteger:
                    dc.ulong_data = binarydata;
                break;
                /* needs to be checked */
                case SignalType.Float32: 
                    dc.float_data = BitConverter.UInt32BitsToSingle((uint)binarydata); 
                break;
                case SignalType.Float64: 
                    dc.double_data = BitConverter.UInt64BitsToDouble(binarydata); 
                break;
            }

            parsedData.Add(dc);
            i++;
        }

        return parsedData;
    }
}
