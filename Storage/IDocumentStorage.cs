using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDatabase.Storage
{
    internal interface IDocumentStorage
    {
        string GetFileOpenablePath(string fileId);
        public void AddFile(string sourceFileName);
        public void AddFile(Stream fileContents, string fileName);



    }
}
