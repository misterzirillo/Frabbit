// Learn more about F# at http://fsharp.org

open Frabbit
open Frabbit.Basic
open FSharp.Control.Reactive
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Threading

// Rabbit connection stuff
let f = ConnectionFactory()
f.UserName <- "guest"
f.Password <- "guest"
f.HostName <- "localhost"
f.VirtualHost <- "/"

let exchangeName = "test1"
let pairQueue = "pair"
let logQueue = "logging"
let pairRouting = "pair"
let logRouting = "log"

[<EntryPoint>]
let main argv =

    // create model
    use conn = f.CreateConnection()
    use model1 = conn.CreateModel()
    use model2 = conn.CreateModel()

    // declare rabbit things
    model1.ExchangeDeclare(exchangeName, ExchangeType.Direct, false, true)
    let q1 = model1.QueueDeclare(pairQueue, false, true, true, null)
    model1.QueueBind(pairQueue, exchangeName, pairRouting, null)

    model2.ExchangeDeclare(exchangeName, ExchangeType.Direct, false, true)
    let q2 = model1.QueueDeclare(logQueue, false, true, true, null)
    model2.QueueBind(logQueue, exchangeName, logRouting, null)

    let respondAddress = PublicationAddress(ExchangeType.Direct, exchangeName, pairRouting)
    let logAddress = PublicationAddress(ExchangeType.Direct, exchangeName, logRouting)

    // set up echo consumer
    let echoConsumer = EventingBasicConsumer(model1)
    let echoObserver = observeConsumer(echoConsumer)

    let mapEcho deliveries =
      mapBodyString deliveries
      |> Observable.choose (fun e -> match e with Ok s -> Some s | _ -> None)
      |> Observable.pairwise
      |> Observable.map (fun (a, b) -> sprintf "Buddy 1: %s\tPal 2: %s" a b) 
    
    // re-send deliveries with echos
    use __ = 
      echoObserver
      |> mapEcho
      |> mapStringToPayload null
      |> publish(logAddress, model2)
      |> Observable.subscribe ignore

    // set up log consumer
    let logConsumer = EventingBasicConsumer(model2)
    let logObserver = observeConsumer(logConsumer)

    let loggingString e =
      match e with
        | Ok s -> sprintf "[LOG]\t%s" s
        | Error s -> sprintf "[ERROR]\t%s" s

    // log event content
    use __ =
      logObserver
      |> mapBodyString
      |> Observable.map loggingString
      |> Routines.log

    // start consumers
    model1.BasicConsume(q1.QueueName, true, echoConsumer) |> ignore
    model2.BasicConsume(q2.QueueName, true, logConsumer) |> ignore
    
    // queue some greetings
    seq {
      yield "Hey Buddy!"
      yield "Hey Pal!!"
      yield "Hi Friend!"
      yield "Hey Bro or Sis!!"
    }
    |> Observable.ofSeq
    |> mapStringToPayload null
    |> publish(respondAddress, model1)
    |> Observable.wait // blocks until all messages are published

    Thread.Sleep 1000

    0 // return an integer exit code