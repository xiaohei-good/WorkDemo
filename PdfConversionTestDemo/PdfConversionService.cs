
using DevExpress.Pdf;
using DevExpress.XtraPrinting;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using System.Drawing;

namespace PdfConversionTestDemo;

public static class PdfConversionService
{
    //public Stream AddBackground(Stream imageStream, Stream pdfStream)
    //{
    //    //TODO: Implement this method
    //    throw new NotImplementedException();
    //}
    public static string GetPdfStr()
    {
        using (PdfDocumentProcessor processor = new PdfDocumentProcessor())
        { processor.LoadDocument("./docs/Sample_PDF_type_14.pdf");
            using var exportMemoryStream = new MemoryStream();
            processor.SaveDocument(exportMemoryStream);
            string pdfbase64str = Base64Helper.ToBase64String(exportMemoryStream);
            return pdfbase64str;
        }
        }
    public static string ConvertFromRtf(Stream rtfStream, string pdfCompatibility, AllowArgs? allowArgs)
    {
       
        using var processor = new RichEditDocumentServer();
        processor.LoadDocument(rtfStream);
        var options = new PdfExportOptions
        {
            DocumentOptions =
            {
                Author = string.Join(',', processor.Document.GetAuthors())
            },
            Compressed = false,
            ImageQuality = PdfJpegImageQuality.Highest,
        };

        using var exportMemoryStream = new MemoryStream();
        processor.ExportToPdf(exportMemoryStream, options);

       // processor.SaveDocument("./docs/temp.pdf");
       // processor.CloseDocument();
        //pdfDocumentProcessor.SaveDocument("./docs/temp.pdf");
        //  return newMemoryStream;
        string pdfbase64str = Base64Helper.ToBase64String(exportMemoryStream);
        return pdfbase64str;
    }

    public static Stream ConvertFromTiff(Stream TiffStream, string pdfCompatibility, AllowArgs? allowArgs)
    {
        Image image = Image.FromStream(TiffStream);
        using (RichEditDocumentServer server = new RichEditDocumentServer())
        {
            DocumentImage docImage = server.Document.Images.Append(DocumentImageSource.FromImage(image));
            server.Document.Sections[0].Page.Width = docImage.Size.Width + server.Document.Sections[0].Margins.Right + server.Document.Sections[0].Margins.Left;
            server.Document.Sections[0].Page.Height = docImage.Size.Height + server.Document.Sections[0].Margins.Top + server.Document.Sections[0].Margins.Bottom;
            using var exportMemoryStream = new MemoryStream();
            server.ExportToPdf(exportMemoryStream);


            string pdfbase64str=Base64Helper.ToBase64String(exportMemoryStream);
            var isoBytesw = Convert.FromBase64String(pdfbase64str);
            File.WriteAllBytes("./docs/temptiff.pdf", isoBytesw);//save pdf for test

            return exportMemoryStream;

        }
       
    }

    public static Stream CombinePdfFiles(List<Stream> pdfStreams)
    {
        using (PdfDocumentProcessor processor = new PdfDocumentProcessor())
        {
            var exportMemoryStream = new MemoryStream();
           // processor.CreateEmptyDocument(exportMemoryStream);
            processor.CreateEmptyDocument();
            foreach (var pdfStream in pdfStreams)
            { 
                processor.AppendDocument(pdfStream);
                
            }
          
            processor.SaveDocument(exportMemoryStream);

            string pdfbase64str = Base64Helper.ToBase64String(exportMemoryStream);
            var isoBytesw = Convert.FromBase64String(pdfbase64str);
            File.WriteAllBytes("./docs/CombinePdfFiles.pdf", isoBytesw);//save pdf for test

            return exportMemoryStream;
        }
    }
}