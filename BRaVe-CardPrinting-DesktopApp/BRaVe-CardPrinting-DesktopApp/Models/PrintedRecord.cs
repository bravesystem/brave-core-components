using System;

namespace BRaVe_CardPrinting_DesktopApp.Models
{
    public class PrintedRecord
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string HouseholdId { get; set; }
        public DateTime PrintedOn { get; set; }
        public string TemplateUsed { get; set; }
        public string PrinterName { get; set; }

        public DateTime? SyncTime { get; set; }
    }
}