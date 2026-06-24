using BRaVe_CardPrinting_DesktopApp.Models;
using BRaVe_CardPrinting_DesktopApp.Models.Request; 
using System.Collections.Generic;
using System.Linq;

namespace BRaVe_CardPrinting_DesktopApp.Helpers.Mappers
{
    public static class QueueMapper
    {
        public static List<User> ToUsers(List<CardPrintQueueItem> items)
        {
            return items.Select(x => new User
            {
                FullName = x.FullName,
                HouseholdId = x.HouseholdId,
                Program = x.Program,
                Activity = x.Activity,
                LocationInformation = x.LocationInformation,
                AdditionalInformation=x.AdditionalInformation,
                FamilySize = x.FamilySize,
                RegDate = x.RegDate,
                CardPrinted = x.CardPrinted,
                IsLocked = x.IsLocked,
                LockedBy = x.LockedBy,
                barcodeId=x.barcodeId
            }).ToList();
        }
    }
}