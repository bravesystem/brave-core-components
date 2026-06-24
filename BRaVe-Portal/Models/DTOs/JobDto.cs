
using Antlr4.Runtime.Tree;
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{
    public enum JobStatus
    {
        Success,
        Failed,
        Pending
    }

    public class JobDto
    {
        public Guid JobId { get; set; }
        public DateTime TransferredOn { get; set; }
        public Guid DeviceId { get; set; }
        public string Enumerator { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
    }
}