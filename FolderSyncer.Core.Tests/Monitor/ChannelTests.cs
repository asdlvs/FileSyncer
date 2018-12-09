using System;
using System.Linq;
using System.Timers;
using FolderSyncer.Core.Monitor;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Monitor
{
    public class ChannelTests
    {
        [Fact]
        public void Constructor_WithNulTimer_ShouldThrowException()
        {
            // Arrange -> Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new Channel(null));
        }

        [Theory]
        [InlineData(FileAction.Create)]
        [InlineData(FileAction.Delete)]
        public void AddFile_ChangeAtTheEnd_ShouldIgnoreAllPreviousActions(FileAction action)
        {
            // Arrange
            var timer = new Mock<ITimer>();

            // Act
            var channel = new Channel(timer.Object);
            channel.AddFile(new FileModel
            {
                RelativePath = "1.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = action
            });

            channel.AddFile(new FileModel
            {
                RelativePath = "1.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = FileAction.Change
            });
            timer.Raise(t => t.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            var file = channel.GetFile().First();
            Assert.Equal("1.txt", file.RelativePath);
            Assert.Equal("C:\\MyFolder\\1.txt", file.FullPath);
            Assert.Equal(FileAction.Change, file.FileAction);
        }

        [Theory]
        [InlineData(FileAction.Create)]
        [InlineData(FileAction.Delete)]
        [InlineData(FileAction.Change)]
        public void AddFile_RenameAfterAction_ShouldSaveLastNotRenamedEvent(FileAction action)
        {
            // Arrange
            var timer = new Mock<ITimer>();

            // Act
            var channel = new Channel(timer.Object);
            channel.AddFile(new FileModel
            {
                RelativePath = "1.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = action
            });

            channel.AddFile(new RenameFileModel
            {
                RelativePath = "1.txt",
                NewFileName = "2.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = FileAction.Rename
            });

            timer.Raise(t => t.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            var changeFile = channel.GetFile().First();
            Assert.Equal("1.txt", changeFile.RelativePath);
            Assert.Equal("C:\\MyFolder\\1.txt", changeFile.FullPath);
            Assert.Equal(action, changeFile.FileAction);

            var renameFile = (RenameFileModel)channel.GetFile().First();
            Assert.Equal("1.txt", renameFile.RelativePath);
            Assert.Equal("2.txt", renameFile.NewFileName);
            Assert.Equal("C:\\MyFolder\\1.txt", renameFile.FullPath);
            Assert.Equal(FileAction.Rename, renameFile.FileAction);
        }

        [Theory]
        [InlineData(FileAction.Create)]
        [InlineData(FileAction.Delete)]
        [InlineData(FileAction.Change)]
        public void AddFile_MultipleRenamesAfterAction_ShouldSaveLastNotRenamedEvent(FileAction action)
        {
            // Arrange
            var timer = new Mock<ITimer>();

            // Act
            var channel = new Channel(timer.Object);
            channel.AddFile(new FileModel
            {
                RelativePath = "1.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = action
            });

            channel.AddFile(new RenameFileModel
            {
                RelativePath = "1.txt",
                NewFileName = "2.txt",
                FullPath = "C:\\MyFolder\\1.txt",
                FileAction = FileAction.Rename
            });

            channel.AddFile(new RenameFileModel
            {
                RelativePath = "2.txt",
                NewFileName = "3.txt",
                FullPath = "C:\\MyFolder\\2.txt",
                FileAction = FileAction.Rename
            });

            timer.Raise(t => t.Elapsed += null, (ElapsedEventArgs)null);

            // Assert
            var changeFile = channel.GetFile().First();
            Assert.Equal("1.txt", changeFile.RelativePath);
            Assert.Equal("C:\\MyFolder\\1.txt", changeFile.FullPath);
            Assert.Equal(action, changeFile.FileAction);

            var renameFile = (RenameFileModel)channel.GetFile().First();
            Assert.Equal("1.txt", renameFile.RelativePath);
            Assert.Equal("2.txt", renameFile.NewFileName);
            Assert.Equal("C:\\MyFolder\\1.txt", renameFile.FullPath);
            Assert.Equal(FileAction.Rename, renameFile.FileAction);

            var secondRenameFile = (RenameFileModel)channel.GetFile().First();
            Assert.Equal("2.txt", secondRenameFile.RelativePath);
            Assert.Equal("3.txt", secondRenameFile.NewFileName);
            Assert.Equal("C:\\MyFolder\\2.txt", secondRenameFile.FullPath);
            Assert.Equal(FileAction.Rename, secondRenameFile.FileAction);
        }
    }
}
