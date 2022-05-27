using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using Examine;
using PropertyChanged;
using Syncfusion.Windows.Shared;
using WPFTagControl;

namespace DocumentDatabase.UI
{
    public class PdfCreationInfo
    {
        public string DocumentPath { get; set; }
        public string BodyText { get; set; }
        public string UniqueName { get; set; }
    }


    class IndexDatabaseDocument
    {
        public string DocumentTitle { get; set; }
        public DateTime DocumentDate { get; set; }
        public string BodyText { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
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
            DocumentView.Load(_pdfCreationInfo.DocumentPath);

            var db = DependencyInjection.GetService<DatabaseContext>();
            TagControl.SuggestedTags = db.DocumentTags.Select(x => x.TagName).ToList();
        }

        private void ButtonSubmit_OnClick(object sender, RoutedEventArgs e)
        {
            //#TODO Copy document to storage path

            var docStorage = DependencyInjection.GetService<IDocumentStorage>();
            docStorage.AddFile(_pdfCreationInfo.DocumentPath);

            var db = DependencyInjection.GetService<DatabaseContext>();
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
