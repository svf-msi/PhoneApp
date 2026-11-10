using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public class Project
    {
        #region Static section

        public static string DefaultExtension { get; private set; } = ".vibs";

        public static string DefaultName { get; private set; } = "project" + DefaultExtension;

        public static Project Read(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            var text = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(text)) return null;

            try
            {
                var settings = new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                };
                var project = JsonConvert.DeserializeObject<Project>(text, settings);
                project.FileName = filePath;
                Debug.WriteLine($"[Debug]: {StandardLib.Utils.ToString(project)}");
                return project;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in reading project: {e}");
                return null;
            }
        }

        public static bool Create(string projectName, out Project project, out bool duplicate, string folder = "")
        {
            project = null;
            duplicate = false;

            if (string.IsNullOrEmpty(projectName)) return false;

            var projectPath = $"{App.DataFolder}{folder}{projectName}{DefaultExtension}";
            if (File.Exists(projectPath))
            {
                duplicate = true;
                return false;
            }

            project = new Project { Name = projectName, FileName = projectPath };
            project.Save();
            Debug.WriteLine($"[Debug]: created {project.Name}, check {project.FileName}");
            return true;
        }

        #endregion

        [JsonIgnore]
        public string FileName { get; set; } = ""; 
        
        [JsonIgnore]
        public bool IsSaving { get; private set; }

        public string Name { get; set; } = "";

        public List<DataChart> DataCharts { get; set; } = new List<DataChart>();

        public void Save(string file = null)
        {
            if (file == null) file = FileName;
            if (file == null || IsSaving) return;

            try
            {
                IsSaving = true;
                var text = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(file, text);
                FileInfo fileInfo = new FileInfo(file);

                //Debug.WriteLine($"[Debug]: {text}");
                Debug.WriteLine($"[Debug]: saved {fileInfo.Length / 1024} kbytes to {Name}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in saving project: {e}");
            }
            finally { IsSaving = false; }
        }
    }
}
