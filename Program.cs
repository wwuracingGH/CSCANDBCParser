class Program {
    static void Main(string[] args) {
        DBCObj obj = DBCObj.fromFile("test.dbc");
        
        obj.getChannels().ToList().ForEach(x => Console.WriteLine(x.Key + "\t" + x.Value));

        List<DBCObj.DataContainer> data = obj.parseChannels(1, 0xFF800000FFFFFF00);
        data.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        List<DBCObj.DataContainer> data2 = obj.parseChannels(2, 0xFFFFFFFFFFFFFFFF);
        data2.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        List<DBCObj.DataContainer> data3 = obj.parseChannels(3, 0x40a000003f800000);
        data3.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();
        
        List<DBCObj.DataContainer> data4 = obj.parseChannels(4, 0x400921FB54442D18);
        data4.ForEach(x => Console.WriteLine(x));
        Console.WriteLine();

        FileData.FromPath("can/logs/11-10-2026_2132.log", obj).Print();

        JSONParser p = new JSONParser("ReprogrammingValues/repronodes.jsnx");
        Console.WriteLine(p);
    } 
}