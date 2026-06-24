using BRaVe_CardPrinting_DesktopApp.Models;
using System.Collections.Generic;


namespace BRaVe_CardPrinting_DesktopApp.Services
{
    public class TemplateService
    {
        public List<CardTemplate> GetTemplates()
        {
            return new List<CardTemplate>
        {
            Templates.StandardTemplate(),
            Templates.CompactTemplate()
        };
        }
    }
}
