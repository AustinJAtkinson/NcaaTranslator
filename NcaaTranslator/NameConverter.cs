using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NcaaTranslator.NameConverter;

namespace NcaaTranslator
{
    internal class NameConverter
    {
        public string char6 { get; set; }
        public string @short { get; set; }
        public string customShort { get; set; }

        public NameConverter() { }
        public NameConverter(string line)
        {
            var paramaters = line.Split('\t');
            this.char6 = paramaters[0];
            this.@short = paramaters[1];
            this.customShort = paramaters[2];
        }
    }

    public class NameConverters
    {
        internal static Dictionary<String, NameConverter> NameDict { get; set; }

        static NameConverters()
        {
            NameDict = new Dictionary<String, NameConverter>();
        }

        public static void Load(string dataFile)
        {
            using (var textReader = new StreamReader(dataFile))
            {

                string line = textReader.ReadLine();

                while (line != null)
                {

                    if (!line.StartsWith(@"//"))
                    {
                        var name = new NameConverter(line);
                    }

                    line = textReader.ReadLine();
                }
            }
        }

        public static string Lookup(string char6)
        {
            var name = new NameConverter();

            return NameDict.TryGetValue(char6, out name) ? name.customShort : "";
        }
    }
}
