namespace Frabbit

open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Control.Reactive

// High level application activities to be executed asynchronously
module Activities =

    let private setupQueue (model: IModel, qsetup) =
        let rkey = match qsetup.Routing.RoutingKey with Some s -> s | None -> ""
        model.ExchangeDeclare(
            qsetup.Routing.ExchangeName, 
            qsetup.Routing.ExchangeType,
            qsetup.Durable,
            qsetup.AutoDelete)
        model.QueueDeclare(qsetup.QueueName) |> ignore
        model.QueueBind(qsetup.QueueName, qsetup.Routing.ExchangeName, rkey)
    

    // consumes a queue of strings and prints them
    let loggingConsumer (model: IModel, setup: QueueSetup) =
        setupQueue(model, setup)

        let consumer = EventingBasicConsumer(model)
        let injestMapping = consumer |> BasicMQ.observeConsumer |> Routines.mapBodyString

        let happyPath = 
            injestMapping
            |> Routines.chooseOk
            |> Observable.map(printfn "%s")

        let logFormatter = printfn "Logger error: %s"
        let sadPath =
            injestMapping
            |> Routines.chooseError
            |> Observable.map (fun ex -> logFormatter(ex.ToString()))

        let pipeline = Observable.merge happyPath sadPath

        model.BasicConsume(setup.QueueName, true, consumer) |> ignore
        pipeline


    let private resultString preamble r =
        match r with
        | Ok s -> sprintf "OK\t%s\t%s" preamble s
        | Error e -> sprintf "ERROR\t%s\t%s" preamble e


    // observe a result sequence and send log strings to the logger
    let loggingProducer preamble model routing source =
        source
        |> Observable.map (resultString preamble)
        |> Routines.mapStringToPayload null
        |> BasicMQ.publish(routing, model)


    // pairs a queue of strings
    let conversationConsumer (model: IModel, setup: QueueSetup) =
        setupQueue(model, setup)

        let consumer = EventingBasicConsumer(model)
        let injestMapping = consumer |> BasicMQ.observeConsumer |> Routines.mapBodyString

        let pipeline = 
            injestMapping
            |> Routines.chooseOk
            |> Observable.pairwise
            |> Observable.map (fun (a, b) -> sprintf "Buddy 1: %s\tPal 2: %s" a b) 
            |> Observable.map Ok

        model.BasicConsume(setup.QueueName, true, consumer) |> ignore
        pipeline


    let publishStrings (conn: IConnection, strings, routing) =
        let model = conn.CreateModel()
        strings
        |> Observable.ofSeq
        |> Routines.mapStringToPayload null
        |> BasicMQ.publish(routing, model)
        |> Observable.wait