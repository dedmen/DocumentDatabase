using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentDatabase.Storage;
using Examine;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.PlatformAbstractions;
namespace DocumentDatabase
{
    public static class DependencyInjection
    {
        private static ServiceCollection _serviceCollection;
        static DependencyInjection()
        {
            
            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddExamine();
            _serviceCollection.AddExamineLuceneIndex("MyIndex", null, new ClassicAnalyzer(LuceneVersion.LUCENE_48));
            _serviceCollection.AddOptions();
            _serviceCollection.AddTransient<Examine.Lucene.LuceneDirectoryIndexOptions>();
            var fac = new LoggerFactory();
            fac.AddProvider(new DebugLoggerProvider());
            _serviceCollection.AddSingleton<ILoggerFactory>(fac);
            _serviceCollection.AddSingleton<DatabaseContext>();
            _serviceCollection.AddSingleton<IDocumentStorage>(new DocumentStorageLocal("P:/"));

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
