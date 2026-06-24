
using Antlr4.Runtime.Tree;
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{
    public class FailedJobGridRowDto
    {
        public Guid BatchId { get; set; }
        public string Mission { get; set; } = string.Empty;
        public DateTime SyncAttemptOn { get; set; }
        public string DeviceId { get; set; }
    }
}