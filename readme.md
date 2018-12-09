# FolderSyncer

Folder syncer is lightweight tool for one-side folder synchronization.

# How it's working!

FolderSyncer doesn't use any highlevel libraries. Everything is based on some funny proprietary protocol. The idea of this protocol is to make life simple.

### Protocol
After establishing connection, client send header that is common for all actions:

- Version: 1 byte
- Action: 1 byte
- FileType: 1 byte
- Filename size: 4 byte
- Filename: ^ bytes

If action is `delete`, not further information is required.
If action is `rename`, client send:

- New filename size: 4 byte
- New Filename: ^ bytes

If action is `change` or `create` for file:

- Hash: 16 bytes

After sending hash, client turn into reading mode, and waiting for answer from server.
Server should answer 0 if current hash doesn't exist, and 1 is hash exists.
If answer is 1, execution will be stopped. Otherwise client read segment with configurable size, calculate its hash and send it to server.

- Hash: 16 bytes

After sending hash, client turn into reading mode, and waiting for answer from server.
Server should answer 0 if current hash doesn't exist, and 1 is hash exists.
If answer is 1 client skip current segment and start processing the next one, if answer is 0, client start sending byte of current segment.

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

