using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocumentDatabase.Storage;
using DocumentDatabase.Util;
using Examine;
using PropertyChanged;
using Syncfusion.Pdf;
using Syncfusion.Windows.Shared;
using WPFTagControl;

namespace DocumentDatabase.UI
{

    //[AddINotifyPropertyChangedInterface]
    public partial class WindowAddDocumentMulti : Window
    {

        [AddINotifyPropertyChangedInterface]
        public class PDFDocumentInternal
        {
            [OnChangedMethod(nameof(OnPdfChanged))]
            public PdfCreationInfo PdfCreationInfo { get; set; }
            public ObservableCollection<string> SelectedTags { get; set; } = new();

            public string InputFilename => System.IO.Path.GetFileName(PdfCreationInfo.InputFilePath);

            [OnChangedMethod(nameof(UpdateCanAdd))]
            public bool? OCRProcessState { get; set; } = false;

            [OnChangedMethod(nameof(UpdateCanAdd))]
            public string Title { get; set; }
#if DEBUG
                = "test";
#endif

            public bool CanAdd { get; set; }

            [OnChangedMethod(nameof(UpdateCanAdd))]
            public DateTime Date { get; set; } = DateTime.Today;

            [OnChangedMethod(nameof(UpdateCanAdd))]
            public bool IsAdded { get; set; } = false; // Don't allow to add twice


            void UpdateCanAdd()
            {
                CanAdd = !Title.IsNullOrWhiteSpace() && !IsAdded && OCRProcessState.HasValue && OCRProcessState.Value; //#TODO ability to re-add document which just edits the existing document
            }

            void OnPdfChanged()
            {
                OCRProcessState = null; // OCR is processing
                PdfCreationInfo?.OCRConversionTask.ContinueWith(x =>
                {
                    OCRProcessState = true; // OCR has finished
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }

        public bool CanAdd { get; set; } = false;

        public ObservableCollection<PDFDocumentInternal> Documents { get; set; } = new();
        public ObservableCollection<string> SuggestedTags { get; set; } = new();

        public WindowAddDocumentMulti(IEnumerable<PdfCreationInfo> pdfDocuments)
        {
            InitializeComponent();

            var db = DependencyInjection.GetService<DatabaseContext>();
            SuggestedTags = new ObservableCollection<string>(db.DocumentTags.Select(x => x.TagName));

            foreach (var pdfCreationInfo in pdfDocuments)
            {
                var x = new PDFDocumentInternal();
                x.PdfCreationInfo = pdfCreationInfo;

                Documents.Add(new PDFDocumentInternal { PdfCreationInfo = pdfCreationInfo });
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            foreach (var pdfCreationInfo in Documents)
            {
                pdfCreationInfo.PdfCreationInfo.OCRDocumentStream.Dispose();
            }
        }

        private void ButtonSubmit_OnClick(object sender, RoutedEventArgs e)
        {
            //#TODO Copy document to storage path

            var document = (e.Source as FrameworkElement)?.DataContext as PDFDocumentInternal;
            if (document == null) return;

            var docStorage = DependencyInjection.GetService<IDocumentStorage>();
            docStorage.AddFile(document.PdfCreationInfo.OCRDocumentStream, $"{document.PdfCreationInfo.UniqueName}.pdf");

            var db = DependencyInjection.GetService<DatabaseContext>();
            Debug.Assert(!document.PdfCreationInfo.BodyText.IsNullOrWhiteSpace());
            db.Documents.Add(new DocumentInfo
            {
                Id = document.PdfCreationInfo.UniqueName,
                Title = document.Title,
                Date = document.Date,
                BodyText = new CompressedDBString { Data = document.PdfCreationInfo.BodyText },
                Tags = new ObservableCollection<DocumentTag>(document.SelectedTags.Select(x => db.DocumentTags.AddIfNotExists(new DocumentTag { TagName = x }, y => y.TagName == x)))
                    
            });
            db.SaveChanges();
            
            
            var manager = DependencyInjection.GetService<IExamineManager>();
            var index = manager.Indexes.First();

            index.IndexItem(
                ValueSet.FromObject(document.PdfCreationInfo.UniqueName, "content",
                    new IndexDatabaseDocument {
                        DocumentTitle = document.Title,
                        DocumentDate = document.Date,
                        BodyText = document.PdfCreationInfo.BodyText
                    }
                )
            );

            document.IsAdded = true;

            //Close();
        }

        private void DocumentTitle_OnTextChanged(object sender, TextChangedEventArgs e)
        {

            var document = (e.Source as FrameworkElement)?.DataContext as PDFDocumentInternal;
            if (document == null) return;

            //document.Title

            //CanAdd = !DocumentTitle.Text.IsNullOrWhiteSpace() && (DocumentDate?.DateTime.HasValue ?? false);
            //document.CanAdd = !document.Title.IsNullOrWhiteSpace() && (document.Date.DocumentDate?.DateTime.HasValue ?? false);
        }

        private void DocumentDate_OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var document = (e.Source as FrameworkElement)?.DataContext as PDFDocumentInternal;
            //if (document == null) return;
            //
            //
            //document.CanAdd = !document.Title.IsNullOrWhiteSpace() && document.Date.DateTime.HasValue;
        }

        private void TagControl_OnTagAdded(object? sender, TagEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void ListDocuments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            var document = (e.AddedItems[0] as PDFDocumentInternal);
            if (document?.PdfCreationInfo.OCRDocumentStream != null)
                DocumentView.Load(document.PdfCreationInfo.OCRDocumentStream);
        }
    }
}
