using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Test
{
    public static class ExternalDllTest
    {
        public static void TestDllLoad()
        {
            Console.WriteLine(AbyssLib.GetVersion());
            Console.WriteLine(AbyssLib.GetError().ToString());
        }
    }
}
