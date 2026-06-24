using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_CardPrinting_DesktopApp.Models.DTOs
{
    public class SyncPrintedRequest
    {
        public List<string> HouseholdIds { get; set; }
        public string DeviceId { get; set; }
        public string WindowsUser { get; set; }
    }
}
