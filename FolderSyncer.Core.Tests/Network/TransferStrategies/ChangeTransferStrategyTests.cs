using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.TransferStrategies;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.TransferStrategies
{
    public class ChangeTransferStrategyTests
    {
        private const int InitialDataSize = 12;

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowException()
        {
            // Arrange
            var hash = new Mock<HashAlgorithm>();
            var fileSystem = new Mock<IFileSystem>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new ChangeTransferStrategy(null, fileSystem.Object, hash.Object));
        }

        [Fact]
        public void Constructor_WithNullFileSystem_ShouldThrowException()
        {
            // Arrange
            var hash = new Mock<HashAlgorithm>();
            var configuration = new Mock<IConfiguration>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new ChangeTransferStrategy(configuration.Object, null, hash.Object));
        }

        [Fact]
        public void Constructor_WithNullHash_ShouldThrowException()
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>();
            var configuration = new Mock<IConfiguration>();

            // Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new ChangeTransferStrategy(configuration.Object, fileSystem.Object, null));
        }

        [Fact]
        public void Execute_WithNullModel_ShouldShrowException()
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>();
            var configuration = new Mock<IConfiguration>();
            var hash = new Mock<HashAlgorithm>();
            
            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hash.Object);

            // Assert
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, new MemoryStream()));
        }

        [Fact]
        public void Execute_WithNullStream_ShouldShrowException()
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>();
            var configuration = new Mock<IConfiguration>();
            var hash = new Mock<HashAlgorithm>();
            
            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hash.Object);
            
            // Assert
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(new RenameFileModel(), null));
        }

        [Fact]
        public void Execute_ShouldInitiateDialogWithCorrectValues()
        {
            // Arrange
            const string filePath = "C:\\MyFolder\\1.txt";

            var file = new Mock<FileBase>();
            file.Setup(f => f.OpenRead(filePath)).Returns(Stream.Null);
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);

            var configuration = new Mock<IConfiguration>();
            var hash = new Mock<HashAlgorithm>();

            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hash.Object);
            var memoryStream = new MemoryStream();
            strategy.Execute(GetFileModel(filePath), memoryStream);

            // Assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(memoryStream);

            var version = reader.ReadBytes(1).Single();
            Assert.Equal(1, version);

            var action = (FileAction)reader.ReadBytes(1).Single();
            Assert.Equal(FileAction.Change, action);

            var type = (FileType)reader.ReadBytes(1).Single();
            Assert.Equal(FileType.Unknown, type);


            var size = BitConverter.ToInt32(reader.ReadBytes(4), 0);
            Assert.Equal(5, size);

            var filename = Encoding.GetEncoding("UTF-8").GetString(reader.ReadBytes(size));
            Assert.Equal("1.txt", filename);
        }

        [Fact]
        public void Execute_IfFileExistsOnServer_ShouldNotSendFile()
        {
            // Arrange
            const string filePath = "C:\\MyFolder\\1.txt";

            var fileSystem = MockFileSystem(filePath);
            var configuration = new Mock<IConfiguration>();
            var hashAlg = MD5.Create();

            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hashAlg);
            var memoryStream = new MemoryStream();
            strategy.Execute(GetFileModel(filePath), memoryStream);

            // Assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(memoryStream);
            memoryStream.Seek(InitialDataSize, SeekOrigin.Begin); // Skip initiation data

            var hash = reader.ReadBytes(hashAlg.HashSize);
            Assert.Equal(16, hash.Length);

            var otherValues = reader.ReadBytes(10);
            Assert.Equal(0, otherValues?.Length);
        }

        [Fact]
        public void Execute_IfFileDoesntExistOnServerButSegmentExists_ShouldNotSendSegment()
        {
            // Arrange
            const string filePath = "C:\\MyFolder\\1.txt";

            var fileSystem = MockFileSystem(filePath);
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["segment-size"]).Returns("10");
            var hashAlg = MD5.Create();
            var hashSize = hashAlg.HashSize / 8;

            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hashAlg);
            var buffer = new byte[45];
            buffer[27] = 1; // Answer from server that we need this file
            var memoryStream = new MemoryStream(buffer);
            strategy.Execute(GetFileModel(filePath), memoryStream);

            // Assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(memoryStream);
            memoryStream.Seek(InitialDataSize + hashSize + 1, SeekOrigin.Begin);  // Skip initiation data + filehash + server response
            var hash = reader.ReadBytes(hashSize);
            Assert.Equal(16, hash.Length);
            var otherValues = reader.ReadBytes(10);
            Assert.Equal(0, otherValues?.Length);
        }

        [Fact]
        public void Execute_IfFileDoesntExist_ShouldSendAllSegments()
        {
            // Arrange
            const string filePath = "C:\\MyFolder\\1.txt";

            var fileSystem = MockFileSystem(filePath);
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(conf => conf["segment-size"]).Returns("5");
            var hashAlg = MD5.Create();
            var hashSize = hashAlg.HashSize / 8;

            // Act
            var strategy = new ChangeTransferStrategy(configuration.Object, fileSystem.Object, hashAlg);
            var buffer = new byte[73];
            buffer[28] = 1; // Answer from server that we need this file
            buffer[45] = 1; // Answer from server that we need first segment
            buffer[67] = 1; // Answer from server that we need second segment
            var memoryStream = new MemoryStream(buffer);
            strategy.Execute(GetFileModel(filePath), memoryStream);

            // Assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(memoryStream);
            // Skip initiation data + filehash + server response + segment hash + server resposne
            memoryStream.Seek(InitialDataSize + hashSize + 1 + hashSize + 1, SeekOrigin.Begin);

            var firstSegment = reader.ReadBytes(5);
            Assert.Equal(5, firstSegment?.Length);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, firstSegment);

            memoryStream.ReadByte(); // Skip server response
            reader.ReadBytes(hashSize); // Skip hash

            var secondSegment = reader.ReadBytes(5);
            Assert.Equal(5, secondSegment?.Length);
            Assert.Equal(new byte[] { 6, 7, 8, 9, 0 }, secondSegment);
        }

        private static Mock<IFileSystem> MockFileSystem(string filePath)
        {
            var file = new Mock<FileBase>();
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 });
            file.Setup(f => f.OpenRead(filePath)).Returns(stream);
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);
            return fileSystem;
        }

        private FileModel GetFileModel(string filePath)
        {
            return new FileModel
            {
                FileAction = FileAction.Change,
                FullPath = filePath,
                RelativePath = filePath.Split("\\").Last()
            };
        }
    }
}
