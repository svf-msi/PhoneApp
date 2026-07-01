using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Diagnostics;

namespace StandardLib
{
    public static class Utils
    {
        public static string ToString<T>(T data)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            settings.Error = (obj, args) =>
            {
                var contextErrors = args.ErrorContext;
                contextErrors.Handled = true;
            };
            return ToString(data, settings);
        }

        public static T FromJson<T>(string contents)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            settings.Error = (obj, args) =>
            {
                var contextErrors = args.ErrorContext;
                contextErrors.Handled = true;
            };
            return FromJson<T>(contents, settings);
        }

        public static string ToString<T>(T data, JsonSerializerSettings jsonSettings)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented, jsonSettings);
        }

        public static T FromJson<T>(string contents, JsonSerializerSettings jsonSettings)
        {
            return JsonConvert.DeserializeObject<T>(contents, jsonSettings);
        }

        public static T1 Clone<T1, T2>(T2 data) where T2 : T1
        {
            try
            {
                var settings = new JsonSerializerSettings();
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                settings.Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;
                };
                var text = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
                //Console.WriteLine(text);
                return JsonConvert.DeserializeObject<T1>(text, settings);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in cloning: {e}");
                return default(T1);
            }
        }

        public static void LogDebugMessage(string message, string logfile = null)
        {
            if (logfile == null)
                logfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DebugLog.txt";
            StreamWriter log = new StreamWriter(logfile, true);
            log.WriteLine(message);
            log.Flush();
            log.Close();
        }

        public static void TimeCheck(Action func, string label = "Time check")
        {
            var watch = new Stopwatch();
            watch.Start();
            func?.Invoke();
            watch.Stop();
            Console.WriteLine($"{label} : elaplsed {watch.ElapsedMilliseconds} msecs");
        }

        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    action();
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }

        public static void Execute(Action action, bool sync = false)
        {
            if (action == null) return;
            if (!sync)
            {
                Task.Run(() => action());
            }
            else
                action();
        }

        public static void ClearFolder(string folderName, bool includeSubfolders = true)
        {
            DirectoryInfo dir = new DirectoryInfo(folderName);

            if (dir.Exists)
            {
                foreach (FileInfo fi in dir.GetFiles())
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception e) { Console.WriteLine($"Error in clearing folder {folderName}: {e.Message}"); }
                }

                if (includeSubfolders)
                {
                    foreach (DirectoryInfo di in dir.GetDirectories())
                    {
                        try
                        {
                            ClearFolder(di.FullName);
                            di.Delete();
                        }
                        catch (Exception e) { Console.WriteLine($"Error in clearing folder {folderName}: {e.Message}"); }
                    }
                }
            }
        }

        public static T FromFile<T>(string filepath) where T : class
        {
            try
            {
                if (!File.Exists(filepath))
                {
                    Console.WriteLine($"{filepath} does not exists");
                    return null;
                }
                var file = File.ReadAllText(filepath);
                return JsonConvert.DeserializeObject<T>(file);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Reading scene JSON error: {e.Message}");
            }
            return null;
        }

    }
}
