using BaileysCSharp.Core.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Helper
{
    internal static class JsonHelper
    {
        public static JsonSerializerOptions Options
        {
            get
            {
                return new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
            }
        }
        public static JsonSerializerOptions BufferOptions
        {
            get
            {
                return new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    Converters =
                    {
                        new BufferConverter()
                    }
                };
            }
        }
    }
}
