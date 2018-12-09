using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FolderSyncer.Core.Monitor
{
    /// <summary>
    /// FileSystemWatcher can generate multiple events for single file/folder change.
    /// That's why it could be a good idea to generate snapshot once per period
    /// and process exactly this snapshot.
    /// </summary>
    public class Channel : IChannel
    {
        private readonly object _writerLocker = new object();
        private readonly Dictionary<string, LinkedList<FileModel>> _inputContainer;
        private readonly BlockingCollection<FileModel> _outputContainer;

        public Channel(ITimer timer)
        {
            if (timer == null) { throw new ArgumentNullException(nameof(timer)); }

            _inputContainer = new Dictionary<string, LinkedList<FileModel>>();
            _outputContainer = new BlockingCollection<FileModel>();
            timer.Elapsed += (sender, args) => GenerateSnapshot();
            timer.Start();
        }

        public void AddFile(FileModel fileModel)
        {
            lock (_writerLocker)
            {
                string key = fileModel.RelativePath;
                if (!_inputContainer.ContainsKey(key))
                {
                    _inputContainer[key] = new LinkedList<FileModel>();
                }

                _inputContainer[key].AddLast(fileModel);
            }
        }

        private void GenerateSnapshot()
        {
            lock (_writerLocker)
            {
                foreach (var pair in _inputContainer)
                {
                    var eventsList = pair.Value;
                    
                    var lastNode = eventsList.Last;
                    var previousNode = lastNode.Previous;

                    while (lastNode.Value.FileAction == FileAction.Rename && previousNode != null)
                    {
                        lastNode = previousNode;
                    }

                    while (lastNode != null)
                    {
                        _outputContainer.Add(lastNode.Value);
                        lastNode = lastNode.Next;
                    }
                }
                _inputContainer.Clear();
            }
        }

        public IEnumerable<FileModel> GetFile()
        {
            foreach (var fileModel in _outputContainer.GetConsumingEnumerable())
            {
                yield return fileModel;
            }
        }
    }
}

