using System;
using System.Threading;

namespace Carubbi.Communications.ServerSample;

internal static class Program
{
    private static void Main()
    {
        var service = new EchoService();
        service.Start();

        while (!Console.KeyAvailable)
        {
            Thread.Sleep(10000);
            Console.WriteLine($"{DateTime.Now}: Server running");
        }
    }
}
