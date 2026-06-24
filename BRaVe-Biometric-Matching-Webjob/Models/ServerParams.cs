using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Models
{
    public class ServerParams
    {
        public string Host { get; set; }
        public int AdminPort { get; set; } = 24932;
        public int ClientPort { get; set; } = 25452;
        public int MinMatchScore { get; set; } = 200;
        public int matchingThreshold { get; set; } = 70;
    }
}
