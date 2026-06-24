using BRaVe_CardPrinting_DesktopApp.Models;
using System;

namespace BRaVe_CardPrinting_DesktopApp.Helpers
{
    public static class Mapper
    {
        public static CardData ToCardData(User user)
        {
            return new CardData
            {
                UserName = user.FullName,
                LocationInformation = user.LocationInformation,
                FamilySize = user.FamilySize,
                AdditionalInformation = user.AdditionalInformation,
                RegDate = user.RegDate,
                HouseholdId = user.HouseholdId,
                Photo = user.Photo,
                PrintedOn = DateTime.Now,
                barcode=user.barcodeId
            };
        }
    }
}