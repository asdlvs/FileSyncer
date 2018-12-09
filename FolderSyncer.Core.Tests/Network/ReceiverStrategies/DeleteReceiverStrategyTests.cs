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
    public class DeleteReceiverStrategyTests
    {
        [Fact]
        public void Constructor_WithNullDialogServer_shouldThrowExcetion()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DeleteReceiverStrategy(null, configuration.Object, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_shouldThrowExcetion()
        {
            // Arrange
            var dialog = new Mock<IProtocolDialogServer>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DeleteReceiverStrategy(dialog.Object, null, fileSystem.Object));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_shouldThrowExcetion()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var dialog = new Mock<IProtocolDialogServer>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DeleteReceiverStrategy(dialog.Object, configuration.Object, null));
        }

        [Fact]
        public void ProcessRequest_ShouldProperlyMoveFile()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["output-directory"]).Returns("C:\\MyFolder");

            var dialog = new Mock<IProtocolDialogServer>();
            dialog.Setup(dia => dia.DialogData).Returns(new ProtocolDialogData
            {
                FileAction = FileAction.Delete,
                Version = 1,
                RelativeFilePath = "a.txt"
            });

            const string fullPath = "C:\\MyFolder\\a.txt";
            var file = new Mock<FileBase>();
            file.Setup(f => f.Exists(fullPath)).Returns(true);
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.Path).Returns(new FileSystem().Path);
            fileSystem.Setup(fs => fs.File).Returns(file.Object);

            // Act
            IFileReceiverStrategy delete = new DeleteReceiverStrategy(dialog.Object, configuration.Object, fileSystem.Object);
            delete.ProcessRequest();

            // Assert
            file.Verify(f => f.Delete(fullPath), Times.Once);
        }
    }
}
