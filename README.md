# Carubbi.Communication
Implementation of named pipes stream to abstract the complexity. Allow you to use inter-process communication in a simple way

## 1. Server sample:

> Echo Service:
```csharp
public class EchoService : Server<string, string>
{
    public EchoService()
    : base(nameof(EchoService))
    {

    }

    protected override string ProcessRequest(string requestMessage)
    {
        Console.WriteLine($"Message received: {requestMessage}");
        return $"Message sent: {requestMessage}";
    }

    protected override void BeforeStart()
    {
        Console.WriteLine("Server starting...");
    }
}
```

> Server App:
```csharp
var service = new EchoService();
service.Start();
 ```
 
 ## 2. Client Sample
 
 > Service Callback:
 ```csharp
public class EchoServiceCallback : IObserver<string>
{
    public void OnNext(string value)
    {
        Console.WriteLine(value);
    }

    public void OnError(Exception error)
    {
        Console.WriteLine(error.Message);
    }

    public void OnCompleted()
    {
        Console.WriteLine("Request Completed");
    }
}
```

> Client App:
```csharp
using (var client = new Client<string, string>("EchoService"))
{
    client.BeforeConnect += (sender, eventArgs) => Console.WriteLine("Connecting...");
    client.AfterEnd += (sender, eventArgs) => Console.WriteLine("Disconected.");
    client.Subscribe(new EchoServiceCallback());
    client.Connect();
   
    Console.WriteLine("Type your message:");
    var message = Console.ReadLine();
    client.SendRequest(new List<string> { message });
} 
```
        
 
    
