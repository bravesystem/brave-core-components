using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_CardPrinting_DesktopApp.Models.Request
{
    public class RefreshResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
