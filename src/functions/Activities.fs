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


    let private resultString tag r =
        match r with
        | Ok s -> sprintf "[OK]\t[%s]\t%s" tag s
        | Error e -> sprintf "[ERROR]\t[%s]\t%s" tag e
    
    let private formatException (e: exn) = e.Message

    
    let loggingTag queue =
        let rkey = getRoutingKey(queue.Routing)
        sprintf "%s:%s:%s" queue.Routing.ExchangeName queue.QueueName rkey


    // consumes a queue of strings and prints them
    let loggingConsumer (model: IModel, setup: QueueSetup) =
        setupQueue(model, setup)

        let consumer = EventingBasicConsumer(model)
        let injestMapping = consumer |> BasicMQ.observeConsumer |> Routines.mapBodyString
        let logFormatter = setup |> loggingTag |> resultString

        let happyPath = 
            injestMapping
            |> Routines.chooseOk
            |> Observable.map(printfn "%s")

        let sadPath =
            injestMapping
            |> Routines.chooseError
            |> Observable.map (formatException >> Error >> logFormatter >> printfn "%s")

        let pipeline = Observable.merge happyPath sadPath

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

        model.BasicConsume(setup.QueueName, true, consumer) |> ignore
        pipeline


    let publishStrings (conn: IConnection, strings, routing) =
        let model = conn.CreateModel()
        strings
        |> Observable.ofSeq
        |> Routines.mapStringToPayload null
        |> BasicMQ.publish(routing, model)
        |> Observable.wait