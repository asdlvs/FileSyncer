using System;
using System.IO.Abstractions;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using FolderSyncer.Core.Network.ReceiverStrategies;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.ReceiverStrategies
{
    public class RenameReceiverStrategyTests
    {
        [Fact]
        public void Constructor_WithNullDialogServer_shouldThrowExcetion()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RenameReceiverStrategy(null, configuration.Object, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_shouldThrowExcetion()
        {
            // Arrange
            var dialog = new Mock<IProtocolDialogServer>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RenameReceiverStrategy(dialog.Object, null, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_shouldThrowExcetion()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var dialog = new Mock<IProtocolDialogServer>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RenameReceiverStrategy(dialog.Object, configuration.Object, null));
        }

        [Fact]
        public void ProcessRequest_ShouldProperlyMoveFile()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["output-directory"]).Returns("C:\\MyFolder");

            var dialog = new Mock<IProtocolDialogServer>();
            dialog.Setup(dia => dia.ReadValue(4, It.IsAny<Func<byte[], int>>())).Returns(5);
            dialog.Setup(dia => dia.ReadValue(5, It.IsAny<Func<byte[], string>>())).Returns("b.txt");
            dialog.Setup(dia => dia.DialogData).Returns(new ProtocolDialogData
            {
                FileAction = FileAction.Rename,
                Version = 1,
                RelativeFilePath = "a.txt"
            });

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.Path).Returns(new FileSystem().Path);
            var file = new Mock<FileBase>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);

            // Act
            IFileReceiverStrategy rename = new RenameReceiverStrategy(dialog.Object, configuration.Object, fileSystem.Object);
            rename.ProcessRequest();

            // Assert
            file.Verify(f => f.Move("C:\\MyFolder\\a.txt", "C:\\MyFolder\\b.txt"), Times.Once);
        }
    }
}
