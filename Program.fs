// Learn more about F# at http://fsharp.org

open Frabbit
open Frabbit.Basic
open FSharp.Control.Reactive
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Threading

let f = ConnectionFactory()
f.UserName <- "guest"
f.Password <- "guest"
f.HostName <- "localhost"
f.VirtualHost <- "/"

let exchangeName = "test1"
let queueName = "test1"
let routingKey = "test"

[<EntryPoint>]
let main argv =

    // create model
    use conn = f.CreateConnection()
    use model = conn.CreateModel()

    // declare rabbit things
    model.ExchangeDeclare(exchangeName, ExchangeType.Direct, false, true)
    let q = model.QueueDeclare(queueName, false, true, true, null)
    model.QueueBind(queueName, exchangeName, routingKey, null)

    let props = model.CreateBasicProperties()
    let address = PublicationAddress(ExchangeType.Direct, exchangeName, routingKey)

    // set up consumer
    let consumer = EventingBasicConsumer(model)
    let deliveryObserver = observeConsumer(consumer)

    use __ = Routines.log(deliveryObserver)
    
    use __ = 
      deliveryObserver
      |> mapBodyString
      |> Observable.map (fun s -> s + " -- Hi again!")
      |> mapStringToPayload(props)
      |> Routines.reinjectN(10, address, model)

    // start consumer
    model.BasicConsume(q.QueueName, true, consumer) |> ignore
    
    // send a message
    seq { yield "Hey!"; yield "Hi there!" }
    |> Observable.ofSeq
    |> mapStringToPayload props
    |> publish(address, model)
    |> Observable.wait

    Thread.Sleep 5000

    0 // return an integer exit code