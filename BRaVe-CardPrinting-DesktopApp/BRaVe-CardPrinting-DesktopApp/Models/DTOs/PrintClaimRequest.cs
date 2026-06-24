using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_CardPrinting_DesktopApp.Models.Request
{
    public class PrintClaimRequest
    {
        public string SessionCode { get; set; } = "";
        public string DeviceId { get; set; } = "";
    }
}
