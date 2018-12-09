using System;
using System.IO;
using System.IO.Abstractions;
using FolderSyncer.Core.Monitor;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Monitor
{
    public class DirectoryMonitorTests
    {
        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowException()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new DirectoryMonitor(null, channel.Object, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullChannel_ShouldThrowException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new DirectoryMonitor(configuration.Object, null, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_ShouldThrowException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var channel = new Mock<IChannel>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new DirectoryMonitor(configuration.Object, channel.Object, null));
        }

        [Fact]
        public void Constructor_WithCorrectArguments_ShouldProperlySetupFileSystemWatcher()
        {
            // Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, false);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);

            // Assert
            fileSystemWatcher.VerifySet(watcher => watcher.IncludeSubdirectories = true);
        }

        [Fact]
        public void StartMonitoring_WithNonEmptyDirectory_ShouldSendAllFilesToChannel()
        {
            //Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, true);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);
            directoryMonitor.StartMonitoring();

            //Assert
            fileSystemWatcher.VerifySet(watcher => watcher.EnableRaisingEvents = true);

            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\a" &&
                model.RelativePath == "a" &&
                model.FileAction == FileAction.Create)), Times.Once);

            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\b" &&
                model.RelativePath == "b" &&
                model.FileAction == FileAction.Create)), Times.Once);

            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\a\\1.txt" &&
                model.RelativePath == "a\\1.txt" &&
                model.FileAction == FileAction.Change)), Times.Once);

            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\2.mov" &&
                model.RelativePath == "2.mov" &&
                model.FileAction == FileAction.Change)), Times.Once);

            channel.VerifyNoOtherCalls();
        }

        [Fact]
        public void ChangedEvent_WithFileChanged_ShouldSendFileToChannel()
        {
            //Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, false);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);
            directoryMonitor.StartMonitoring();
            fileSystemWatcher.Raise(watcher => watcher.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, directory, "a.txt"));

            // Assert
            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\a.txt" &&
                model.RelativePath == "a.txt" &&
                model.FileAction == FileAction.Delete)), Times.Once);

            channel.VerifyNoOtherCalls();
        }

        [Fact]
        public void ChangedEvent_WithFileCreated_ShouldSendFileToChannel()
        {
            //Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, false);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);
            directoryMonitor.StartMonitoring();
            fileSystemWatcher.Raise(watcher => watcher.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, directory, "a.txt"));

            // Assert
            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\a.txt" &&
                model.RelativePath == "a.txt" &&
                model.FileAction == FileAction.Create)), Times.Once);

            channel.VerifyNoOtherCalls();
        }

        [Fact]
        public void ChangedEvent_WithFileDeleted_ShouldSendFileToChannel()
        {
            //Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, false);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);
            directoryMonitor.StartMonitoring();
            fileSystemWatcher.Raise(watcher => watcher.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, directory, "a.txt"));

            // Assert
            channel.Verify(chan => chan.AddFile(It.Is<FileModel>(model =>
                model.FullPath == "C:\\MyFolder\\a.txt" &&
                model.RelativePath == "a.txt" &&
                model.FileAction == FileAction.Delete)), Times.Once);

            channel.VerifyNoOtherCalls();
        }

        [Fact]
        public void ChangedEvent_WithFileRenamed_ShouldSendFileToChannel()
        {
            //Arrange
            const string directory = "C:\\MyFolder";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["directory-path"]).Returns(directory);
            var channel = new Mock<IChannel>();
            var (fileSystem, fileSystemWatcher) = GenerateFileSystemMock(directory, false);

            // Act
            var directoryMonitor = new DirectoryMonitor(configuration.Object, channel.Object, fileSystem.Object);
            directoryMonitor.StartMonitoring();
            fileSystemWatcher.Raise(watcher => watcher.Renamed += null, new RenamedEventArgs(WatcherChangeTypes.Renamed, directory, "b.txt", "a.txt"));

            // Assert
            channel.Verify(chan => chan.AddFile(It.Is<RenameFileModel>(model =>
                model.RelativePath == "a.txt" &&
                model.NewFileName == "b.txt" &&
                model.FileAction == FileAction.Rename)), Times.Once);

            channel.VerifyNoOtherCalls();
        }

        private (Mock<IFileSystem>, Mock<FileSystemWatcherBase>) GenerateFileSystemMock(string directory, bool withFiles)
        {
            var fileSystem = new Mock<IFileSystem>();
            var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();
            var directoryIo = new Mock<DirectoryBase>();
            if (withFiles)
            {
                directoryIo.Setup(dir => dir.EnumerateDirectories(directory))
                    .Returns(new[] {directory + "\\a", directory + "\\b"});
                directoryIo.Setup(dir => dir.EnumerateFiles(directory + "\\a"))
                    .Returns(new[] {directory + "\\a\\1.txt"});
                directoryIo.Setup(dir => dir.EnumerateFiles(directory)).Returns(new[] {directory + "\\2.mov"});
            }

            var fileSystemWatcher = new Mock<FileSystemWatcherBase>();

            fileSystemWatcherFactory.Setup(factory => factory.FromPath(directory)).Returns(fileSystemWatcher.Object);
            fileSystem.Setup(fs => fs.FileSystemWatcher).Returns(fileSystemWatcherFactory.Object);
            fileSystem.Setup(fs => fs.Directory).Returns(directoryIo.Object);

            return (fileSystem, fileSystemWatcher);
        }
    }
}
