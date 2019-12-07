using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public class Package
    {
        // TODO(patrik): Add the rest of the info from the MassProject.json??
        public string Name { get; private set; }
        public Dictionary<string, CompilationUnit> Units { get; private set; }

        public bool IsMain { get; private set; }

        public Dictionary<string, Package> Imports { get; private set; }

        public Resolver Resolver { get; set; }

        public Package(string name, List<CompilationUnit> units, bool isMain = false)
        {
            this.Name = name;

            this.Units = new Dictionary<string, CompilationUnit>();
            foreach (CompilationUnit unit in units)
            {
                this.Units.Add(Path.GetFileNameWithoutExtension(unit.FilePath), unit);
            }

            this.IsMain = isMain;

            this.Imports = new Dictionary<string, Package>();
        }

        public void ImportPackage(Package package)
        {
            Debug.Assert(!Imports.ContainsKey(package.Name));

            Imports.Add(package.Name, package);
        }

        public Package GetImportPackage(string name)
        {
            if (!Imports.ContainsKey(name))
                return null;

            return Imports[name];
        }

        public CompilationUnit FindUnitByName(string name)
        {
            if (!Units.ContainsKey(name))
                return null;

            return Units[name];
        }
    }
}
