using Neurotec.Biometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Models
{
    public class BiometricMatch
    {
        public string Id;
        public int TenantId;
        public string MatchingAction;
        public NSubject Subject;
        public NBiometricStatus Status;
        public NMatchingResult MatchingResult;

        public BiometricMatchStaging GetStaging()
        {
            /*int MatchTenantId = int.Parse(
                    MatchingResult.Subject.Properties["TenantId"].ToString()
                );*/

            return new BiometricMatchStaging
            {
                Id = new Guid(Id),
                TenantId = TenantId,    
                MatchId = new Guid(MatchingResult.Id),
                MatchTenantId = TenantId,
                Score = MatchingResult.Score,
                Status = (int)Status
            };
        }
    }
}
