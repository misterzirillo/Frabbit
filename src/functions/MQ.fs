namespace Frabbit

open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Control.Reactive

module BasicMQ =

  let observeConsumer(consumer: EventingBasicConsumer) =
    Observable.fromEventConversion
      (fun handler -> (fun _ e -> handler e))
      consumer.Received.AddHandler
      consumer.Received.RemoveHandler

  let publish(routing: Routing, model: IModel) =
    let routingKey = match routing.RoutingKey with Some r -> r | None -> "";
    let address = PublicationAddress(null, routing.ExchangeName, routingKey)
    Observable.map (fun p -> model.BasicPublish(address, p.Properties, p.Bytes))