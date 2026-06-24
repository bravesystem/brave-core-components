using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace BRaVe_CardPrinting_DesktopApp.Models
{
    public static class Templates
    {
        public static CardTemplate StandardTemplate()
        {
            return new CardTemplate
            {
                Name = "Standard Card",
                BackgroundPath = Path.Combine(Application.StartupPath, "Resources", "card_template.png"),

                RenderAction = (g, data) =>
                {
                    var fontHeader = new Font("Segoe UI", 14, FontStyle.Bold);
                    var fontSub = new Font("Segoe UI", 10, FontStyle.Bold);
                    var fontName = new Font("Segoe UI", 12, FontStyle.Bold);
                    var fontSmall = new Font("Segoe UI", 10, FontStyle.Italic);
                    var fontBig = new Font("Segoe UI", 16, FontStyle.Bold);

                    Brush brush = Brushes.Black;

                   
                    g.DrawString(data.LocationInformation ?? "", fontHeader, brush, 40, 40);
                    g.DrawString(data.RegDate.ToString("dd MMM yyyy"), fontSub, brush, 40, 120);

                    int y = 200;
                    var nameParts = (data.UserName ?? "").Split(' ');

                    foreach (var part in nameParts)
                    {
                        g.DrawString(part, fontName, brush, 40, y);
                        y += 55;
                    }



                    g.DrawString(data.FamilySize.ToString() ?? "", fontBig, brush, 40, 360);
                    g.DrawString(data.AdditionalInformation ?? "", fontSub, brush, 300, 390);
                    g.DrawString($"printed on {DateTime.Now:dd/MMMM/yyyy}", fontSmall, brush, 300, 460);

                    // BARCODE 
                    var writer = new BarcodeWriter
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new EncodingOptions
                        {
                            Width = 120,
                            Height = 400,
                            Margin = 2,
                            PureBarcode=true
                        }
                    };

                    using (var barcodeBitmap = writer.Write(data.barcode ?? "0000000"))
                    {
                        barcodeBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                        g.DrawImage(barcodeBitmap, new Rectangle(850, 40, 120, 500));
                    }
                }
            };
        }

        public static CardTemplate CompactTemplate()
        {
            return new CardTemplate
            {
                Name = "Compact Card",
                BackgroundPath = "Resources/card_template.png",
                RenderAction = (g, data) =>
                {
                    var font = new Font("Arial", 10, FontStyle.Bold);

                    g.DrawString(data.UserName, font, Brushes.Black, 20, 20);
                    g.DrawString(data.HouseholdId, font, Brushes.Black, 20, 60);
                }
            };
        }
    }
}
