using Newtonsoft.Json;

namespace BaileysCSharp.Core.Models
{
    public class JidWidhDevice
    {
        public string User { get; set; }
        public int? Device { get; set; }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);   
        }
    }
}
