# FolderSyncer

Folder syncer is lightweight tool for one-side folder synchronization.

# How it's working!

FolderSyncer doesn't use any highlevel libraries. Everything is based on some funny proprietary protocol. The idea of this protocol is to make life simple.

### Client side.
`DirectoryMonitor` - Initializes parsing of working directory and starts monitoring of this directory. Each event is sent to Channel.

`Channel` - FileSystemWatcher can generate multiple events for single file/folder change. That's why it could be a good idea to generate snapshot once per period and process exactly this snapshot.

`Transfer manager`- Dispatcher class that reads events from channel, creates task for each event, chooses strategy and execute it.

STRATEGIES:
`ChangeTransferStrategy` - Send hash of file to the server and wait for answer. If answer is true, starting uploading file segment by segment. For each segment it calcualte its hash, send this hash to server and wait for answer. If answer is try - upload segment, if fasle - skip it.

`DeleteTransferStrategy` - Send delete event to server.

`RenameTransferStrategy` - Send rename event to server.

### Server side.
`File Receiver` - Dispatcher class that creates task for each received connection, choose strategy execute it.

STRATEGIES
`ChangeReceiverStrategy` - Receive change event for file, compare hash of this file if exists, if hashes are similar, do nothing. If hashes are different or file is not exists, class read file segment by segment. For each segment it calculate hash and trying to find this hash in cache folder. If hash was founded, it read value from cache folder, if not - from network.

`CreateReceiverStrategy` - Check if event was received for file or folder. If it was received for file - it execute ChangeReceverStrategy, if for folder - just create it.

`DeleteReceiverStrategy` - Delete file or folder.

`RenameReceiverStrategy` - Rename file or folder.

### To Run
To run client application use `directory-path={folder to monitor}`
appsettings.json:
```
{
  "ip": "127.0.0.1",
  "port": 12345,
  "segment-size": 1048576,
  "degree-of-parallelism": 10
 }
```

To run server application use `output-directory={output directory}`

```
{
  "ip": "127.0.0.1",
  "port": 12345,
  "segment-size": 1048576
}
```

