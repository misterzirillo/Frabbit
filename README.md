# Frabbit
Messing around with F#, reactive programmin, and RabbitMQ.

__Requirements:__
- Dotnet Core 3.0
- docker

__To test__
1. run `./start-rabbit.sh`
2. `dotnet run`

### What does it do?
This program creates reactive streams from RabbitMQ queues to perform operations on those queues.
Overall the flow of the system is:

1. Given a stream of values
2. Pair each string with the last, putting the concattenated pairs on the logging channel. (This is the interesting part)
3. Log each string
