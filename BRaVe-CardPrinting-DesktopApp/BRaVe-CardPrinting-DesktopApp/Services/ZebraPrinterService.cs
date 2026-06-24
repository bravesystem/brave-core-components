using System.Drawing;
using System.Drawing.Printing;

public class ZebraPrinterService
{
    private readonly string _printerName;

    public ZebraPrinterService(string printerName)
    {
        _printerName = printerName;
    }

    public void Print(Bitmap bitmap)
    {
        PrintDocument pd = new PrintDocument();
        pd.PrinterSettings.PrinterName = _printerName;

        // CR80 size 
        pd.DefaultPageSettings.PaperSize = new PaperSize("Card", 350, 220);
        pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        pd.PrintPage += (sender, e) =>
        {
            e.Graphics.DrawImage(bitmap, e.MarginBounds);
        };

        pd.Print();
    }
}