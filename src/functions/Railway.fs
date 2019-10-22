namespace Frabbit

open System
open FSharp.Control.Reactive

/// # Frabbit.Railway
/// Utilities for using railway-oriented programming with observable sequences.
module Railway =

    /// An expression that converts one observable sequence to another
    type Transducer<'i,'o> = IObservable<'i> -> IObservable<'o>

    /// An exception handler that acts as a "railway switch"
    type Switch<'e> = exn -> 'e


    /// ## xform
    /// Applies a transform to the main rail of the observable with the given switch
    let xform<'i,'o,'e> (xf: Transducer<'i,'o>) (ex: Switch<'e>) source =

        let catch = Observable.catchResult (ex >> Observable.single)

        source
        |> Observable.flatmap (fun r ->
            match r with
            | Ok s -> s |> Observable.single |> xf |> catch
            | Error s -> s |> Error |> Observable.single)


    /// ## exform
    /// Applies a transformation to the error rail of the observable with the given switch
    let exform<'i,'ie,'oe> (xf: Transducer<'ie,'oe>) (ex: Switch<'oe>) (source: IObservable<Result<'i, 'ie>>) =

        let unifyErrors = Observable.map (fun r -> match r with Ok e -> Error e | Error e -> Error e)
    
        source 
        |> Observable.flatmap (fun r ->
            match r with
            | Error s -> s |> Ok |> Observable.single |> xform xf ex |> unifyErrors
            | Ok s -> s |> Ok |> Observable.single)
