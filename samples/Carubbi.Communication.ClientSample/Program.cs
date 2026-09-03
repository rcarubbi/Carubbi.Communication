using System;
using System.Collections.Generic;
using System.Threading;
using Carubbi.Communication.NamedPipe;

namespace Carubbi.Communication.ClientSample;

internal static class Program
{
    private static void Main()
    {
        try
        {
            using (var client = new Client<string, string>("EchoService"))
            {
                var callbackFired = new ManualResetEventSlim(false);

                client.BeforeConnect += (_, _) => Console.WriteLine("Connecting...");
                client.AfterEnd += (_, _) => Console.WriteLine("Disconnected.");
                client.Subscribe(new EchoServiceCallback(callbackFired));
                client.Connect();

                do
                {
                    Console.WriteLine("Type your message:");
                    var message = Console.ReadLine() ?? string.Empty;

                    callbackFired.Reset();
                    client.SendRequest(new List<string> { message });
                    Console.WriteLine("Waiting callback...");
                    callbackFired.Wait();

                    Console.WriteLine("Press any key to continue or ESC to exit");
                } while (Console.ReadKey().Key != ConsoleKey.Escape);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.ReadKey();
        }
    }

    private sealed class EchoServiceCallback : IObserver<string>
    {
        private readonly ManualResetEventSlim _callbackFired;

        public EchoServiceCallback(ManualResetEventSlim callbackFired)
        {
            _callbackFired = callbackFired;
        }

        public void OnNext(string value)
        {
            Console.WriteLine(value);
            _callbackFired.Set();
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.Message);
            _callbackFired.Set();
        }

        public void OnCompleted()
        {
            Console.WriteLine("Request Completed");
            _callbackFired.Set();
        }
    }
}
