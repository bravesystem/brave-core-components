using System;
using System.Drawing;


namespace BRaVe_CardPrinting_DesktopApp.Models
{
    public class CardTemplate
    {
        public string Name { get; set; }
        public string BackgroundPath { get; set; }
        public Action<Graphics, CardData> RenderAction { get; set; }
    }
}
