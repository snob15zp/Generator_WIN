using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratorAppMain.Utils
{
    class Strings
    {
        public static string NormalizeVersion(string version)
        {
            return String.Join(".", version.Split('.').Select(v => Int32.Parse(v)));
        }
    }
}