﻿using DevExpress.Pdf;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using System.Drawing;

namespace PdfConversionTestDemo;

 public static class ConversionService
 {
    //Convert PDF/A to PDF 1.4
    public static  void converPdfType()
    {
        using (PdfDocumentProcessor target = new PdfDocumentProcessor())//pdf1.4 to pdfa
        {  
            target.CreateEmptyDocument("./docs/converPdfType.pdf", new PdfCreationOptions()
            {
                Compatibility = PdfCompatibility.PdfA1b,
                MergePdfADocuments=true
            });
            target.AppendDocument("./docs/Sample_PDF_type_14.pdf");
        }
    }


    //secure a pdf
    public static  void SecurePdf()
    {

        using (PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor())
        {
            // Load a PDF document.
            pdfDocumentProcessor.LoadDocument("./docs/converPdfType_PdfA1b.pdf");

            // Specify printing, data extraction, modification, and interactivity permissions. 
            PdfEncryptionOptions encryptionOptions = new PdfEncryptionOptions();

            encryptionOptions.PrintingPermissions = PdfDocumentPrintingPermissions.NotAllowed;
            encryptionOptions.DataExtractionPermissions = PdfDocumentDataExtractionPermissions.NotAllowed;
            encryptionOptions.ModificationPermissions = PdfDocumentModificationPermissions.NotAllowed;
            encryptionOptions.InteractivityPermissions = PdfDocumentInteractivityPermissions.NotAllowed;


            // Specify the owner and user passwords for the document.  
            encryptionOptions.OwnerPasswordString = "OwnerPassword";
           // encryptionOptions.UserPasswordString = "up";

            // Specify the 256-bit AES encryption algorithm.
            encryptionOptions.Algorithm = PdfEncryptionAlgorithm.AES256;

            // Save the protected document with encryption settings.  
            pdfDocumentProcessor.SaveDocument("./docs/Result_SecurePdf.pdf", new PdfSaveOptions() { EncryptionOptions = encryptionOptions });

            bool allowPrinting = pdfDocumentProcessor.Document.AllowPrinting;
            Console.WriteLine("AllowPrinting:" + allowPrinting+ "   AllowDocumentAssembling:"+pdfDocumentProcessor.Document.AllowDocumentAssembling
               + "   AllowModifying:" + pdfDocumentProcessor.Document.AllowModifying);
        }
    }


    //combine multiple pdf into 1
    public static void MergedToOnePdf()
    {
        using (PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor())
        {
            pdfDocumentProcessor.CreateEmptyDocument("./docs/Result_MergedToOnePdf.pdf");
            pdfDocumentProcessor.AppendDocument("./docs/TextMerge1.pdf");
            pdfDocumentProcessor.AppendDocument("./docs/TextMerge2.pdf");
        }
    }



    //add background image
    // img position:x+y+width+height
    public static void AddBgToPdf()
    {
        using var newDocument = new PdfDocumentProcessor();
        newDocument.CreateEmptyDocument();

        using var foregroundDocument = new PdfDocumentProcessor();
        foregroundDocument.LoadDocument("./docs/pdf_test.pdf");

        for (var i = 0; i < foregroundDocument.Document.Pages.Count; i++)
        {
            using var graphics = newDocument.CreateGraphics();

            newDocument.AddNewPage(PdfPaperSize.A4);

            using (Bitmap image = new Bitmap("./docs/bg.png"))
            {
                float width = image.Width;
                float height = image.Height;
                graphics.DrawImage(image, new RectangleF(100, 100, width / 2, height / 2));
            }
            graphics.AddToPageBackground(newDocument.Document.Pages[i]);

           graphics.DrawPageContent(foregroundDocument.Document.Pages[i]);
           graphics.AddToPageForeground(newDocument.Document.Pages[i]);
        }

        newDocument.SaveDocument("./docs/Result_AddBgToPdf.pdf"); 

    }


    public static void ConvertRTFToPdf() 
    {
        RichEditDocumentServer richServer = new RichEditDocumentServer();
        richServer.LoadDocument("./docs/sample_rtf_file_for_testing.rtf");
        richServer.ExportToPdf("./docs/Result_ConvertRTFToPdf.pdf");
    }

    public static void ConvertTiffToPdf()
    {
        //load image
        Image image = Image.FromFile("./docs/R.tiff");

        ////set image size
        //int width = image.Width;
        //int height = image.Height;
        //float scale = 0.3f;  //zoom setting
        //Size size = new Size((int)(width * scale), (int)(height * scale));
        //Bitmap scaledImage = new Bitmap(image, size);

        using (RichEditDocumentServer server = new RichEditDocumentServer())
        {
           DocumentImage docImage = server.Document.Images.Append(DocumentImageSource.FromImage(image));
            //server.Document.Unit = DevExpress.Office.DocumentUnit.Millimeter;
            //server.Document.Sections[0].Margins.Right = 0;
            //server.Document.Sections[0].Margins.Left = 0;
            //server.Document.Sections[0].Margins.Top = 0;
            //server.Document.Sections[0].Margins.Bottom = 0;
            //docImage.ScaleX = 210 / (docImage.Size.Width + server.Document.Sections[0].Margins.Right + server.Document.Sections[0].Margins.Left);
            //docImage.ScaleY = 297 /(docImage.Size.Height + server.Document.Sections[0].Margins.Top + server.Document.Sections[0].Margins.Bottom);

            server.Document.Sections[0].Page.Width = docImage.Size.Width + server.Document.Sections[0].Margins.Right + server.Document.Sections[0].Margins.Left;
            server.Document.Sections[0].Page.Height = docImage.Size.Height + server.Document.Sections[0].Margins.Top + server.Document.Sections[0].Margins.Bottom;


          //  server.Document.Sections[0].Page.Width = 210;//A4 size
          //  server.Document.Sections[0].Page.Height = 297;
            using (System.IO.FileStream fs = new System.IO.FileStream("./docs/Result_Tiff.pdf", System.IO.FileMode.OpenOrCreate))
            {
                server.ExportToPdf(fs);
            }
        }

    }

    public static void ConvertBase64ToPdf()
    {
       // string pdflocation = "D:\\";
        string fileName = "./docs/Result_softcore.pdf";

        // put your base64 string converted from online platform here instead of V
        //https://freeformatter.com/base64-encoder.html online platform


        int mod4 = base64String.Length % 4;

        // as of my research this mod4 will be greater than 0 if the base 64 string is corrupted
        if (mod4 > 0)
        {
            base64String += new string('=', 4 - mod4);
        }
       // pdflocation = pdflocation + fileName;

        byte[] data = Convert.FromBase64String(base64String);

        using (FileStream stream = System.IO.File.Create(fileName))
        {
            stream.Write(data, 0, data.Length);
        }
    }
    public static void ConvertPdfToBase64()
    {
        string path = "./docs/pdf_test.pdf";
        byte[] document = File.ReadAllBytes(path);
        string base64string = Convert.ToBase64String(document);//base64

        var isoBytes = Convert.FromBase64String(base64string);
         File.WriteAllBytes("./docs/sketch.pdf", isoBytes);
    }

    //Convert rtf To Base64 and then convert Base64 To rtf back
    public static void ConvertRtfToBase64() 
    {
        string path = "./docs/sample_rtf_file_for_testing.rtf";
        byte[] document = File.ReadAllBytes(path);
        string base64string = Convert.ToBase64String(document);

        var isoBytes = Convert.FromBase64String(base64string);
        File.WriteAllBytes("./docs/sketch.rtf", isoBytes);
    }


    //Convert Tiff To Base64 and then convert Base64 To Tiff back
    public static void ConvertTiffToBase64()
    {
        string path = "./docs/R.tiff";
        byte[] document = File.ReadAllBytes(path);
        string base64string = Convert.ToBase64String(document);

        var isoBytes = Convert.FromBase64String(base64string);
        File.WriteAllBytes("./docs/sketch.tiff", isoBytes);
    }

}
