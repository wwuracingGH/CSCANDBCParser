class Program {
    static void Main(string[] args) {
        DBCObj obj = DBCObj.fromFile("sigsremoved.dbc");
        
        obj.getAllSignalInfo().ToList().ForEach(x => Console.WriteLine(x.Key + "\t" + x.Value));

        obj = DBCObj.fromFile("test.dbc");
        List<DBCObj.DataContainer> data = obj.parseSignalsFromFramedata(0x101, 0xFF800000FFFFFF00);
        data.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        List<DBCObj.DataContainer> data2 = obj.parseSignalsFromFramedata(0x101, 0xFFFFFFFFFFFFFFFF);
        data2.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        List<DBCObj.DataContainer> data3 = obj.parseSignalsFromFramedata(0x101, 0x40a000003f800000);
        data3.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();
        
        List<DBCObj.DataContainer> data4 = obj.parseSignalsFromFramedata(0x101, 0x400921FB54442D18);
        data4.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        obj = DBCObj.fromFile("test.dbc");
        FileData.FromPath("can/logs/11-10-2026_2132.log", obj).Print();

        JSONParser p = new JSONParser("ReprogrammingValues/repronodes.json5");
        Console.WriteLine(p);
    } 
}