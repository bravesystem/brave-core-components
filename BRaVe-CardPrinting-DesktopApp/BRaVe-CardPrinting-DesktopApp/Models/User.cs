using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_CardPrinting_DesktopApp.Models
{
    public class User
    {
       
        public string FullName { get; set; }
        public int FamilySize { get; set; }

        public string HouseholdId { get; set; }

        public string Mission { get; set; }

        public string Program { get; set; }
        public string LocationInformation { get; set; }
        public string? AdditionalInformation { get; set; }
     

        public string Activity { get; set; }
        public byte[]? Photo { get; set; }
        public string? barcodeId { get; set; }

        public DateTime RegDate { get; set; }

        public bool CardPrinted { get; set; }
        public DateTime? PrintedOn { get; set; }

        public bool IsLocked { get; set; }
        public string LockedBy { get; set; }
    }
}


