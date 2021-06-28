using CommandLine;

namespace DirectoryWatcher
{
    public class Options
    {

        [Value(index: 0, Required = false, HelpText = "File or folder path to analyze.")]
        public string Path { get; set; }

        [Value(index: 1, Required = false, HelpText = "Filter to analyze.")]
        public string Filter { get; set; }

        [Option("oncreated", Required = false, HelpText = "Action OnCreated")]
        public string OnCreated { get; set; }

        [Option("onchanged", Required = false, HelpText = "Action OnChanged")]
        public string OnChanged { get; set; }

        [Option("ondeleted", Required = false, HelpText = "Action OnDeleted")]
        public string OnDeleted { get; set; }

        [Option("onrenamed", Required = false, HelpText = "Action OnRenamed")]
        public string OnRenamed { get; set; }

    }
}
