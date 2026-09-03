using System;
using System.Threading;

namespace Carubbi.Communications.ServerSample;

internal static class Program
{
    private static void Main(string[] args)
    {
        var callbackClientPath = args.Length > 0 ? args[0] : ".";

        var service = new EchoService(callbackClientPath);
        service.Start();

        while (!Console.KeyAvailable)
        {
            Thread.Sleep(10000);
            Console.WriteLine($"{DateTime.Now}: Server running");
        }
    }
}
