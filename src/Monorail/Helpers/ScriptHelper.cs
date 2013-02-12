#region using

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Castle.Core.Logging;
using Castle.MonoRail.Framework.Helpers;

#endregion

namespace Dry.Common.Monorail.Helpers {
    public class ScriptHelper : AbstractHelper {
        public string RegisterActionScript() {
            var list = new List<string> {"~"};
            list.AddRange(Context.Services.ViewSourceLoader.VirtualViewDir.SplitAndTrim("\\", "/"));
            list.AddRange(Context.CurrentControllerContext.ViewFolder.SplitAndTrim("\\", "/"));
            list.Add(Context.CurrentControllerContext.Action + ".js");
            return RegisterScript(list.Join("/"));
        }

        static readonly IDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        static readonly IDictionary<string, string> Dict = new ConcurrentDictionary<string, string>();

        static void AddWatcher(string path) {
            if (path == null) return;
            if (Watchers.ContainsKey(path)) return;
            var fs = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            fs.Changed += (o, e) => {
                try {
                    Dict.Remove(e.FullPath);
                } catch (Exception error) {
                    var logger = IoC.Resolve<ILogger>() ?? NullLogger.Instance;
                    logger.Error("Failure removing file hash key.", error);
                }
            };
            fs.EnableRaisingEvents = true;
            Watchers.Add(path, fs);
        }

        public string RegisterScriptFolder(string path) {
            var realPath = Context.Server.MapPath(path);
            var scripts = new StringBuilder();
            foreach (var script in new DirectoryInfo(realPath).EnumerateFiles("*.js")) {
                var scriptpath = script.FullName.Replace(Context.Server.MapPath("~"), "~/").Replace(Path.DirectorySeparatorChar, '/');
                scripts.Append(RegisterScript(scriptpath));
            }
            return scripts.ToString();
        }

        public string RegisterScript(string url) {
            var realPath = Context.Server.MapPath(url);
            if (!File.Exists(realPath)) return null;

            lock(Dict) {
                CalculateHash(realPath);

                var sb = new StringBuilder();
                sb.Append("<script src=\"")
                    .Append(url.StartsWith("~") ? url.TrimStart('~') : url)
                    .Append("?")
                    .Append(Dict[realPath])
                    .Append("\"></script>");
                return sb.ToString();
            }
        }

        static void CalculateHash(string realPath) {
            AddWatcher(realPath);
            if (!Dict.ContainsKey(realPath)) {
                try {
                    using (var fs = new FileStream(realPath, FileMode.Open)) {
                        Dict.Add(realPath,
                                 MD5.Create().ComputeHash(fs).ToBase64String()
                            );
                    }
                } catch (Exception e) {
                    var logger = IoC.Resolve<ILogger>() ?? NullLogger.Instance;
                    logger.Error("Failure adding file hash key.", e);
                }
            }
        }

        public string RegisterStyle(string url) {
            return RegisterStyle(url, null);
        }

        public string RegisterStyle(string url, IDictionary dict) {
            var realPath = Context.Server.MapPath(url);
            if (!File.Exists(realPath)) return null;

            lock(Dict) {
                CalculateHash(realPath);

                var sb = new StringBuilder();
                sb.Append("<link href=\"")
                    .Append(Context.ApplicationPath)
                    .Append(url.StartsWith("~") ? url.TrimStart('~') : url)
                    .Append("?")
                    .Append(Dict[realPath])
                    .Append("\" rel=\"stylesheet\" ");

                if (dict != null) {
                    foreach (var key in dict.Keys) {
                        sb.Append(" ")
                            .Append(key)
                            .Append("=")
                            .Append("\"")
                            .Append(dict[key])
                            .Append("\"");
                    }
                }
                sb.Append("/>");
                return sb.ToString();
            }
        }
    }
}
