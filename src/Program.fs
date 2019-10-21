// Learn more about F# at http://fsharp.org

open Frabbit
open RabbitMQ.Client
open System.Threading

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
    use __ =
      Activities.loggingConsumer(loggingChannel, loggingQueue)
      |> Observable.subscribe ignore

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
    Activities.publishStrings(
      conn,
      seq {
        yield "Hey Buddy!"
        yield "Hey Pal!!"
        yield "Hi Friend!"
        yield "Hey Bro or Sis!!"
      },
      conversationRouting)

    Thread.Sleep 1000

    0 // return an integer exit code