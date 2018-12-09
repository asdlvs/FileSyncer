using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FolderSyncer.Core.Network.Protocol;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FolderSyncer.Core.Network.ReceiverStrategies
{
    /// <summary>
    /// Receive change event for file, compare hash of this file if exists, if hashes are similar, do nothing.
    /// If hashes are different or file is not exists, class read file segment by segment.
    /// For each segment it calculate hash and trying to find this hash in cache folder.
    /// If hash was founded, it read value from cache folder, if not - from network.
    /// </summary>
    public class ChangeFileReceiverStrategy : IFileReceiverStrategy
    {
        private readonly IFileSystem _fileSystem;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly IProtocolDialogServer _dialog;
        private readonly IConfiguration _configuration;

        private readonly string _outputDirectory;
        private readonly int _hashSize;

        public ChangeFileReceiverStrategy(IProtocolDialogServer dialogServer,
            IConfiguration configuration,
            HashAlgorithm hashAlgorithm,
            IFileSystem fileSystem)
        {
            if (hashAlgorithm == null) { throw new ArgumentNullException(nameof(hashAlgorithm)); }
            if (dialogServer == null) { throw new ArgumentNullException(nameof(dialogServer)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            _hashAlgorithm = hashAlgorithm;
            _dialog = dialogServer;
            _fileSystem = fileSystem;
            _configuration = configuration;
            _outputDirectory = _configuration["output-directory"];
            _hashSize = hashAlgorithm.HashSize / 8;
        }

        public virtual void ProcessRequest()
        {
            string absolutePath = _fileSystem.Path.Combine(_outputDirectory, _dialog.DialogData.RelativeFilePath);

            if (ClientAndServerFileHashesAreEqual(absolutePath)) { return; }
            CreateRelativeDirectory(absolutePath);
            string tmpFilePath = GenerateTempFilePath(absolutePath);
            RemoveExistingFile(tmpFilePath);

            using (var destinationStream = _fileSystem.File.Open(tmpFilePath, FileMode.CreateNew))
            {
                int takenFromCache = 0;
                int readFromNetwork = 0;
                while (true)
                {
                    string hash = _dialog.ReadValue(_hashSize, ToHex);
                    if (string.IsNullOrWhiteSpace(hash))
                    {
                        break;
                    }

                    string segmentPath = GetSegmentPath(_outputDirectory, hash);
                    CreateRelativeDirectory(segmentPath);

                    byte[] fileSegment;
                    if (_fileSystem.File.Exists(segmentPath))
                    {
                        _dialog.NotifyClient(actionIsNeeded: false);
                        fileSegment = _fileSystem.File.ReadAllBytes(segmentPath);
                        takenFromCache++;
                    }
                    else
                    {
                        _dialog.NotifyClient(actionIsNeeded: true);
                        fileSegment = _dialog.ReadValue(int.Parse(_configuration["segment-size"]), value => value);
                        _fileSystem.File.WriteAllBytes(segmentPath, fileSegment);
                        readFromNetwork++;
                    }

                    destinationStream.Write(fileSegment, 0, fileSegment.Length);
                }
                Log.Information($"For file {absolutePath}, {takenFromCache} segments were taken from cache, {readFromNetwork} were read from network.");
            }

            RemoveExistingFile(absolutePath);
            _fileSystem.File.Move(tmpFilePath, absolutePath);
            Log.Information($"Processing of file {absolutePath} is done.");
        }

        private bool ClientAndServerFileHashesAreEqual(string absolutePath)
        {
            byte[] fileHash = _dialog.ReadValue(_hashSize, value => value);
            if (_fileSystem.File.Exists(absolutePath))
            {
                using (var fs = _fileSystem.File.OpenRead(absolutePath))
                {
                    var currentHash = _hashAlgorithm.ComputeHash(fs);
                    Log.Information($"File {absolutePath} -> Received hash: {ToHex(fileHash)} / Local hash: {ToHex(currentHash)}");
                    if (fileHash.SequenceEqual(currentHash))
                    {
                        Log.Information($"File {absolutePath} wasn't change since last upload. ");
                        _dialog.NotifyClient(false);
                        return true;
                    }
                }
            }

            _dialog.NotifyClient(true);
            return false;
        }

        private void RemoveExistingFile(string absolutePath)
        {
            if (_fileSystem.File.Exists(absolutePath))
            {
                _fileSystem.File.Delete(absolutePath);
            }
        }

        private void CreateRelativeDirectory(string absolutePath)
        {
            var directory = _fileSystem.Path.GetDirectoryName(absolutePath);
            if (!_fileSystem.Directory.Exists(directory))
            {
                _fileSystem.Directory.CreateDirectory(directory ?? throw new InvalidOperationException());
            }
        }

        private string GenerateTempFilePath(string absolutePath)
        {
            return $"{absolutePath}.tmp";
        }

        private string GetSegmentPath(string outputDirectory, string hash)
        {
            return _fileSystem.Path.Combine(outputDirectory, ".syncer", hash);
        }

        private string ToHex(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            foreach (var item in bytes)
            {
                result.Append(item.ToString("x2"));
            }

            return result.ToString();
        }
    }
}
