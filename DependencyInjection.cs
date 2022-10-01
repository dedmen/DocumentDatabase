using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DocumentDatabase.Storage;
using Examine;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DocumentDatabase
{
    public static class DependencyInjection
    {
        private static ServiceCollection _serviceCollection;
        static DependencyInjection()
        {
            
            _serviceCollection = new ServiceCollection();


            MessageBox.Show("Hello! I'm Jeff. Please select the storage directory where I should store my Database or... Jeffbase if you're into that shizz.");
            
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\";
            dialog.Title = "Jeffbase location :cool_emojithing:";
            dialog.EnsurePathExists = true;
            dialog.ShowPlacesList = true;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _serviceCollection.AddSingleton<IDocumentStorage>(new DocumentStorageLocal(dialog.FileName));
            }
            else
            {
                MessageBox.Show("Hello! Jeff again. You didn't select a directory for me so.. I guess you're not interested afterall so.. uhm.. gave a nice day I guess :wave:", "Jeffsters base", default, MessageBoxImage.Hand);
            }
            

            _serviceCollection.AddExamine(new DirectoryInfo(dialog.FileName));
            _serviceCollection.AddExamineLuceneIndex("MyIndex", null, new ClassicAnalyzer(LuceneVersion.LUCENE_48));
            _serviceCollection.AddOptions();
            _serviceCollection.AddTransient<Examine.Lucene.LuceneDirectoryIndexOptions>();
            var fac = new LoggerFactory();
            fac.AddProvider(new DebugLoggerProvider());
            _serviceCollection.AddSingleton<ILoggerFactory>(fac);
            _serviceCollection.AddSingleton<DatabaseContext>(provider =>
            {
                DatabaseContext ctx = new DatabaseContext();
                ctx.Database.Migrate();
                ctx.SaveChanges();
                return ctx;
            });

            ServiceProvider = _serviceCollection.BuildServiceProvider();
        }

        public static void AddInstance<TService>(TService o) where TService : class
        {
            _serviceCollection.AddSingleton<TService>(o);
            ServiceProvider = _serviceCollection.BuildServiceProvider();
        }

        public static TService GetService<TService>() where TService : class
        {
            return (TService)ServiceProvider.GetService(typeof(TService));
        }

        public static ServiceProvider ServiceProvider { get; set; }
    }
}
