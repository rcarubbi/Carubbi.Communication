# Carubbi.Communication

[![NuGet](https://img.shields.io/nuget/v/Carubbi.Communication)](https://www.nuget.org/packages/Carubbi.Communication)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Carubbi.Communication)](https://www.nuget.org/packages/Carubbi.Communication)

Implementation of named pipes stream to abstract the complexity. Allows you to use inter-process communication in a simple way.

## Projects

| Project | Package |
|---------|---------|
| `Carubbi.Communication` | `Carubbi.Communication` |

Target framework: `net10.0-windows` (Windows only). Requires .NET 10 SDK.

> **Wire version compatibility**: each library version uses its own wire protocol.
> Client and Server MUST run the same version (v2.1.0). Do not mix a v2.1.0 peer
> with a v2.0.0 peer — the transport and framing changed.

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

The pipe names default to `{processName}_SERVER_PIPE` (client → server) and `{processName}_CALLBACK_PIPE` (server → client). Both the server and the client must use the same `processName`, or pass explicit `serverPipeName`/`callbackPipeName` values.

## Serialization formats

Messages are serialized with a pluggable strategy. Choose it on both the server and the client constructors via the `MessageFormat` argument (default `MessageFormat.Xml`):

| Format | Notes |
|--------|-------|
| `MessageFormat.Xml` | Default. Uses `System.Xml.Serialization.XmlSerializer` (BCL). Any public POCO. |
| `MessageFormat.Json` | Uses `System.Text.Json` (BCL). Any public POCO. |
| `MessageFormat.Binary` | Custom reflection-based encoder (`BinaryWriter`/`BinaryReader`) over public read/write properties. Supports primitives, `string`, `char`, `DateTime`, `TimeSpan`, `Guid`, `decimal`, `enum`, `byte[]`, arrays and `List<T>`. |
| `MessageFormat.Protobuf` | Uses `protobuf-net`. Message types must be annotated with `[ProtoContract]` / `[ProtoMember]`. |

```csharp
// Both peers must use the same format.
var server = new EchoService(format: MessageFormat.Protobuf);
var client = new Client<string, string>("EchoService", format: MessageFormat.Protobuf);
```

For full control, pass your own serializers implementing `IMessageSerializer<T>`:

```csharp
var client = new Client<string, string>(
    customRequestSerializer,   // IMessageSerializer<List<string>>
    customResponseSerializer); // IMessageSerializer<string>
```

## Network / cross-machine (".")

By default both peers connect on the local machine (`"."`). To communicate across the network, pass the remote peer's host on both sides:

- **Client** → `serverPipePath` = the machine hosting the `Server`.
- **Server** → `callbackClientPath` = the machine hosting the `Client`.

```csharp
var client = new Client<string, string>("EchoService", serverPipePath: "192.168.1.50");
var server = new EchoService(callbackClientPath: "192.168.1.60");
```

Hosted pipes default to `AllowEveryone()` (`PipeSecurity` granting the `Everyone` SID read/write), which is required for remote peers that run under a different account. Use `NamedPipeSecurity.AllowEveryone()` (default), `NamedPipeSecurity.AllowCurrentUser()`, or build a custom `PipeSecurity` and pass it as `serverPipeSecurity` / `callbackPipeSecurity`.

For security reasons `.NET 10` removed the `PipeSecurity` overloads from the `NamedPipeServerStream` constructor; this library uses `NamedPipeServerStreamAcl` (part of the `net10.0-windows` framework, no extra package) to apply pipe security.

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
