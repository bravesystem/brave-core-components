using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Models
{
    public class BiometricMatchStaging
    {
        public Guid Id;
        public int TenantId;
        public Guid MatchId;
        public int MatchTenantId;
        public int Score;
        public int Status;
    }
}
