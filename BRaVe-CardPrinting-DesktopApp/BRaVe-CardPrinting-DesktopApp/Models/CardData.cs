using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_CardPrinting_DesktopApp.Models
{
    public class CardData
    {
        public string UserName { get; set; }
        public string LocationInformation { get; set; }

        public int FamilySize { get; set; }

        public DateTime RegDate { get; set; }

        public string? AdditionalInformation { get; set; }
        public string HouseholdId { get; set; }

        public string? barcode { get; set; }
        public byte[]? Photo { get; set; }
        public DateTime PrintedOn { get; set; }
    }
}
