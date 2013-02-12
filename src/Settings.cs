#region using

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace Dry.Common {
    public class Settings {
        const string SettingsFile = "settings.json";
        static readonly string ConfigPath = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string ConfigFull = Path.Combine(ConfigPath, SettingsFile);
        static readonly IDictionary<string, object> Dict = new ConcurrentDictionary<string, object>();
        static readonly FileSystemWatcher Fs;
        static readonly object Syncobj = new object();

        public static string[] AllKeys {
            get { return Dict.Keys.ToArray(); }
        }

        static Settings() {
            if (!File.Exists(ConfigFull)) {
                WriteConfig();
            }
            ReadConfig();
            Fs = new FileSystemWatcher(ConfigPath, SettingsFile);
            Fs.Changed += FsOnChanged;
            Fs.EnableRaisingEvents = true;
        }

        static void FsOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs) {
            ReadConfig();
        }

        static void ReadConfig() {
            lock(Syncobj)
            try {
                using(var fs = new FileStream(ConfigFull, FileMode.Open)) {

                    using (var reader = new StreamReader(fs)) {
                        var sb = new StringBuilder(reader.ReadToEnd());
                        sb.Replace('\n', ' ');
                        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(sb.ToString());
                        Dict.Clear();
                        foreach (var key in dict.Keys) {
                            Dict.Add(key, dict[key]);
                        }
                    }
                }
            } catch {}
        }

        static void WriteConfig() {
            Fs.EnableRaisingEvents = false;
            try {
                using (var fs = new FileStream(ConfigFull, FileMode.Truncate)) {
                    using (var writer = new StreamWriter(fs)) {
                        var output = Newtonsoft.Json.JsonConvert.SerializeObject(Dict);
                        writer.Write(output);
                    }
                }
            } finally {
                Fs.EnableRaisingEvents = true;
            }
        }

        public static object Get(string key) {
            return Dict.TryGet(key);
        }

        public static void Set(string key, string value) {
            Dict.Upsert(key, value);
            WriteConfig();
        }
    }
}