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
using DocumentDatabase.BackgroundTasks;
using DocumentDatabase.Storage;
using DocumentDatabase.UI;
using Examine;
using Lucene.Net.Linq.Util;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using PropertyChanged;
using Syncfusion.OCRProcessor;
using WPFTagControl;

namespace DocumentDatabase
{


    //[AddINotifyPropertyChangedInterface]
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
            if (sender is not MainWindow) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (e.Data.GetData(DataFormats.FileDrop) as string[]) ?? Array.Empty<string>();

                if (files.Length > 1)
                {
                    //#TODO filter pdf only
                    var results = files.Select(file => Task.Run(() => HandleFileOpen(file)));
                    Task.WhenAll(results).ContinueWith(x =>
                    {
                        var newWindow = new WindowAddDocumentMulti(x.Result);
                        newWindow.Show();
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    foreach (var file in files)
                    {
                        Task.Run(() => HandleFileOpen(file)).ContinueWith(async x =>
                        {
                            await x.Result.OCRConversionTask; // Wait till OCR is done before we open
                            //#TODO handle it inside the window as part of the CanAdd
                            var newWindow = new WindowAddDocument(x.Result);
                            newWindow.Show();
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }
              
            }
        }
        private async Task<PdfCreationInfo> HandleFileOpen(string file)
        {
            var documentUniqueName = Guid.NewGuid().ToString("N");

            var result = new PdfCreationInfo
            {
                InputFilePath = file,
                UniqueName = documentUniqueName
            };

            //#TODO let user set language
            var conversionTask = TaskPDF_OCR.FromFile(file).WithLanguage(TaskPDF_OCR.Languages.Dutch).Run_GetStream().ContinueWith(x =>
            {
                {
                    result.BodyText = x.Result.BodyText;
                    result.OCRDocumentStream = x.Result.ParsedDocument; // Taking ownership of the stream
                    //#TODO we need to handle the WindowAddDocument closing before OCR is done and passes the stream
                }
            });

            result.OCRConversionTask = conversionTask;
            return result;

        }
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems.Count == 0) return;

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
            DocumentView.Load(docStorage.GetFileOpenablePath($"{x.Id}.pdf")); //#TODO document stream instead https://help.syncfusion.com/wpf/pdf-viewer/getting-started

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

                //var id = results.FirstOrDefault()?.Id;
                //var res = results.First();
                //var res2 = results.First().GetValues("nodeName").ToArray();

                DocumentCollection.Apply(x => x.SearchHit = false);

                foreach (var searchResult in results)
                {
                    var found = DocumentCollection.FirstOrDefault(x => x.Id == searchResult.Id);
                    if (found != null)
                    {
                        found.SearchHit = true;
                    }
                }

                DocumentCollection.Sort((x,y) => y.SearchHit.CompareTo(x.SearchHit));

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

  //  https://developpaper.com/using-lucene-net-to-do-a-simple-search-engine-full-text-index/
}
