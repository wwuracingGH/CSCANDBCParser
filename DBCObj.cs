using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using DbcParserLib;
using DbcParserLib.Model;

class DBCObj
{
    /* mirrors dbc channel type */
    public enum ChannelType 
    {
        Unknown = -1,
        SignedInteger = 0,
        UnsignedInteger = 1,
        Float32 = 2,
        Float64 = 3,
    }

    private class CanFrame
    {
        public List<Signal> channels;
    }

    /* This should be a union but at least for now it doesn't really matter */
    public struct DataContainer
    {
        public string channel;
        public double double_data;
        public float float_data;
        public long long_data;
        public ulong ulong_data;
        public ChannelType datatype;

        public override string ToString()
        {
            switch(datatype)
            {
                case ChannelType.SignedInteger: return channel + ":\t" + long_data;
                case ChannelType.UnsignedInteger: return channel + ":\t" + ulong_data;
                case ChannelType.Float32: return channel + ":\t" + float_data;
                case ChannelType.Float64: return channel + ":\t" + double_data;
            }
            return "";
        }
    }

    private string Filepath;
    /* 
     * <id, framedata> 
     */
    private Dictionary<int, CanFrame> Messages;

    private DBCObj(string fp) {
        Messages = new Dictionary<int, CanFrame>();
        Filepath = fp;

        Dbc dbc = Parser.ParseFromPath(fp);
        foreach (var m in dbc.Messages) {
            int msgid = (int)m.ID;
            CanFrame frame = new CanFrame();
            frame.channels = new List<Signal>();
            
            foreach (var s in m.Signals)
            {
                frame.channels.Add(s); 
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

    public Dictionary<string, ChannelType> getChannels()
    {
        Dictionary<string, ChannelType> channels = new Dictionary<string, ChannelType>();

        foreach(CanFrame m in Messages.Values)
        {
            foreach(Signal s in m.channels)
            {
                channels.Add(s.Name, (ChannelType)s.ValueType);   
            }
        }

        return channels;
    }

    public List<DataContainer> parseChannels(int id, ulong data)
    {
        List<DataContainer> parsedData = new List<DataContainer>();
        if (!Messages.ContainsKey(id)) return null;

        CanFrame m = Messages[id];

        foreach (Signal s in m.channels)
        {
            DataContainer dc = new DataContainer {
                datatype = (ChannelType)s.ValueType
            };

            ulong binarydata = data >> s.StartBit;
            ulong mask = s.Length >= 64 ? 0xFFFFFFFFFFFFFFFF : (1UL << s.Length) - 1;
            binarydata &= mask;
            dc.channel = s.Name;

            switch(dc.datatype)
            {
                case ChannelType.SignedInteger:
                    if (((1UL << (s.Length - 1)) & binarydata) != 0 && s.Length < 64) {
                        binarydata |= 0xFFFFFFFFFFFFFFFF << s.Length;
                    }
                    dc.long_data = (long)binarydata;
                break;
                case ChannelType.UnsignedInteger:
                    dc.ulong_data = binarydata;
                break;
                /* needs to be checked */
                case ChannelType.Float32: 
                    dc.float_data = BitConverter.UInt32BitsToSingle((uint)binarydata); 
                break;
                case ChannelType.Float64: 
                    dc.double_data = BitConverter.UInt64BitsToDouble(binarydata); 
                break;
            }

            parsedData.Add(dc);
        }

        return parsedData;
    }
}
