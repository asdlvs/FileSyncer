using System;
using System.IO;
using System.Linq;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.Protocol
{
    public class ProtocolDialogServerTests
    {
        [Fact]
        public void Constructor_NullStream_ShouldThrowException()
        {
            // Arrange Act Assert
            Assert.Throws<ArgumentNullException>(() => new ProtocolDialogServer(null));
        }

        [Fact]
        public void AcceptDialog_ShouldReturnProtocolData()
        {
            // Arrange
            var buffer = GetInitBuffer();
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            var data = dialog.AcceptDialog();

            // Assert
            Assert.Equal(1, data.Version);
            Assert.Equal(FileAction.Change, data.FileAction);
            Assert.Equal(FileType.Unknown, data.FileType);
            Assert.Equal("file.my", data.RelativeFilePath);
        }

        [Fact]
        public void DialogData_ShouldReturnProtocolData()
        {
            // Arrange
            var buffer = GetInitBuffer();
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            dialog.AcceptDialog();
            var data = dialog.DialogData;

            // Assert
            Assert.Equal(1, data.Version);
            Assert.Equal(FileAction.Change, data.FileAction);
            Assert.Equal(FileType.Unknown, data.FileType);
            Assert.Equal("file.my", data.RelativeFilePath);
        }

        [Fact]
        public void ReadValue_DataIsLessThanSize_ShouldProperlyReturnValue()
        {
            // Arrange
            byte[] buffer = { 1, 2, 3, 4, 5 };
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            var value = dialog.ReadValue(10, data => data);

            // Assert
            Assert.Equal(5, value.Length);
            Assert.Equal(buffer, value);
            Assert.False(dialog.HasData());
        }

        [Fact]
        public void ReadValue_DataIsEqualToSize_ShouldProperlyReturnValue()
        {
            // Arrange
            byte[] buffer = { 1, 2, 3, 4, 5 };
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            var value = dialog.ReadValue(5, data => data);

            // Assert
            Assert.Equal(5, value.Length);
            Assert.Equal(buffer, value);
            Assert.True(dialog.HasData());
        }

        [Fact]
        public void ReadValue_DataIsMoreThanSize_ShouldProperlyReturnValue()
        {
            // Arrange
            byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8 };
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            var firstSegment = dialog.ReadValue(5, data => data);
            var secondSegment = dialog.ReadValue(5, data => data);

            // Assert
            Assert.Equal(5, firstSegment.Length);
            Assert.Equal(buffer.Take(5), firstSegment);
            Assert.Equal(3, secondSegment.Length);
            Assert.Equal(buffer.Skip(5).Take(5), secondSegment);
            Assert.False(dialog.HasData());
        }

        [Fact]
        public void NotifyClient_ActionIsNeeded_ShouldSendCorrectFlagToStream()
        {
            // Arrange
            byte[] buffer = new byte[1];
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            dialog.NotifyClient(true);

            // Assert
            Assert.Equal(1, buffer[0]);
        }

        [Fact]
        public void NotifyClient_ActionIsNotNeeded_ShouldSendCorrectFlagToStream()
        {
            // Arrange
            byte[] buffer = new byte[1];
            var stream = new MemoryStream(buffer);

            // Act
            var dialog = new ProtocolDialogServer(stream);
            dialog.NotifyClient(false);

            // Assert
            Assert.Equal(0, buffer[0]);
        }

        private byte[] GetInitBuffer()
        {
            // Arrange
            byte[] buffer =
            {
                1, // version
                2, // Change
                0, // Type
                7, 0, 0, 0, // filename size: 7
                102, 105, 108, 101, 46, 109, 121 // filename: file.my
            };
            return buffer;
        }
    }
}
