using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using FolderSyncer.Core.Network.ReceiverStrategies;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.ReceiverStrategies
{
    public class ChangeReceiverStrategyTests
    {
        private const string OutputDirectory = "C:\\MyFolder";
        private readonly byte[] _fileContent = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 };
        private readonly byte[] _correctHash = { 95, 6, 28, 75, 233, 238, 190, 130, 250, 108, 70, 74, 153, 220, 32, 145 };
        private readonly byte[] _incorrectHash = { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1 };

        [Fact]
        public void Constructor_WithNullDialogServer_shouldThrowExcetion()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var hashAlgorithm = new Mock<HashAlgorithm>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChangeReceiverStrategy(null, configuration.Object, hashAlgorithm.Object, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_shouldThrowExcetion()
        {
            // Arrange
            var dialogServer = new Mock<IProtocolDialogServer>();
            var hashAlgorithm = new Mock<HashAlgorithm>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChangeReceiverStrategy(dialogServer.Object, null, hashAlgorithm.Object, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullHash_shouldThrowExcetion()
        {
            // Arrange
            var dialogServer = new Mock<IProtocolDialogServer>();
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, null, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_shouldThrowExcetion()
        {
            // Arrange
            var dialogServer = new Mock<IProtocolDialogServer>();
            var configuration = new Mock<IConfiguration>();
            var hashAlgorithm = new Mock<HashAlgorithm>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm.Object, null));
        }

        [Fact]
        public void ProcessRequest_FileAlreadyExistsAndHashIsSame_ShouldNotifyClientNotToSendFile()
        {
            // Arrange
            var configuration = MockConfiguration();
            var (fileSystem, _, _) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_correctHash);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            dialogServer.Verify(d => d.NotifyClient(false), Times.Once);
        }

        [Fact]
        public void ProcessRequest_FileAlreadyExistsButHashIsDifferent_ShouldNotifyClientToSendFile()
        {
            // Arrange
            var configuration = MockConfiguration();
            var (fileSystem, _, _) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_incorrectHash);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            dialogServer.Verify(d => d.NotifyClient(true), Times.Once);
        }

        [Fact]
        public void ProcessRequest_FileAlreadyExistsButHashIsDifferentAndRelativeDirectoryDoesnExist_ShouldCreateRelativeDirectory()
        {
            // Arrange
            var configuration = MockConfiguration();
            var (fileSystem, _, directory) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_incorrectHash);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            directory.Verify(dir => dir.Exists("C:\\MyFolder\\sub"));
            directory.Verify(dir => dir.CreateDirectory("C:\\MyFolder\\sub"));
        }

        [Fact]
        public void ProcessRequest_FileAlreadyExistsButHashIsDifferentAndOldTmpFileExists_ShouldRemoveOldTmpFile()
        {
            // Arrange
            var configuration = MockConfiguration();
            var (fileSystem, file, _) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            file.Setup(f => f.Exists("C:\\MyFolder\\sub\\1.txt.tmp")).Returns(true);
            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_incorrectHash);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            file.Verify(f => f.Exists("C:\\MyFolder\\sub\\1.txt.tmp"));
            file.Verify(f => f.Delete("C:\\MyFolder\\sub\\1.txt.tmp"));
        }

        [Fact]
        public void ProcessRequest_FileNotExistButFirstSegmentExists_ShouldTakeSegmentFromCache()
        {
            // Arrange
            var configuration = MockConfiguration();

            var (fileSystem, file, _) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            var destinationBuffer = new byte[16];
            SetupSegmentPolicy(destinationBuffer, file, true);

            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_incorrectHash);
            SetupDialogSegmentPolicy(dialogServer);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            dialogServer.Verify(d => d.NotifyClient(false), Times.Once);
            Assert.Equal(_fileContent, destinationBuffer);
        }

        [Fact]
        public void ProcessRequest_FileNotExistAndFirstSegmentNotExists_ShouldReadSegmentFromSource()
        {
            // Arrange
            var configuration = MockConfiguration();

            var (fileSystem, file, _) = MockFileSystem(OutputDirectory, new MemoryStream(_fileContent));
            var destinationBuffer = new byte[16];
            var segmentPath = SetupSegmentPolicy(destinationBuffer, file, false);

            var hashAlgorithm = MD5.Create();
            var dialogServer = MockDialogServer(_incorrectHash);
            SetupDialogSegmentPolicy(dialogServer);

            // Act
            var receiveStrategy = new ChangeReceiverStrategy(dialogServer.Object, configuration.Object, hashAlgorithm, fileSystem.Object);
            receiveStrategy.ProcessRequest();

            // Assert
            dialogServer.Verify(d => d.NotifyClient(true), Times.Exactly(2));
            file.Verify(f => f.WriteAllBytes(segmentPath, _incorrectHash));
            Assert.Equal(_incorrectHash, destinationBuffer);
        }

        private Mock<IProtocolDialogServer> MockDialogServer(byte[] hash)
        {
            var dialogServer = new Mock<IProtocolDialogServer>();
            dialogServer.Setup(dia => dia.DialogData).Returns(new ProtocolDialogData
            {
                RelativeFilePath = "sub\\1.txt",
                Version = 1,
                FileAction = FileAction.Change
            });

            dialogServer.Setup(dia => dia.ReadValue(MD5.Create().HashSize / 8, It.IsAny<Func<byte[], byte[]>>()))
                .Returns(hash);
            return dialogServer;
        }

        private Mock<IConfiguration> MockConfiguration()
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["output-directory"]).Returns(OutputDirectory);
            configuration.Setup(conf => conf["segment-size"]).Returns("16");
            return configuration;
        }

        private (Mock<IFileSystem>, Mock<FileBase>, Mock<DirectoryBase>) MockFileSystem(string outputDirectory, MemoryStream stream)
        {
            var file = new Mock<FileBase>();
            file.Setup(f => f.Exists(outputDirectory + "\\sub\\1.txt")).Returns(true);
            file.Setup(f => f.OpenRead(outputDirectory + "\\sub\\1.txt")).Returns(stream);

            var directory = new Mock<DirectoryBase>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.Path).Returns(new FileSystem().Path);
            fileSystem.Setup(fs => fs.File).Returns(file.Object);
            fileSystem.Setup(fs => fs.Directory).Returns(directory.Object);
            return (fileSystem, file, directory);
        }

        private void SetupDialogSegmentPolicy(Mock<IProtocolDialogServer> dialogServer)
        {
            var values = new[]
            {
                "base64_hash_encoded",
                ""
            };
            int i = 0;
            dialogServer.Setup(dia => dia.ReadValue(MD5.Create().HashSize / 8, It.IsAny<Func<byte[], string>>()))
                .Returns(() => values[i++]);
        }

        private string SetupSegmentPolicy(byte[] destinationBuffer, Mock<FileBase> file, bool segmentExists)
        {
            var destinationStream = new MemoryStream(destinationBuffer);
            file.Setup(f => f.Open(OutputDirectory + "\\sub\\1.txt.tmp", FileMode.CreateNew)).Returns(destinationStream);
            string segmentPath = "C:\\MyFolder\\.syncer\\base64_hash_encoded";
            file.Setup(f => f.Exists(segmentPath)).Returns(segmentExists);
            file.Setup(f => f.ReadAllBytes(segmentPath)).Returns(_fileContent);
            return segmentPath;
        }
    }
}
