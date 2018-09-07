using System;
using System.Threading;

namespace Carubbi.Communications.ServerSample
{
    class Program
    {
        static void Main(string[] args)
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
}
