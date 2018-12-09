using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network;
using FolderSyncer.Core.Network.Protocol;
using FolderSyncer.Core.Network.ReceiverStrategies;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Network
{
    public class FileReceiverTests
    {
        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowException()
        {
            // Arrange
            var socket = new Mock<ISocket>();
            var fileStrategies = new Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new FileReceiver(null, socket.Object, fileStrategies));
        }

        [Fact]
        public void Constructor_WithNullSocket_ShouldThrowException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var fileStrategies = new Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new FileReceiver(configuration.Object, null, fileStrategies));
        }


        [Fact]
        public void Constructor_WithNullStrategies_ShouldThrowException()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            var socket = new Mock<ISocket>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new FileReceiver(configuration.Object, socket.Object, null));
        }

        [Fact]
        public void StartServer_NewConnectionAccepted_ShouldProperlyRunStrategy()
        {
            // Arrange
            const string ip = "127.0.0.1";
            const string port = "12345";
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["ip"]).Returns(ip);
            configuration.Setup(conf => conf["port"]).Returns(port);

            var socket = new Mock<ISocket>();
            socket.Setup(s => s.Bind(ip, 12345)).Returns(socket.Object);
            socket.Setup(s => s.Listen(32)).Returns(socket.Object);

            var acceptedConnection = new Mock<ISocket>();
            var acceptedStream = new MemoryStream(GetInitBuffer());
            acceptedConnection.Setup(ac => ac.GetStream()).Returns(acceptedStream);

            socket.Setup(s => s.Accept()).Returns(acceptedConnection.Object);

            var strategy = new Mock<IFileReceiverStrategy>();
            var strategies = new Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>>
            {
                {FileAction.Change, server => strategy.Object}
            };

            // Act
            var receiver = new FileReceiver(configuration.Object, socket.Object, strategies);

            var task = receiver.StartServer().First();
            task.Wait();

            // Arrange
            strategy.Verify(s => s.ProcessRequest());
        }

        private byte[] GetInitBuffer()
        {
            // Arrange
            byte[] buffer =
            {
                1, // version
                2, // Change
                7, 0, 0, 0, // filename size: 7
                102, 105, 108, 101, 46, 109, 121 // filename: file.my
            };
            return buffer;
        }
    }
}

