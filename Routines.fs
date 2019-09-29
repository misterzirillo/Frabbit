namespace Frabbit

open System
open FSharp.Control.Reactive
open RabbitMQ.Client.Events
open RabbitMQ.Client

module Routines =

  let log (consumer: IObservable<BasicDeliverEventArgs>) =
    consumer
    |> Basic.mapBodyString
    |> Observable.subscribe (fun e -> printfn "%s" e)

  let reinjectN(n: int, address, model: IModel) =
    (fun p -> 
      p
      |> Observable.take n
      |> Basic.publish(address, model)
      |> Observable.subscribe ignore)
    