namespace Frabbit

open RabbitMQ.Client

[<AutoOpen>]
module Types =

    type BasicPayload = {
        properties: IBasicProperties
        bytes: byte[]
    }