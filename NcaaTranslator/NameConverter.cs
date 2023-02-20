using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using static NcaaTranslator.NameConverter;

namespace NcaaTranslator
{
    internal class NameConverter
    {
        public string char6 { get; set; }
        public string @short { get; set; }
        public string customShort { get; set; }

        public NameConverter() { } 
        public NameConverter(Names names) 
        {
            this.char6 = names.char6;
            this.@short = names.@short;
            this.customShort = "";
        }
    }

    public class NameConverters
    {
        internal static Dictionary<String, NameConverter> NameDict { get; set; } = new Dictionary<String, NameConverter>();
        internal static List<NameConverter> NameList { get; set; }
        internal static string FilePath = "NcaaNameConverter.json";

        public static void Load()
        {
            string jsonString = File.ReadAllText(FilePath);
            NameList = JsonSerializer.Deserialize<List<NameConverter>>(jsonString);
            NameDict = NameList.ToDictionary(x => x.char6, x => x);
        }
        public static void Reload()
        {
            NameList.OrderBy(x => x.char6);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(NameList, new JsonSerializerOptions() { WriteIndented = true }));
            Load();
        }

        public static string Lookup(Names lookupNames)
        {
            var name = new NameConverter();

            return NameDict.TryGetValue(lookupNames.char6, out name) ? name.customShort : AddNewTeam(lookupNames);
        }

        public static string AddNewTeam(Names names)
        {
            var newTeam = new NameConverter(names);
            NameList.Add(newTeam);
            Reload();
            return "";
        }
    }
}
