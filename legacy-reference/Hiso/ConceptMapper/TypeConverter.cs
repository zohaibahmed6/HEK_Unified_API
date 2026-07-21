using Aspose.Words;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Hiso.ConceptMapper
{
    public static class TypeConverter
    {
        public static byte[] CreatePDFromImage(byte[] recievedBytes)
        {
            Aspose.Words.Document doc = new Aspose.Words.Document();
            try
            {
                DocumentBuilder documentBuilder = new DocumentBuilder(doc);
                documentBuilder.MoveToDocumentEnd();
                documentBuilder.InsertImage(recievedBytes);
                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms, Aspose.Words.SaveFormat.Pdf);
                    Logger.Logging.Instance.WriteEventLog("Successfully Converted Image");
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception occurred in MHN.Utilities.InboxHelpers.PDFOperations.CreatePDFromImage()" + ex.Message);
            }
        }

        

        public static bool IsValidXML(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                return true;
            }
            catch (XmlException e)
            {
                return false;
            }
        }

        public static byte[] ConvertHTMLToByte(byte[] FileBytes)
        {
            byte[] test = FileBytes;
            try
            {
                using (MemoryStream stream = new MemoryStream(FileBytes))
                {
                    // load HTML file
                    try
                    {
                        Aspose.Words.LoadOptions loadOptions = new Aspose.Words.LoadOptions();
                        loadOptions.LoadFormat = LoadFormat.Unknown;
                        Logger.Logging.Instance.WriteEventLog("Aspose Enter");
                        Aspose.Words.Document doc = new Aspose.Words.Document(stream);
                        Logger.Logging.Instance.WriteEventLog("Aspose Exit");
                        foreach (Aspose.Words.Section section in doc)
                        {
                            section.PageSetup.PaperSize = PaperSize.Letter;
                            section.PageSetup.RightMargin = 20;
                            section.PageSetup.LeftMargin = 10;
                            section.PageSetup.TopMargin = 10;
                            section.PageSetup.BottomMargin = 10;
                        }


                        // save output PDF into file/stream
                        using (MemoryStream ms = new MemoryStream())
                        {

                            doc.Save(ms, Aspose.Words.SaveFormat.Pdf);
                            Logger.Logging.Instance.WriteEventLog("Successfully Converted HTML");
                            return ms.ToArray();
                        }


                        //Guid GUID = Guid.NewGuid();
                        //string filePath = ConfigurationManager.AppSettings["LogRoot"] + "\\" + GUID.ToString() + ".pdf";
                        //doc.Save(filePath, Aspose.Words.SaveFormat.Pdf);
                        ////doc.Save(ms, Aspose.Words.SaveFormat.Pdf);
                        //test = File.ReadAllBytes(filePath);
                        ////File.Delete(filePath);


                    }
                    catch (Exception ex)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(ex);
                        return test;
                    }
                    
                   
                }
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                return test;
                //throw new Exception("Exception occurred in MHN.Utilities.InboxHelpers.PDFOperations.ConvertHTMLToByte()" + ex.Message);
            }
        }
    }
}