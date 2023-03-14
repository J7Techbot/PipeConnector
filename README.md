# PipeConnector
This project can be used for easy implementing of NamedPipes.  
For succesfull connection, must be implemented at both sides client/server.

Create new instance of PipeService and call EstablishConnection(). If this func is called at both sides and client/server pipes types are same, it will connect. 
Then just call SendMessage func and catch event invoked at other side.
Hooray!Two-way communication.

Hope to help someone.
