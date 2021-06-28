using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryWatcher
{
    public class FileSystemWatcher : System.IO.FileSystemWatcher
    {
        public string ActionOnCreated { get; set; }
        public string ActionOnChanged { get; set; }
        public string ActionOnDeleted { get; set; }
        public string ActionOnRenamed { get; set; }

    }
}
