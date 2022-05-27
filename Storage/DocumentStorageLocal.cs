using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDatabase.Storage
{
    internal class DocumentStorageLocal : IDocumentStorage
    {
        private readonly string _localDirectory;

        public DocumentStorageLocal(string localDirectory)
        {
            _localDirectory = localDirectory;
        }

        public string GetFileOpenablePath(string fileId)
        {
            return $"{_localDirectory}/{fileId}.pdf";
        }

        public void AddFile(string sourceFileName)
        {
            try
            {
                System.IO.File.Copy(sourceFileName, $"{_localDirectory}/{System.IO.Path.GetFileName(sourceFileName)}");
            }
            catch (System.IO.IOException ex)
            {
                // Hope this is "already exists" error
            }
            
        }


    }
}
