using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace AstroShutter
{
    public class Program
    {
        public string subject {get; set;}
        public bool createDir {get;set;}
        public bool createSubDir {get;set;}
        public bool downloadIm {get;set;}
        public bool downloadAft {get;set;}
        public bool renamePhotos {get;set;}
        public bool makeImageTypeDir {get;set;}
        public bool requestUserInput {get; set;}
        public string saveDirectory {get;set;}
        public List<ProgramEntry> entries;

        public bool sequenceStarted;
        public bool sequenceFinished;
        public DateTime? sequenceStartedAt;

        public Program()
        { }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public string JsonString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
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
        public bool isBulb {get; set;}
        public string iso {get; set;}
        public string imageQuality {get; set;}


        public bool queueBegun {get;set;}
        public List<List<string>> exposuresDone {get;set;}

        public ProgramEntry()
        {
            
        }

        public ProgramEntry(int exposures, string shutter,bool isBulb, string duration, string iso)
        {
            this.exposures = exposures;
            this.shutter = shutter;
            this.duration = duration;
            this.isBulb = isBulb;
            this.iso = iso;
        }
    }
}