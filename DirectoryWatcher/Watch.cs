using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryWatcher
{
    internal class Watch
    {
        public List<WatchFolder> WatchList { get; set; }
    }

    internal class WatchFolder
    {
        public string Path { get; set; }
        public string Filter { get; set; }
        public string OnCreated { get; set; }
        public string OnChanged { get; set; }
        public string OnDeleted { get; set; }
        public string OnRenamed { get; set; }
    }
}