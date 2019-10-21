namespace Frabbit

open RabbitMQ.Client

[<AutoOpen>]
module Types =

    type Routing = {
        ExchangeName: string
        ExchangeType: string
        RoutingKey: string option
    }

    type BasicPayload = {
        Properties: IBasicProperties
        Bytes: byte[]
    }

    type QueueSetup = {
        Routing: Routing
        QueueName: string
        AutoDelete: bool
        Durable: bool
        Exclusive: bool
    }