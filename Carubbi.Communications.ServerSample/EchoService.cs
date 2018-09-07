using System;
using Carubbi.Communication.NamedPipe;

namespace Carubbi.Communications.ServerSample
{
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
}
