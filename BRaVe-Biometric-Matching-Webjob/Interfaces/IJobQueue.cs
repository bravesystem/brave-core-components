using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Interfaces
{

    public interface IJobQueue
    {
        /// <summary>Returns a jobId if available; otherwise null.</summary>
        Task<string> TryGetNextJobIdAsync(CancellationToken ct);

        /// <summary>Complete (delete) the current message after successful processing.</summary>
        Task CompleteAsync(CancellationToken ct);

        /// <summary>Abandon (unlock) the current message to retry later.</summary>
        Task AbandonAsync(CancellationToken ct);
    }

}
