namespace Frabbit

open System
open FSharp.Control.Reactive
open RabbitMQ.Client

module Routines =

  let log (consumer: IObservable<string>) =
    consumer
    |> Observable.subscribe (fun e -> printfn "%s" e)

  let injectN(n: int, address, model: IModel) =
    (fun p -> 
      p
      |> Observable.take n
      |> Basic.publish(address, model)
      |> Observable.subscribe ignore)
    