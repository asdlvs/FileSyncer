using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FolderSyncer.Core.Monitor
{
    /// <summary>
    /// Initializes parsing of working directory and starts monitoring of this directory.
    /// Each event is sent to Channel.
    /// </summary>
    public class DirectoryMonitor : IDirectoryMonitor
    {
        private readonly IChannel _channel;
        private readonly IFileSystem _fileSystem;
        private readonly FileSystemWatcherBase _watcher;

        private readonly string _directoryPath;

        public DirectoryMonitor(IConfiguration configuration, IChannel channel, IFileSystem fileSystem)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }
            if (channel == null) {  throw new ArgumentNullException(nameof(channel)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }

            _channel = channel;
            _fileSystem = fileSystem;
            _directoryPath = configuration["directory-path"];

            _watcher = _fileSystem.FileSystemWatcher.FromPath(_directoryPath);
            _watcher.IncludeSubdirectories = true;
            _watcher.Created += GetEventAction(FileAction.Create);
            _watcher.Changed += GetEventAction(FileAction.Change);
            _watcher.Deleted += GetEventAction(FileAction.Delete);
            _watcher.Renamed += (sender, args) =>
            {
                _channel.AddFile(new RenameFileModel
                {
                    FileAction = FileAction.Rename,
                    RelativePath = GetRelativePath(args.OldFullPath, _directoryPath),
                    NewFileName = GetRelativePath(args.FullPath, _directoryPath)
                });
            };
        }

        /// <summary>
        /// Init parsing of directory and starting FileSystemWatcher.
        /// </summary>
        public void StartMonitoring()
        {
            ParseDirectory(_directoryPath);
            _watcher.EnableRaisingEvents = true;
        }

        private void ParseDirectory(string directoryPath)
        {
            foreach (var directory in _fileSystem.Directory.EnumerateDirectories(directoryPath))
            {
                SendFile(directory, FileAction.Create);
                ParseDirectory(directory);
            }

            foreach (var absolutePath in _fileSystem.Directory.EnumerateFiles(directoryPath))
            {
                SendFile(absolutePath, FileAction.Change);
            }
        }

        private string GetRelativePath(string fullPath, string basePath)
        {
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            return relativeUri.ToString().Replace("/", "\\");
        }

        private void SendFile(string absolutePath, FileAction action)
        {
            Log.Information($"Event {action} for file {absolutePath} was received.");
            _channel.AddFile(new FileModel
            {
                RelativePath = GetRelativePath(absolutePath, _directoryPath),
                FileAction = action,
                FullPath = absolutePath
            });
        }

        private FileSystemEventHandler GetEventAction(FileAction action)
        {
            return (sender, args) =>
            {
                SendFile(args.FullPath, action);
            };
        }
    }
}