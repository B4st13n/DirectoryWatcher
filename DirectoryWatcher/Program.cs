using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommandLine;
using Microsoft.Extensions.Configuration;

namespace DirectoryWatcher
{
    public class Program
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly List<WatchFolder> watchList = new();

        static void Main(string[] args)
        {
            logger.Info("Start application");

            #region load folder to watch from command line
            if (args.Any())
            {
                logger.Info(@$"Load watch configuration (command line)");
                // default options
                var result = Parser.Default.ParseArguments<Options>(args);
                result.WithParsed(options =>
                {
                    Console.WriteLine("Parser Success- Creating Options with values:");
                    watchList.Add(new WatchFolder
                    {
                        Path = options.Path,
                        Filter = options.Filter,
                        OnCreated = options.OnCreated,
                        OnChanged = options.OnChanged,
                        OnDeleted = options.OnDeleted,
                        OnRenamed = options.OnDeleted
                });
                })
                    .WithNotParsed(errors => Console.WriteLine("Failed with errors:\n{0}", String.Join("\n", errors)));
            }
            #endregion

            #region Load folder to watch from json file (not used)
            //if (File.Exists(@$"{Directory.GetCurrentDirectory()}\{watchFileName}"))
            //{
            //    logger.Info(@$"Load watch configuration ({watchFileName})");
            //    string fileName = @$"{Directory.GetCurrentDirectory()}\{watchFileName}";
            //    string jsonString = File.ReadAllText(fileName);
            //    watchList.AddRange(JsonSerializer.Deserialize<List<WatchFolder>>(jsonString));
            //}
            #endregion

            watchList.ForEach(y => Watch(y));

            // wait - not to end
            _ = new System.Threading.AutoResetEvent(false).WaitOne();
        }

        private static void Watch(WatchFolder watchFolder)
        {
            try
            {
                logger.Info(@$"Watching {watchFolder.Path} {watchFolder.Filter}");
                FileSystemWatcher fileSystemWatcher = new()
                {
                    Path = watchFolder.Path,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                         | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    ActionOnCreated = watchFolder.OnCreated,
                    ActionOnChanged = watchFolder.OnChanged,
                    ActionOnDeleted = watchFolder.OnDeleted,
                    ActionOnRenamed = watchFolder.OnRenamed,
                };

                watchFolder.Filter.Split('|').ToList().ForEach(y =>
                {
                    fileSystemWatcher.Filters.Add(y);
                });

                fileSystemWatcher.Created += new FileSystemEventHandler(OnCreated);
                fileSystemWatcher.Changed += new FileSystemEventHandler(OnChanged);
                fileSystemWatcher.Deleted += new FileSystemEventHandler(OnDeleted);
                fileSystemWatcher.Renamed += new RenamedEventHandler(OnRenamed);
                fileSystemWatcher.Error += new ErrorEventHandler(OnError);
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.IncludeSubdirectories = true;
            }
            catch (Exception exception)
            {
                logger.Error(@$"Exception on {watchFolder.Path}. {exception.Message}.");
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(nameof(OnCreated));
            logger.Debug(@$"{nameof(OnCreated)} {e.Name}");

            string path = ((FileSystemWatcher)sender).Path;
            string action = ((FileSystemWatcher)sender).ActionOnCreated;

            if (!string.IsNullOrWhiteSpace(action))
            {
                logger.Info(@$"New files detected {e.Name}");
                string launchApp = action
                    .Replace("%1", @$"""{ e.FullPath}""")
                    .Replace("{{filename}}", @$"""{ e.FullPath}""")
                    .Replace("\\\\", "/")
                    .Replace("\\", "/");
                RunCommandLine(launchApp);
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(nameof(OnChanged));
            logger.Debug(@$"{nameof(OnChanged)} {e.Name}");

            string path = ((FileSystemWatcher)sender).Path;
            string action = ((FileSystemWatcher)sender).ActionOnChanged;

            if (!string.IsNullOrWhiteSpace(action))
            {
                logger.Info(@$"Modification detected {e.Name}");
                string launchApp = action
                    .Replace("%1", @$"""{ e.FullPath}""")
                    .Replace("{{filename}}", @$"""{ e.FullPath}""")
                    .Replace("\\\\", "/")
                    .Replace("\\", "/");
                RunCommandLine(launchApp);
            }
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(nameof(OnDeleted));
            logger.Debug(@$"{nameof(OnDeleted)} {e.Name}");

            string path = ((FileSystemWatcher)sender).Path;
            string action = ((FileSystemWatcher)sender).ActionOnDeleted;

            if (!string.IsNullOrWhiteSpace(action))
            {
                logger.Info(@$"Files removed {e.Name}");
                string launchApp = action
                    .Replace("%1", @$"""{ e.FullPath}""")
                    .Replace("{{filename}}", @$"""{ e.FullPath}""")
                    .Replace("\\\\", "/")
                    .Replace("\\", "/");
                RunCommandLine(launchApp);
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            //Console.WriteLine(nameof(OnRenamed));
            logger.Debug(@$"{nameof(OnRenamed)} {e.Name}");

            string path = ((FileSystemWatcher)sender).Path;
            string action = ((FileSystemWatcher)sender).ActionOnRenamed;

            if (!string.IsNullOrWhiteSpace(action))
            {
                logger.Info(@$"File renamed from {e.OldName} to {e.Name}");
                string launchApp = action
                    .Replace("%1", @$"""{ e.FullPath}""")
                    .Replace("{{filename}}", @$"""{ e.FullPath}""")
                    .Replace("\\\\", "/")
                    .Replace("\\", "/");
                RunCommandLine(launchApp);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                logger.Error("File System Watcher internal buffer overflow");
            }
            else
            {
                logger.Error("Watched directory not accessible");
            }
           // NotAccessibleError(fileSystemWatcher, e);
        }

        static void NotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
        {
            source.EnableRaisingEvents = false;
            int iMaxAttempts = 120;
            int iTimeOut = 30000;
            int i = 0;
            while (source.EnableRaisingEvents == false && i < iMaxAttempts)
            {
                i += 1;
                try
                {
                    source.EnableRaisingEvents = true;
                }
                catch
                {
                    source.EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(iTimeOut);
                }
            }

        }

        private static void RunCommandLine(string commandText)
        {
            try
            {
                logger.Debug(@$"{nameof(RunCommandLine)} {commandText}");

                Process process = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = @$"/c {commandText}"
                };
                process.StartInfo = startInfo;
                logger.Info(@$"Starting {commandText}");
                var result = process.Start();
                if (!result)
                    logger.Error(@$"Process {commandText} is {result}");
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }
    }
}
