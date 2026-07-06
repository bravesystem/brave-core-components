using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Models
{
    public class Biometric
    {
        public string Id { get; set; }
        public byte[] Template { get; set; }
        public int TenantId { get; set; }
        public int Gender { get; set; }
        public string MatchingAction { get; set; }
        public bool IsEncrypted { get; set; } = false;
    }
}
