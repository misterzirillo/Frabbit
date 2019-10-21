// Learn more about F# at http://fsharp.org

open Frabbit
open RabbitMQ.Client
open FSharp.Control.Reactive

// Rabbit connection stuff
let f = ConnectionFactory()
f.UserName <- "guest"
f.Password <- "guest"
f.HostName <- "localhost"
f.VirtualHost <- "/"

[<EntryPoint>]
let main argv =

    // create connection
    use conn = f.CreateConnection()

    // first set up the logger - it just prints things
    let logRouting = {
      ExchangeName = "logging"
      RoutingKey = None
      ExchangeType = ExchangeType.Fanout
    }
    
    let loggingQueue = {
      Routing = logRouting
      QueueName = "logging1"
      AutoDelete = true
      Durable = false
      Exclusive = true
    }

    use loggingChannel = conn.CreateModel()
    let loggingStream = Activities.loggingConsumer(loggingChannel, loggingQueue)

    // set up a consumer that will simulate a "conversation"
    let conversationRouting = {
      ExchangeName = "conversation"
      RoutingKey = Some "testconversation"
      ExchangeType = ExchangeType.Direct
    }

    let conversationQueue = {
      Routing = conversationRouting
      QueueName = "conversation1"
      AutoDelete = true
      Durable = false
      Exclusive = true
    }

    use conversationChannel = conn.CreateModel()
    use __ =
      Activities.conversationConsumer(conversationChannel, conversationQueue)
      |> Activities.loggingProducer "conversation" conversationChannel logRouting
      |> Observable.subscribe ignore

    
    // queue some greetings
    let greetings =
      seq {
        yield "Hey Buddy!"
        yield "Hey Pal!!"
        yield "Hi Friend!"
        yield "Hey Bro or Sis!!"
      }

    Activities.publishStrings(conn, greetings, conversationRouting)

    // wait on the logger to receive the expected output
    loggingStream
    |> Observable.take (Seq.length(greetings) - 1)
    |> Observable.wait
    |> ignore

    0 // return an integer exit code