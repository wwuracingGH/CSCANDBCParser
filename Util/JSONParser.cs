/*
 * Nicole Swierstra
 * Simple JSON Parser with some custom functionality and a new filetype "jsnx"
 * 
 * Imported from past project and converted from java
 * 
 * The fact that it's regex based is kind of cursed
 */

using System.Text;
using System.Text.RegularExpressions;

public class JSONParser {
    /* finds all spaces that aren't in quotes */
    const string spacesRegex = "\\s+(?=([^\"]*\"[^\"]*\")*[^\"]*$)"; 
    private object obj;
    const string commentregex = "\\/\\/.*|\\/\\*.*\\*\\/";

    /* removes comments from the string */
    private string JsnxPreprocess(string input)
    {
        return Regex.Replace(input, commentregex, "");
    }

    /*
     * creates the object
     */
    public JSONParser(string filepath) {
        string jsonstring = File.ReadAllText(filepath);

        jsonstring = JsnxPreprocess(jsonstring);

        jsonstring = Regex.Replace(jsonstring, spacesRegex, "").Replace("\n", "").Replace("\r", "");        
       
        obj = parse(jsonstring);
    }

    /*
     * Returns the JSON file as a generic Object
     */
    public object getObj(){
        return obj;
    }

    /*
     * finds all commas outside of any brackets and splits on them
     */
    private static string[] jsonSplit(string toSplit){
        int bracketCounter = 0;
        int braceCounter = 0;
        bool inquote = false;

        List<string> strings = new List<string>();
        strings.Add("");

        for(int i = 0; i < toSplit.Length; i++){
            switch(toSplit[i]){
                case '}': bracketCounter--; break;
                case '{': bracketCounter++; break;
                case '[': braceCounter++; break;
                case ']': braceCounter--; break;
                case '"': inquote = !inquote; break;
               
                case ',':
                    if(braceCounter == 0 && bracketCounter == 0 && !inquote) {
                        strings.Add(""); 
                        continue;
                    }
                break; 
                default: break;
            }
            strings[strings.Count - 1] = strings.Last() + toSplit[i];
        }

        return strings.ToArray<string>();
    }

    private static object parse(string toparse){
        
        if(toparse.Length == 0) return null;
        switch(toparse[0]){
            case '0':
                if (toparse[1] == 'x') {
                    return Convert.ToInt32(toparse, 16);
                } else if (toparse[1] == 'b') {
                    return Convert.ToInt32(toparse, 2);
                }
                goto case '9';
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                try {
                    return int.Parse(toparse);
                } catch (Exception e) {
                    return float.Parse(toparse);
                }

            case '"':
                return toparse.Substring(1, toparse.Length - 1);

            case '[':{
                List<object> al = new List<object>();
                string[] arr = jsonSplit(toparse[1..(toparse.Length - 1)]);
                foreach(string s in arr){
                    al.Add(parse(s));
                }
                return al;
            }
            
            case '{':{
                Dictionary<string, object> hm = new Dictionary<string, object>();
                string[] arr = jsonSplit(toparse[1..(toparse.Length - 1)]);
                foreach (string s in arr) {
                    string[] ss = s.Split(":", 2);
                    hm.Add(ss[0].Substring(1, ss[0].Length - 1), parse(ss[1]));
                }
                return hm;
            }
        }

        return null;
    }

    static string getObjString(Object obj)
    {
        string retstr = "";
        if (obj as Dictionary<String, Object> != null) {
            retstr += "{\n";
            foreach (var h in (obj as Dictionary<String, Object>)) {
                retstr += string.Format("\"{0}\" : {1},", h.Key, h.Value);
            }
            retstr += "}\n";
        } else if (obj as List<Object> != null) {
            retstr += "[\n";
            foreach (var h in (obj as List<Object>)) {
                retstr += string.Format("{0},", h);
            }
            retstr += "]\n";
        } else if ((obj as Int32) != null) {
            
        }

        return retstr;
    }

    public override string ToString() {
        getObjString(obj);
    }
}
