using System;
using System.Collections.Generic;
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
        public string[] Imports { get; set; }
        public string Type { get; set; }
    }

    public class PackageManager
    {
        private static Dictionary<string, Package> ResolvedPackages = new Dictionary<string, Package>();

        private PackageManager() { }

        public static Package FindPackage(string name)
        {
            Console.WriteLine($"DEBUG: Trying to load package '{name}'");

            string tempPath = "tests";
            tempPath = Path.GetFullPath(tempPath);

            string packagePath = Path.Join(tempPath, name);

            string projectDataPath = Path.Join(packagePath, "MassProject.json");
            Debug.Assert(File.Exists(projectDataPath));

            Console.WriteLine($"DEBUG: Found package '{name}'");

            string projectDataContent = File.ReadAllText(projectDataPath);
            ProjectData data = JsonConvert.DeserializeObject<ProjectData>(projectDataContent);

            Console.WriteLine($"DEBUG: Loading package '{name}'");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Name: {data.Name}");
            Console.WriteLine($"Desc: {data.Desc}");
            Console.WriteLine($"Version: {data.Version}");
            Console.WriteLine($"Type: {data.Type}");
            Console.WriteLine("-----------------------------------------");

            // TODO(patrik): Load packages used in this package

            List<CompilationUnit> units = new List<CompilationUnit>();
            foreach (string file in data.Files)
            {
                string filePath = Path.Join(packagePath, file);
                // TODO(patrik): Real error?!?!?!?
                Debug.Assert(File.Exists(filePath));

                Console.WriteLine($"DEBUG: Compiling file '{filePath}' for '{data.Name}'");

                CompilationUnit unit = MassCompiler.CompileFile(filePath);
                units.Add(unit);
            }

            return new Package(data.Name, units);
        }

        public static void ResolvePackage(Package package)
        {
            foreach (var import in package.Imports)
            {
                ResolvePackage(import.Value);
            }

            Resolver resolver = new Resolver(package);
            resolver.ResolveSymbols();
            resolver.FinalizeSymbols();

            package.Resolver = resolver;

            ResolvedPackages.Add(package.Name, package);
        }
    }
}