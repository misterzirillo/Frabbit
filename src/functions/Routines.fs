namespace Frabbit

open FSharp.Control.Reactive
open System.Text
open RabbitMQ.Client.Events

// Common observable operations
module Routines =

  let tee choice o =
    let t = Observable.choose choice o
    (t, o)

  let mapBodyString o =
    Observable.map (fun (e: BasicDeliverEventArgs) ->
      try
        Ok(Encoding.UTF8.GetString(e.Body))
      with
        | e -> Error(e)) o

  let mapStringToPayload props = 
    Observable.map (fun (s: string) -> { Properties = props; Bytes = Encoding.UTF8.GetBytes(s) })
  
  let chooseOk o =
    o |> Observable.choose (fun e -> match e with Ok s -> Some s | _ -> None)

  let chooseError o =
    o |> Observable.choose (fun e -> match e with Error s -> Some s | _ -> None)