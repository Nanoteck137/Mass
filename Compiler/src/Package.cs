using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public class Package
    {
        public string Name { get; private set; }
        public List<CompileUnit> CompileUnits { get; private set; }

        /*public Package(string name, List<CompileUnit> compileUnits)
        {
            this.Name = name;
            this.CompileUnits = compileUnits;
        }*/

        public static Package Import(string workingPath, string name)
        {
            string libPath = Path.Combine(workingPath, name);
            string[] files = Directory.GetFiles(libPath);

            List<CompileUnit> compileUnits = new List<CompileUnit>();
            foreach (string fileName in files)
            {
                // TODO(patrik): Change "ma" to a better constant if the file ext changes in the future
                if (Path.GetExtension(fileName) == ".ma")
                {
                    compileUnits.Add(CompileUnit.CompileFile(fileName));
                }
            }

            Package result = new Package
            {
                Name = name,
                CompileUnits = compileUnits
            };

            return result;
        }
    }
}
