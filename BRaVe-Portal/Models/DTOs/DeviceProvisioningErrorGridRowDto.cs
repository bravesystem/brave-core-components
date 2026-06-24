
using Antlr4.Runtime.Tree;
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{
    public class DeviceProvisioningErrorGridRowDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string CurrentTenantName { get; set; } = string.Empty;
        public int CurrentTenantId { get; set; }
        public string OldTenantName { get; set; } = string.Empty;
        public int OldTenantId { get; set; }
        public DateTime? FirstAttempt { get; set; }
        public DateTime? LastAttempt { get; set; }
        public int? AttemptCount { get; set; }
    }
}