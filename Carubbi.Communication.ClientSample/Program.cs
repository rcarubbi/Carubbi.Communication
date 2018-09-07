using Carubbi.Communication.NamedPipe;
using System;
using System.Collections.Generic;
using System.Threading;
using static System.Console;

namespace Carubbi.Communication.ClientSample
{
    class Program
    {
        private static volatile bool _callbackCalled; 

        class Callback : IObserver<string>
        {
            public void OnNext(string value)
            {
                WriteLine(value);
                _callbackCalled = true;
            }

            public void OnError(Exception error)
            {
                WriteLine(error.Message);
                _callbackCalled = true;
            }

            public void OnCompleted()
            {
                WriteLine("Request Completed");
                _callbackCalled = true;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                using (var client = new Client<string, string>("EchoService"))
                {
                    client.BeforeConnect += (sender, eventArgs) => WriteLine("Connecting...");
                    client.AfterEnd += (sender, eventArgs) => WriteLine("Disconected.");
                    client.Subscribe(new Callback());
                    client.Connect();

                    do
                    {
                        WriteLine("Type your message:");
                        var message = ReadLine();

                        client.SendRequest(new List<string> { message });
                        WriteLine("Waiting callback...");
                        while (!_callbackCalled)
                        {
                            Thread.Sleep(100);
                        }

                        _callbackCalled = false;
                        WriteLine("Press any key to continue or esc to exit");
                    } while (ReadKey().Key != ConsoleKey.Escape);
                }
            }
            catch (Exception e)
            {
                WriteLine(e);
                ReadKey();
            }
        }
    }
}
