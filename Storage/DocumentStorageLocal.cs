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

        public string GetFileOpenablePath(string fileName)
        {
            return $"{_localDirectory}{System.IO.Path.DirectorySeparatorChar}{fileName}";
        }

        public void AddFile(string sourceFileName)
        {
            try
            {
                System.IO.File.Copy(sourceFileName, $"{_localDirectory}/{System.IO.Path.GetFileName(sourceFileName)}", true);
            }
            catch (System.IO.IOException ex)
            {
                // Hope this is "already exists" error
            }
        }

        public void AddFile(Stream fileContents, string fileName)
        {
            using FileStream outStream = new FileStream(GetFileOpenablePath(fileName), FileMode.CreateNew);
            fileContents.CopyTo(outStream);
        }
    }
}
