using BaileysCSharp.Core.Helper;
using System.Text.Json;

namespace BaileysCSharp.Core.Signal
{
    public class JidWidhDevice
    {
        public string User { get; set; }
        public uint? Device { get; set; }


        public override string ToString()
        {
            return JsonSerializer.Serialize(this, JsonHelper.Options);
        }
    }
}
