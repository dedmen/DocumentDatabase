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
using Syncfusion.Windows.Shared;
using WPFTagControl;

namespace DocumentDatabase.UI
{
    public class PdfCreationInfo
    {
        public string InputFilePath { get; set; }
        //public string DocumentPath { get; set; }
        public string BodyText { get; set; }
        public string UniqueName { get; set; }
        public Task OCRConversionTask { get; set; }
        public Stream OCRDocumentStream { get; set; }
    }


    class IndexDatabaseDocument
    {
        public string DocumentTitle { get; set; }
        public DateTime DocumentDate { get; set; }
        public string BodyText { get; set; }
    }

    //[AddINotifyPropertyChangedInterface]
    public partial class WindowAddDocument : Window
    {
        private readonly PdfCreationInfo _pdfCreationInfo;
        
        public bool CanAdd { get; set; } = false;

        public ObservableCollection<string> SelectedTags { get; set; } = new();

        public WindowAddDocument(PdfCreationInfo pdfDocumentPath)
        {
            _pdfCreationInfo = pdfDocumentPath;
            InitializeComponent();

            Debug.Assert(_pdfCreationInfo != null, nameof(_pdfCreationInfo) + " != null");
            DocumentView.Load(_pdfCreationInfo.OCRDocumentStream);

            var db = DependencyInjection.GetService<DatabaseContext>();
            TagControl.SuggestedTags = db.DocumentTags.Select(x => x.TagName).ToList();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _pdfCreationInfo.OCRDocumentStream.Dispose();
        }

        private void ButtonSubmit_OnClick(object sender, RoutedEventArgs e)
        {
            //#TODO Copy document to storage path

            var docStorage = DependencyInjection.GetService<IDocumentStorage>();
            docStorage.AddFile(_pdfCreationInfo.OCRDocumentStream, $"{_pdfCreationInfo.UniqueName}.pdf");

            //#TODO this is mostly duplicated in WindowAddDocumentMulti, fix that

            var db = DependencyInjection.GetService<DatabaseContext>();
            Debug.Assert(!_pdfCreationInfo.BodyText.IsNullOrWhiteSpace());
            db.Documents.Add(new DocumentInfo
            {
                Id = _pdfCreationInfo.UniqueName,
                Title = DocumentTitle.Text,
                Date = DocumentDate.DateTime.Value,
                BodyText = new CompressedDBString { Data = _pdfCreationInfo.BodyText },
                Tags = new ObservableCollection<DocumentTag>(TagControl.SelectedTags.Select(x => db.DocumentTags.AddIfNotExists(new DocumentTag { TagName = x }, y => y.TagName == x)))
                    
            });
            db.SaveChanges();


            var manager = DependencyInjection.GetService<IExamineManager>();
            var index = manager.Indexes.First();

            index.IndexItem(
                ValueSet.FromObject(_pdfCreationInfo.UniqueName, "content",
                    new IndexDatabaseDocument {
                        DocumentTitle = DocumentTitle.Text,
                        DocumentDate = DocumentDate.DateTime.Value,
                        BodyText = _pdfCreationInfo.BodyText
                    }
                )
            );

            Close();
        }

        private void DocumentTitle_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CanAdd = !DocumentTitle.Text.IsNullOrWhiteSpace() && (DocumentDate?.DateTime.HasValue ?? false);
        }

        private void DocumentDate_OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CanAdd = !DocumentTitle.Text.IsNullOrWhiteSpace() && DocumentDate.DateTime.HasValue;
        }

        private void TagControl_OnTagAdded(object? sender, TagEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
