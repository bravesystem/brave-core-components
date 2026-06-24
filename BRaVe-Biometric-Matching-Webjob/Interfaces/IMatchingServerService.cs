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

    public interface IMatchingServerService
    {
        Task<bool> IsHealthyAsync(CancellationToken ct);

        Task PerformTask(ServerTask serverTask);

        Task ClearTask(ServerTask serverTask);

        Task<NBiometricTask.SubjectCollection> GetSubjects(ServerTask serverTask);

        Task<NSubject> CreateSubject(Biometric biometric);

        Task AddSubject(ServerTask serverTask, NSubject subject);

        Exception getTaskError(ServerTask serverTask);

        int GetMinScore();

        Task clearTask(ServerTask serverTask);

        Task<NBiometricStatus> GetStatus(ServerTask serverTask);


    }

}
