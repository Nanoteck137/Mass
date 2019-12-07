using System;
using System.Diagnostics;
using System.IO;

using Newtonsoft.Json;

namespace Mass.Compiler
{
    class ProjectData
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Version { get; set; }
        public string[] Files { get; set; }
        public bool Library { get; set; }
    }

    public class PackageManager
    {
        private PackageManager() { }

        public static Package FindPackage(string name)
        {
            Console.WriteLine($"DEBUG: Trying to load package '{name}'");

            string tempPath = "tests";

            string packagePath = Path.Join(tempPath, name);

            string projectDataPath = Path.Join(packagePath, "MassProject.json");
            Debug.Assert(File.Exists(projectDataPath));

            Console.WriteLine($"DEBUG: Found package '{name}'");

            string projectDataContent = File.ReadAllText(projectDataPath);
            ProjectData data = JsonConvert.DeserializeObject<ProjectData>(projectDataContent);

            Console.WriteLine($"DEBUG: Loading package '{name}'");
            Console.WriteLine("--------------------------");
            Console.WriteLine($"Name: {data.Name}");
            Console.WriteLine($"Desc: {data.Desc}");
            Console.WriteLine($"Version: {data.Version}");
            Console.WriteLine($"Is a Library: {data.Library}");
            Console.WriteLine("--------------------------");

            foreach (string file in data.Files)
            {
                string filePath = Path.Join(packagePath, file);
                Console.WriteLine($"DEBUG: Compiling file {filePath}");

                CompilationUnit unit = MassCompiler.CompileFile(filePath);
            }

            return null;
        }
    }
}