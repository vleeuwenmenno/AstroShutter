using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AstroShutter
{
    public class Program
    {
        public string subject {get; set;}
        public bool downloadAfterwards {get; set;}
        public bool downloadImmediatly {get; set;}
        public bool requestUserInput {get; set;}
        // public bool convertToRawAfterDownload {get; set;}
        public string saveDirectory {get;set;}
        public List<ProgramEntry> entries;

        public Program()
        { }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        public static Program Load(string path)
        {
            return JsonConvert.DeserializeObject<Program>(File.ReadAllText(path));
        }
    }

    public class ProgramEntry
    {
        public int exposures {get; set;}
        public string shutter {get; set;}
        public string duration {get; set;}
        public string iso {get; set;}

        public ProgramEntry(int exposures, string shutter, string duration, string iso)
        {
            this.exposures = exposures;
            this.shutter = shutter;
            this.duration = duration;
            this.iso = iso;
        }
    }
}