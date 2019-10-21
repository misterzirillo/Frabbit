# Frabbit
Messing around with F#, reactive programming, and RabbitMQ.

__Requirements:__
- Dotnet Core >=2.x
- docker (or some local rabbit instance)

__To test__
1. if needed start rabbit - run `./start-rabbit.sh`
2. `dotnet run`

### What does it do?
This program creates reactive streams from RabbitMQ queues to perform operations on those queues.
Overall the flow of the system is:

1. Given a stream of values
2. Pair each string with the last, mapping the joined pairs in some way. (This is the interesting part)
3. Log each thing
