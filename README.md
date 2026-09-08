# eqx-tipping-point

> [!TIP]
> Possible work-around is to update to `AWSSDK.DynamoDBv2` >= `4.0.15`

The issue appears when writing many events quickly to Dynamo.

Start the sandbox:

```bash
docker compose up
```

Write some events:

```bash
dotnet run --project eqx-tipping-point
```

Dump events using the tool:

```bash
# Example
./dump.sh Counter-d3f0323e2cd3458f800a0971049c2163
```

Observe an encoding error:

```
19:48:59 I DynamoDB AwsKeyCredentials "http://localhost:8000/" Timeout 5s Retries 1 {}
19:48:59 I DynamoStore Main Table eqx Archive null {}
19:48:59 I DynamoStore Tip thresholds: 32768b nulle Query paging 10 items {}
19:48:59 I Reading... {streams=["Counter-d3f0323e2cd3458f800a0971049c2163"]}
19:49:00 F Exiting {}
System.InvalidOperationException: Decoder ran into invalid data.
   at System.IO.Compression.BrotliStream.TryDecompress(Span`1 destination, Int32& bytesWritten)
   at System.IO.Compression.BrotliStream.Read(Span`1 buffer)
   at System.IO.Compression.BrotliStream.Read(Byte[] buffer, Int32 offset, Int32 count)
   at System.IO.Stream.CopyTo(Stream destination, Int32 bufferSize)
   at System.IO.Stream.CopyTo(Stream destination)
   at FsCodec.Impl.brotliDecompressTo(Stream output, ReadOnlyMemory`1 data) in /_//src/FsCodec/Encoding.fs:line 34
   at FsCodec.Impl.decode@42-1.Invoke(MemoryStream output, ReadOnlyMemory`1 data)
   at FsCodec.Impl.unpack[a](FSharpFunc`2 alg, a compressedBytes) in /_//src/FsCodec/Encoding.fs:line 37
   at FsCodec.Impl.decode(ValueTuple`2 _arg1) in /_//src/FsCodec/Encoding.fs:line 42
   at FsCodec.Encoding.ToBlob(ValueTuple`2 x) in /_//src/FsCodec/Encoding.fs:line 77
   at <StartupCode$FsCodec>.$Encoding.Compressed@93-1.Invoke(ValueTuple`2 x)
   at <StartupCode$FsCodec>.$FsCodec.MapBodies@125-3.FsCodec.IEventData<'Mapped>.get_Data() in /_//src/FsCodec/FsCodec.fs:line 131
   at Equinox.Tool.Program.Dump.dumpEvents@829-5.Invoke(ITimelineEvent`1 x) in /_//tools/Equinox.Tool/Program.fs:line 840
   at Microsoft.FSharp.Control.AsyncPrimitives.CallThenInvoke[T,TResult](AsyncActivation`1 ctxt, TResult result1, FSharpFunc`2 part2) in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 508
   at Microsoft.FSharp.Control.Trampoline.Execute(FSharpFunc`2 firstAction) in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 112
--- End of stack trace from previous location ---
   at Microsoft.FSharp.Control.AsyncResult`1.Commit() in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 453
   at Microsoft.FSharp.Control.AsyncPrimitives.QueueAsyncAndWaitForResultSynchronously[a](CancellationToken token, FSharpAsync`1 computation, FSharpOption`1 timeout) in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 1133
   at Microsoft.FSharp.Control.AsyncPrimitives.RunSynchronously[T](CancellationToken cancellationToken, FSharpAsync`1 computation, FSharpOption`1 timeout) in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 1160
   at Microsoft.FSharp.Control.FSharpAsync.RunSynchronously[T](FSharpAsync`1 computation, FSharpOption`1 timeout, FSharpOption`1 cancellationToken) in D:\a\_work\1\s\src\FSharp.Core\async.fs:line 1504
   at Equinox.Tool.Program.main(String[] argv) in /_//tools/Equinox.Tool/Program.fs:line 884
```
