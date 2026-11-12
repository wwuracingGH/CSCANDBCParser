/*
 * Nicole Swierstra
 * Arbitrary bytes printer
 *
 * Prints an arbitrary collection of bytes, similar to how a text editor might. Handy for debugging.
 */

using System.Text;
using System.Text.RegularExpressions;

class ArbitraryBytesPrinter
{
    public static void Print(string s)
    {
        Console.WriteLine("printing String of length: " + s.Length);
        for (int i = 0; i < s.Length; i += 16) {
            int length = Math.Min(16, s.Length-i);
            string line = s[i..(i+length)];
            string printable = Regex.Replace(line, @"\p{C}", ".");

            string bytes = BitConverter.ToString(Encoding.ASCII.GetBytes(line)).Replace("-", " ");
            bytes = bytes.PadRight((3 * 16) + 8);

            Console.WriteLine(bytes + printable);
        }
    }
}