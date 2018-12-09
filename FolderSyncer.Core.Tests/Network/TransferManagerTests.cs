using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network;
using FolderSyncer.Core.Network.TransferStrategies;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Network
{
    public class TransferManagerTests
    {
        [Fact]
        public void Constructor_WithNullChannel_ShouldThrowException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();
            var socket = new Mock<ISocket>();
            ISocket SocketFactory() => socket.Object;
            var transferStrategies = new Dictionary<FileAction, Func<ITransferStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new TransferManager(null, configuration.Object, fileSystem.Object, SocketFactory, transferStrategies));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowException()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var fileSystem = new Mock<IFileSystem>();
            var socket = new Mock<ISocket>();
            ISocket SocketFactory() => socket.Object;
            var transferStrategies = new Dictionary<FileAction, Func<ITransferStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new TransferManager(channel.Object, null, fileSystem.Object, SocketFactory, transferStrategies));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_ShouldThrowException()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var configuration = new Mock<IConfiguration>();
            var socket = new Mock<ISocket>();
            ISocket SocketFactory() => socket.Object;
            var transferStrategies = new Dictionary<FileAction, Func<ITransferStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new TransferManager(channel.Object, configuration.Object, null, SocketFactory, transferStrategies));
        }

        [Fact]
        public void Constructor_WithNullSocket_ShouldThrowException()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();
            var transferStrategies = new Dictionary<FileAction, Func<ITransferStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new TransferManager(channel.Object, configuration.Object, fileSystem.Object, null, transferStrategies));
        }

        [Fact]
        public void Constructor_WithNullStrategies_ShouldThrowException()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();
            var socket = new Mock<ISocket>();
            ISocket SocketFactory() => socket.Object;

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TransferManager(channel.Object, configuration.Object, fileSystem.Object, SocketFactory, (Dictionary<FileAction, Func<ITransferStrategy>>)null));
        }

        [Fact]
        public void TransferFile_FileIsLocked_ShouldWaitWhileFileIsLocked()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var configuration = MockConfiguration();

            const string filePath = "C:\\MyFolder\\1.txt";
            var (fileSystem, file) = MockFileSystemToLockFile(filePath);

            var strategy = new Mock<ITransferStrategy>();
            var strategies = new Dictionary<FileAction, Func<ITransferStrategy>>
            {
                {FileAction.Change, () => strategy.Object}
            };

            var memoryStream = new MemoryStream();
            var socket = MockSocket(memoryStream);

            // Act
            var transfer = new TransferManager(channel.Object, configuration.Object, fileSystem.Object, () => socket.Object, strategies);
            transfer.TransferFile(new FileModel
            {
                FileAction = FileAction.Change,
                RelativePath = "1.txt",
                FullPath = filePath
            });

            // Assert
            file.Verify(f => f.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None), Times.Exactly(3));
        }

        [Fact]
        public void TransferFile_ShouldExecuteStrategy()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            var configuration = MockConfiguration();

            const string filePath = "C:\\MyFolder\\1.txt";
            var fileSystem = MockFileSystem();

            var strategy = new Mock<ITransferStrategy>();
            var strategies = new Dictionary<FileAction, Func<ITransferStrategy>>
            {
                {FileAction.Change, () => strategy.Object}
            };

            var memoryStream = new MemoryStream();
            var socket = MockSocket(memoryStream);

            // Act
            var transfer = new TransferManager(channel.Object, configuration.Object, fileSystem.Object, () => socket.Object, strategies);
            transfer.TransferFile(new FileModel
            {
                FileAction = FileAction.Change,
                RelativePath = "1.txt",
                FullPath = filePath
            });

            // Assert
            strategy.Verify(s => s.Execute(It.Is<FileModel>(model =>
                model.FileAction == FileAction.Change &&
                model.RelativePath == "1.txt" &&
                model.FullPath == filePath), memoryStream));
        }

        [Fact]
        public void TransferData_MultipleFileModels_ShouldExecuteStrategyForEach()
        {
            // Arrange
            var channel = new Mock<IChannel>();
            channel.Setup(chan => chan.GetFile()).Returns(new List<FileModel>
            {
                new RenameFileModel {  FileAction = FileAction.Rename, FullPath = "C:\\1.txt", RelativePath = "1.txt", NewFileName = "2.txt" },
                new FileModel {  FileAction = FileAction.Change, FullPath = "C:\\1.txt", RelativePath = "1.txt" },
                new FileModel {  FileAction = FileAction.Delete, FullPath = "C:\\1.txt", RelativePath = "1.txt" },
            });

            var configuration = MockConfiguration();

            const string filePath = "C:\\1.txt";
            var fileSystem = MockFileSystem();

            var rename = new Mock<ITransferStrategy>();
            var change = new Mock<ITransferStrategy>();
            var delete = new Mock<ITransferStrategy>();
            var strategies = new Dictionary<FileAction, Func<ITransferStrategy>>
            {
                {FileAction.Rename, () => rename.Object},
                {FileAction.Change, () => change.Object},
                {FileAction.Delete, () => delete.Object}
            };

            var memoryStream = new MemoryStream();
            var socket = MockSocket(memoryStream);

            // Act
            var transfer = new TransferManager(channel.Object, configuration.Object, fileSystem.Object, () => socket.Object, strategies);
            var tasks = new List<Task>();
            tasks.AddRange(transfer.TransferData());
            Task.WaitAll(tasks.ToArray());

            // Assert
            rename.Verify(s => s.Execute(It.Is<RenameFileModel>(model =>
                model.FileAction == FileAction.Rename &&
                model.RelativePath == "1.txt" &&
                model.NewFileName == "2.txt" &&
                model.FullPath == filePath), memoryStream));

            change.Verify(s => s.Execute(It.Is<FileModel>(model =>
                model.FileAction == FileAction.Change &&
                model.RelativePath == "1.txt" &&
                model.FullPath == filePath), memoryStream));

            delete.Verify(s => s.Execute(It.Is<FileModel>(model =>
                model.FileAction == FileAction.Delete &&
                model.RelativePath == "1.txt" &&
                model.FullPath == filePath), memoryStream));
        }

        private Mock<ISocket> MockSocket(MemoryStream memoryStream)
        {
            var socket = new Mock<ISocket>();
            socket.Setup(s => s.Connect("127.0.0.1", 12345)).Returns(socket.Object);
            socket.Setup(s => s.GetStream()).Returns(memoryStream);
            return socket;
        }

        private (Mock<IFileSystem>, Mock<FileBase>) MockFileSystemToLockFile(string filePath)
        {
            var file = new Mock<FileBase>();
            int call = 0;
            file.Setup(f => f.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                .Returns(() => call++ < 2 ? throw new IOException() : new MemoryStream());

            var directory = new Mock<DirectoryBase>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);
            fileSystem.Setup(fs => fs.Directory).Returns(directory.Object);
            return (fileSystem, file);
        }

        private Mock<IFileSystem> MockFileSystem()
        {
            var file = new Mock<FileBase>();
            var directory = new Mock<DirectoryBase>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);
            fileSystem.Setup(fs => fs.Directory).Returns(directory.Object);
            return fileSystem;
        }

        private static Mock<IConfiguration> MockConfiguration()
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["ip"]).Returns("127.0.0.1");
            configuration.Setup(conf => conf["port"]).Returns("12345");
            return configuration;
        }
    }
}
