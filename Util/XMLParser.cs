/* 
 * Nicole Swierstra
 * Barebones XML parser
 *
 * Imported from past project
 * 
 * My way to make XML files more generic and not have 300 dependancies on everything 
 * Tbh tho is this even necessary in c#? I wrote this origional for java but c# seems 
 * a lot more fine tbh 
 */

using System.Xml;
public class XMLParser {
    public class XMLObj {
        public string tag;
        public Dictionary<string, string> attribs;
        public List<XMLObj> children;
        public string contents;

        public XMLObj() {
            attribs = new Dictionary<string, string>();
            children = new List<XMLObj>();
            contents = null;
        }
    }

    public static XMLObj ReadFile(string filepath) {
        XmlDocument xml = new XmlDocument();
        xml.Load(filepath);

        return transcribe(xml.DocumentElement);
    }

    static XMLObj transcribe(XmlElement node) {
        XMLObj obj = new XMLObj();
        obj.tag = node.Name;
        
        XmlAttributeCollection attribs = node.Attributes;
        for (int i = 0; i < attribs.Count; i++) {
            obj.attribs.Add(attribs[i].Name, attribs[i].Value);
        }

        XmlNodeList children = node.ChildNodes;
        for (int i = 0; i < children.Count; i++) {
            if (children[i] as XmlElement == null) continue;
            obj.children.Add(transcribe((XmlElement)children[i]));
        }
        obj.contents = node.InnerText;

        return obj;
    }
}
