open System
open Amazon.DynamoDBv2
open Equinox
open Equinox.DynamoStore
open FsCodec
open Serilog

type Add =
  {
    Increase : int
    RequestedBy : string
    Notes : string
  }

type Subtract =
  {
    Decrease : int
    RequestedBy : string
  }

type Event =
  | Add of Add
  | Subtract of Subtract
  interface TypeShape.UnionContract.IUnionContract


module Serialization =

  open System.Text.Json

  let codec : IEventCodec<Event, ReadOnlyMemory<byte>, unit> =
    let options = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    FsCodec.SystemTextJson.Codec.Create(options = options)


module Fold =

  type State =
    {
      Count : int
    }

  let evolve (state : State) (event : Event) : State =

    let invalid () =
      failwith $"Unexpected evolution `%A{event}` from `%A{state}`"

    if state.Count > Int32.MaxValue then
      invalid()
    else
      match event with
      | Add x -> { Count = state.Count + x.Increase }
      | Subtract x -> { Count = state.Count - x.Decrease }

  let initial : State = { Count = 0 }

  let fold state events = Array.fold evolve state events


module Decisions =

  open Fold

  type AddError =
    | MaxReached

  let tryAdd (notes : string) (requestedBy : string) (count : int) (state : State) : Result<unit, AddError> * Event array =
    if state.Count > 1_000_000 then
      Error MaxReached, [||]
    else
      Ok (), [| Add { Increase = count; RequestedBy = requestedBy; Notes = notes } |]

  type SubtractError =
    | MinReached

  let trySubtract (requestedBy : string) (count : int) (state : State) : Result<unit, SubtractError> * Event array =
    if state.Count < 0 then
      Error MinReached, [||]
    else
      Ok (), [| Subtract { Decrease = count; RequestedBy = requestedBy } |]


[<RequireQualifiedAccess>]
module Counter =

  type Service (resolve : Guid -> Equinox.Decider<Event, Fold.State>) =

    member _.TryAdd(id : Guid, requestedBy : string, notes : string, count : int) =
      let decider = resolve id
      decider.Transact(Decisions.tryAdd notes requestedBy count)

    member _.TrySubtract(id : Guid, requestedBy : string, count : int) =
      let decider = resolve id
      decider.Transact(Decisions.trySubtract requestedBy count)

    member _.TryFetch(id : Guid) =
      let decider = resolve id
      decider.Query(fun x -> x)

  [<Literal>]
  let Category = "Counter"

  let streamID =
    StreamId.gen (fun (g : Guid) -> g.ToString("N"))

  let create (resolve : _ -> _ -> _) : Service =
    Service(streamID >> resolve Category)


[<EntryPoint>]
let main _ =
  async {
    let log = LoggerConfiguration().WriteTo.Console().CreateLogger()

    let awsAccessKeyID = "abc"
    let awsSecretAccessKey = "def"

    let dynamo =
      let config = AmazonDynamoDBConfig()
      config.ServiceURL <- "http://localhost:8000"
      config.UseHttp <- true
      config.AuthenticationRegion <- "eu-west-2"
      new AmazonDynamoDBClient(awsAccessKeyID, awsSecretAccessKey, config)

    let service : Counter.Service =
      let dynamoStoreClient = DynamoStoreClient(dynamo)
      let context = DynamoStoreContext(dynamoStoreClient, "eqx")
      let accessStrategy = AccessStrategy.Unoptimized
      let cache = Cache("counter", sizeMb = 50)
      let caching = CachingStrategy.SlidingWindow(cache, TimeSpan.FromMinutes(20.))
      let codec = Encoder.Compressed Serialization.codec
      (fun category ->
        DynamoStoreCategory(
          context,
          category,
          codec,
          Fold.fold,
          Fold.initial,
          accessStrategy,
          caching)
        |> Decider.forStream log)
        |> Counter.create

    let counterID = Guid.NewGuid()

    log.Information $"Counter ID: %A{counterID}"

    let random = Random()

    for _ = 1 to 100 do
      for i = 1 to 100 do
        do!
          service.TryAdd(counterID, "user", "add to account", random.Next(i))
          |> Async.Ignore

      for i = 1 to 100 do
        do!
          service.TrySubtract(counterID, "user", random.Next(i))
          |> Async.Ignore

    let dumpCommand = sprintf "./dump.sh Counter-%s" (counterID.ToString("N"))

    log.Information dumpCommand
  }
  |> Async.RunSynchronously

  0
