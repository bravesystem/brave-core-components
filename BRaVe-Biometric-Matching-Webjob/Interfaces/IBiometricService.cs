using BRaVe_Biometric_Matching_Webjob.Models;
using Neurotec.Biometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Interfaces
{
    public interface IBiometricService
    {
        Task<List<Biometric>> GetBiometrics(string jobId, CancellationToken ct);
        Task LogUnknownAllStatuses(List<BiometricMatch> biometricMatches);
        Task UpdateMatchStatuses(List<BiometricMatch> biometricMatches, NBiometricStatus status);
    }
}
