extern alias syncPdfPortable;
using DocumentDatabase.UI;
using Syncfusion.OCRProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syncPdfPortable::Syncfusion.Pdf;
using System.Net;
using System.Net.Http;
using System.Threading;
using DocumentDatabase.BackgroundTasks;
using DocumentDatabase.Storage;

namespace DocumentDatabase.BackgroundTasks
{
    public class TaskPDF_OCR
    {
        interface ITaskInput
        {
            Stream GetStream();
        }

        private class TaskInputFile : ITaskInput
        {
            private string filePath;
            public TaskInputFile(string filePath)
            {
                this.filePath = filePath;
            }

            public Stream GetStream()
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
        }

        private class TaskInputStream : ITaskInput
        {
            public TaskInputStream(Stream inputStream)
            {
                throw new System.NotImplementedException();
            }

            public Stream GetStream()
            {
                throw new System.NotImplementedException();
            }
        }

        private ITaskInput input;

        public class ResultCommon
        {
            public string BodyText;
        }

        // Remember to close and dispose the document after done processing
        public class ResultDocument : ResultCommon
        {
            public syncPdfPortable::Syncfusion.Pdf.Parsing.PdfLoadedDocument ParsedDocument;
        }

        public class ResultStream : ResultCommon
        {
            public MemoryStream ParsedDocument;
        }

        public class Languages
        {
            public const string English = "eng";
            public const string Dutch = "nld";
            public const string German = "deu";
        }

        private string language = Languages.English;

        private TaskPDF_OCR(ITaskInput input)
        {
            this.input = input;
        }

        private async Task EnsureLanguagePresent(string language)
        {
            if (!File.Exists($"tessdata/{language}.traineddata"))
            {
                using var client = new HttpClient();
                await using var s = await client.GetStreamAsync($"https://github.com/tesseract-ocr/tessdata/raw/3.04.00/{language}.traineddata");
                await using var fs = new FileStream($"tessdata/{language}.traineddata", FileMode.OpenOrCreate);
                await s.CopyToAsync(fs);
            }
        }

        private async Task<(syncPdfPortable::Syncfusion.Pdf.Parsing.PdfLoadedDocument, Stream)> GenerateOCRDocument()
        {
            using OCRProcessor processor = new OCRProcessor(@"TesseractBinaries/Windows");

            //Load a PDF document
            Stream stream = input.GetStream();
            var document = new syncPdfPortable::Syncfusion.Pdf.Parsing.PdfLoadedDocument(stream);

            //Set OCR language
            processor.Settings.Language = language;
            processor.Settings.PageSegment = PageSegMode.AutoOsd; // autorotate

            var docStorage = DependencyInjection.GetService<IDocumentStorage>();
            var tempDirPath = docStorage.GetFileOpenablePath("tempdir"); //#TODO move this to a real temp thing, this may be network storage
            if (!System.IO.Path.EndsInDirectorySeparator(tempDirPath))
                tempDirPath += System.IO.Path.DirectorySeparatorChar;
            if (!System.IO.Directory.Exists(tempDirPath))
                System.IO.Directory.CreateDirectory(tempDirPath);

            processor.Settings.TempFolder = tempDirPath;


            await EnsureLanguagePresent(language);

            //Perform OCR with input document and tessdata (Language packs)
            processor.PerformOCR(document, @"tessdata\");

            return (document, stream);
        }

        //private async Task<TaskPDF_OCR.ResultDocument> RunInternal_GetDocument()
        //{
        //    var (document, stream) = await GenerateOCRDocument();
        //    string text = document.Pages.Cast<PdfPageBase>().Aggregate("", (current, documentPage) => current + documentPage.ExtractText());
        //    //stream.Dispose(); //#TODO we have to pass the input stream along, thats why this is commented out
        //    return new TaskPDF_OCR.ResultDocument
        //    {
        //        BodyText = text,
        //        ParsedDocument = document
        //        Stream = stream
        //    };
        //}


        private async Task<TaskPDF_OCR.ResultStream> RunInternal_GetStream()
        {
            var (document, stream) = await GenerateOCRDocument();
            string text = document.Pages.Cast<PdfPageBase>().Aggregate("", (current, documentPage) => current + documentPage.ExtractText());

            var result = new TaskPDF_OCR.ResultStream()
            {
                BodyText = text,
                ParsedDocument = new MemoryStream()
            };

            document.Save(result.ParsedDocument);
            result.ParsedDocument.Seek(0, SeekOrigin.Begin);
            document.Close(true);
            stream.Close();
            await stream.DisposeAsync();
            document.Dispose();

            return result;
        }


        public static TaskPDF_OCR FromFile(string path)
        {
            return new TaskPDF_OCR(new TaskInputFile(path));
        }

        public static TaskPDF_OCR FromStream(Stream stream)
        {
            return new TaskPDF_OCR(new TaskInputStream(stream));
        }
        
        // See TaskPDF_OCR.Languages
        public TaskPDF_OCR WithLanguage(string newLanguage)
        {
            language = newLanguage;
            return this;
        }

        // Remember to close and dispose the document after done processing
        //public Task<TaskPDF_OCR.ResultDocument> Run_GetDocument()
        //{
        //    return Task.Run(RunInternal_GetDocument);
        //}
        public Task<TaskPDF_OCR.ResultStream> Run_GetStream()
        {
            return Task.Run(RunInternal_GetStream);
        }
}
}
