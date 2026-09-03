# Carubbi.Communication

[![NuGet](https://img.shields.io/nuget/v/Carubbi.Communication)](https://www.nuget.org/packages/Carubbi.Communication)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Carubbi.Communication)](https://www.nuget.org/packages/Carubbi.Communication)

Implementation of named pipes stream to abstract the complexity. Allows you to use inter-process communication in a simple way.

## Projects

| Project | Package |
|---------|---------|
| `Carubbi.Communication` | `Carubbi.Communication` |

Target framework: `net10.0` (Windows). Requires .NET 10 SDK.

## Usage

### Server

Create a service class that inherits from `Server<TRequestMessage, TResponseMessage>` and implements `ProcessRequest` and `BeforeStart`:

```csharp
using Carubbi.Communication.NamedPipe;

public class EchoService : Server<string, string>
{
    public EchoService() : base(nameof(EchoService))
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

Start the service in your server app:

```csharp
var service = new EchoService();
service.Start();
```

### Client

Implement an `IObserver<TResponseMessage>` to receive the responses:

```csharp
public class EchoServiceCallback : IObserver<string>
{
    public void OnNext(string value) => Console.WriteLine(value);
    public void OnError(Exception error) => Console.WriteLine(error.Message);
    public void OnCompleted() => Console.WriteLine("Request Completed");
}
```

Send requests from your client app:

```csharp
using Carubbi.Communication.NamedPipe;

using (var client = new Client<string, string>("EchoService"))
{
    client.BeforeConnect += (sender, eventArgs) => Console.WriteLine("Connecting...");
    client.AfterEnd += (sender, eventArgs) => Console.WriteLine("Disconnected.");
    client.Subscribe(new EchoServiceCallback());
    client.Connect();

    Console.WriteLine("Type your message:");
    var message = Console.ReadLine();
    client.SendRequest(new List<string> { message });
}
```

The pipe names default to `{processName}_SERVER_PIPE` (server to client) and `{processName}_CALLBACK_PIPE` (client to server). Both the server and the client must use the same `processName`, or pass explicit `serverPipeName`/`callbackPipeName` values.

## Samples

Run the server and the client samples to see a fully working round-trip:

```shell
dotnet run --project samples/Carubbi.Communications.ServerSample -c Release
dotnet run --project samples/Carubbi.Communication.ClientSample -c Release
```

## Building and testing locally

Prerequisites: .NET SDK 10.

```shell
dotnet build Carubbi.Communication.slnx -c Release
dotnet run --project tests/Carubbi.Communication.Tests/Carubbi.Communication.Tests.csproj -c Release
```

## Releasing

Pushing a tag `v*` (for example `v2.0.0`) triggers the `publish` workflow, which builds in Release mode, packs the library and publishes it to nuget.org using GitHub Actions trusted publishing.
