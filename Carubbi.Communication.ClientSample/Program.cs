using Carubbi.Communication.NamedPipe;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Carubbi.Communication.ClientSample
{
    class Program
    {
        private static volatile bool _callbackCalled; 

        public class EchoServiceCallback : IObserver<string>
        {
            public void OnNext(string value)
            {
                Console.WriteLine(value);
                _callbackCalled = true;
            }

            public void OnError(Exception error)
            {
                Console.WriteLine(error.Message);
                _callbackCalled = true;
            }

            public void OnCompleted()
            {
                Console.WriteLine("Request Completed");
                _callbackCalled = true;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                using (var client = new Client<string, string>("EchoService"))
                {
                    client.BeforeConnect += (sender, eventArgs) => Console.WriteLine("Connecting...");
                    client.AfterEnd += (sender, eventArgs) => Console.WriteLine("Disconected.");
                    client.Subscribe(new EchoServiceCallback());
                    client.Connect();

                    do
                    {
                        Console.WriteLine("Type your message:");
                        var message = Console.ReadLine();

                        client.SendRequest(new List<string> { message });
                        Console.WriteLine("Waiting callback...");
                        while (!_callbackCalled)
                        {
                            Thread.Sleep(100);
                        }

                        _callbackCalled = false;
                        Console.WriteLine("Press any key to continue or esc to exit");
                    } while (Console.ReadKey().Key != ConsoleKey.Escape);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
