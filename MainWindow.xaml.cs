extern alias syncPdfPortable;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocumentDatabase.Storage;
using DocumentDatabase.UI;
using Examine;
using Microsoft.EntityFrameworkCore;
using PropertyChanged;
using Syncfusion.OCRProcessor;
using WPFTagControl;

namespace DocumentDatabase
{


    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public DatabaseContext db { get; set; }

        public MainWindow()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Encoding.UTF8.GetString(Convert.FromBase64String("TmpReU16RTNRRE15TXpBeVpUTXhNbVV6TUVZcmFHSklXVmsyVFZabFpGbHVWbmsxTjNaWllXUXlUWGxJVUhWTGRXZGpTbU5GVmxVM1RtNVpRakE5"))); // https://www.serkanseker.com/syncfusion-community-license-key/

            db = DependencyInjection.GetService<DatabaseContext>();
            var manager = DependencyInjection.GetService<IExamineManager>();
            var index = manager.Indexes.First();
            foreach (var documentInfo in db.Documents.Include(x => x.Tags))
            {
                index.IndexItem(
                    ValueSet.FromObject(documentInfo.Id, "content", new IndexDatabaseDocument
                        {
                            DocumentTitle = documentInfo.Title,
                            DocumentDate = documentInfo.Date,
                            BodyText = documentInfo.BodyText.Data
                        }
                    )
                );
            }

            DocumentCollection = db.Documents.Local.ToObservableCollection();

            //https://shazwazza.github.io/Examine/searching
            //https://lucenenet.apache.org/docs/4.8.0-beta00016/api/memory/Lucene.Net.Index.Memory.html
            //https://lucenenet.apache.org/docs/4.8.0-beta00016/api/classification/Lucene.Net.Classification.html maybe interesting to find similar documents
            //https://lucenenet.apache.org/docs/4.8.0-beta00016/api/analysis-common/Lucene.Net.Analysis.De.html
            //
            //https://www.nuget.org/packages/Syncfusion.PDF.OCR.Net.Core/
            //
            //https://www.nuget.org/packages/Syncfusion.PDF.OCR.WPF/ ?
            //Syncfusion.PdfViewer.WPF
            //https://www.syncfusion.com/sales/communitylicense
            InitializeComponent();
        }

        public ObservableCollection<DocumentInfo> DocumentCollection { get; set; }

        public ObservableCollection<string> SelectedTags { get; set; } = new();
        public ObservableCollection<string> SuggestedTags { get; set; } = new();

        private void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                foreach (var file in files)
                {
                    Task.Run(() => HandleFileOpen(file)).ContinueWith(x =>
                    {
                        if (x.Result == null)
                            return;
                        var newWindow = new WindowAddDocument(x.Result);
                        newWindow.Show();
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
        private async Task<PdfCreationInfo> HandleFileOpen(string file)
        {
            //Initialize the OCR processor with tesseract binaries folder path
            using (OCRProcessor processor = new OCRProcessor(@"TesseractBinaries/Windows"))
            {
                var documentUniqueName = Guid.NewGuid().ToString("N");

                //Load a PDF document
                using FileStream stream = new FileStream(file, FileMode.Open);
                var document = new syncPdfPortable::Syncfusion.Pdf.Parsing.PdfLoadedDocument(stream);
                
                //Set OCR language
                processor.Settings.Language = Languages.English;
                processor.Settings.Language = "nld";
                processor.Settings.PageSegment = PageSegMode.AutoOsd; // autorotate
                processor.Settings.TempFolder = "P:/";

                //Perform OCR with input document and tessdata (Language packs)
                processor.PerformOCR(document, @"tessdata\");
                
                //Save the document into stream.
                {
                    using FileStream outStream = new FileStream($"P:/{documentUniqueName}.pdf", FileMode.CreateNew);
                    document.Save(outStream);
                }

                string text = "";
                
                foreach (syncPdfPortable::Syncfusion.Pdf.PdfPageBase documentPage in document.Pages)
                {
                    text += documentPage.ExtractText();
                }

                //Close the document. 
                document.Close(true);

                return new PdfCreationInfo
                {
                    UniqueName = documentUniqueName,
                    DocumentPath = $"P:/{documentUniqueName}.pdf",
                    BodyText = text
                };
            }
        }
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var x = e.AddedItems[0] as DocumentInfo;
            var db = DependencyInjection.GetService<DatabaseContext>();

            // for this code to work, needs b.Documents.Include(x => x.Tags) on first documents load, should use lazy loading //#TODO

            TagControl.SuggestedTags = db.DocumentTags.Select(x => x.TagName).ToList();
            TagControl.SelectedTags = x.Tags.Select(x => x.TagName).ToList();
            //#TODO below stuff should work, but doesn't update
            //SuggestedTags.Clear();
            //SuggestedTags.AddRange(db.DocumentTags.Select(x => x.TagName));
            //SelectedTags.Clear();
            //SelectedTags.AddRange(x.Tags.Select(x => x.TagName));
            var docStorage = DependencyInjection.GetService<IDocumentStorage>();
            DocumentView.Load(docStorage.GetFileOpenablePath(x.Id)); //#TODO document stream instead https://help.syncfusion.com/wpf/pdf-viewer/getting-started

            if (x.SearchHit)
                DocumentView.SearchText(searchBox.Text);
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {


                var manager = DependencyInjection.GetService<IExamineManager>();
                var index = manager.Indexes.First();
                var results = index.Searcher.Search(searchBox.Text);

                var id = results.First().Id;
                var res = results.First();
                var res2 = results.First().GetValues("nodeName").ToArray();

                foreach (var searchResult in results)
                {
                    var found = DocumentCollection.FirstOrDefault(x => x.Id == searchResult.Id);
                    if (found != null)
                    {
                        found.SearchHit = true;
                    }
                }


            }
        }

        private void TagControl_OnTagAdded(object? sender, TagEventArgs e)
        {
            var x = DocumentsList.SelectedItems[0] as DocumentInfo;
            var db = DependencyInjection.GetService<DatabaseContext>();
            x.Tags.Add(db.DocumentTags.AddIfNotExists(new DocumentTag { TagName = e.Item.Text }, y => y.TagName == e.Item.Text));
            db.SaveChanges();
        }
    }
}
