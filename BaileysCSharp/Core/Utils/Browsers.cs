using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Utils
{
    public class Browsers
    {
        public static string[] Ubuntu(string browser) => ["Ubuntu", browser, "20.0.04"];
        public static string[] MacOS(string browser) => ["Mac OS", browser, "10.15.7"];
        public static string[] Baileys(string browser) => ["Baileys", browser, "4.0.0"];
        public static string[] Windows(string browser) => ["Windows", browser, "10.0.22621"];
    }
}
