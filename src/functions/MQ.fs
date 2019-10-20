namespace Frabbit

open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Control.Reactive
open System.Text

module Basic =

  let observeConsumer(consumer: EventingBasicConsumer) =
    Observable.fromEventConversion
      (fun handler -> (fun _ e -> handler e))
      consumer.Received.AddHandler
      consumer.Received.RemoveHandler

  let publish(address: PublicationAddress, model: IModel) =
    Observable.map (fun p -> model.BasicPublish(address, p.properties, p.bytes))

  let mapBodyString o =
    Observable.map (fun (e: BasicDeliverEventArgs) ->
      try
        Ok(Encoding.UTF8.GetString(e.Body))
      with
        | _ -> Error("Parsing error")) o

  let mapStringToPayload props = 
    Observable.map (fun (s: string) -> { properties = props; bytes = Encoding.UTF8.GetBytes(s) })