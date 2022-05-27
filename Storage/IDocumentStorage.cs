using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDatabase.Storage
{
    internal interface IDocumentStorage
    {
        string GetFileOpenablePath(string fileId);
        public void AddFile(string sourceFileName);



    }
}
