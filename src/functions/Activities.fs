namespace Frabbit

open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Control.Reactive

// High level application activities to be executed asynchronously
module Activities =

    let private getRoutingKey routing =
        match routing.RoutingKey with Some s -> s | None -> ""

    let private setupQueue (model: IModel, qsetup) =
        model.ExchangeDeclare(
            qsetup.Routing.ExchangeName, 
            qsetup.Routing.ExchangeType,
            qsetup.Durable,
            qsetup.AutoDelete)
        model.QueueDeclare(qsetup.QueueName) |> ignore
        model.QueueBind(qsetup.QueueName, qsetup.Routing.ExchangeName, getRoutingKey(qsetup.Routing))

    
    let private exnMessage (e: exn) = e.Message
    let private fmtError = sprintf "[ERROR]\t[%s]\t%s"
    let private fmtOk = sprintf "[OK]\t[%s]\t%s"

    let private resultString tag r =
        match r with
        | Ok s -> fmtOk tag s
        | Error e -> e |> exnMessage |> fmtError tag
    
    
    let loggingTag queue =
        let rkey = getRoutingKey(queue.Routing)
        sprintf "%s:%s:%s" queue.Routing.ExchangeName queue.QueueName rkey


    // consumes a queue of strings and prints them
    let loggingConsumer (model: IModel, setup: QueueSetup) =
        setupQueue(model, setup)

        let consumer = EventingBasicConsumer(model)
        let lt = loggingTag(setup)

        let pipeline =
            consumer
            |> BasicMQ.observeConsumer
            |> Routines.mapBodyString
            |> Railway.exform (Observable.map exnMessage) exnMessage
            |> Observable.map (fun e -> match e with Ok s -> s | Error s -> fmtError lt s)
            |> Observable.map (printfn "%s")

        model.BasicConsume(setup.QueueName, true, consumer) |> ignore
        pipeline


    // observe a result sequence and send log strings to the logger
    let loggingProducer tag model routing source =
        source
        |> Observable.map (resultString tag)
        |> Routines.mapStringToPayload null
        |> BasicMQ.publish(routing, model)


    // pairs a queue of strings
    let conversationConsumer (model: IModel, setup: QueueSetup) =
        setupQueue(model, setup)

        let consumer = EventingBasicConsumer(model)
        let injestMapping = consumer |> BasicMQ.observeConsumer |> Routines.mapBodyString

        let speechFormat (a, b) = sprintf "Buddy 1: %s\tPal 2: %s" a b
        let pipeline = 
            injestMapping
            |> Routines.chooseOk
            |> Observable.pairwise
            |> Observable.map speechFormat
            |> Observable.map Ok

        model.BasicConsume(setup.QueueName, setup.AutoAck, consumer) |> ignore
        pipeline


    let publishStrings (conn: IConnection, strings, routing) =
        let model = conn.CreateModel()
        strings
        |> Observable.ofSeq
        |> Routines.mapStringToPayload null
        |> BasicMQ.publish(routing, model)
        |> Observable.wait