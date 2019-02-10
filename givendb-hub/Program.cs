using System;

namespace GivenDb.Hub
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Hub...");

            using (var server = new Server())
            {
                server.Start();
                Console.WriteLine("Hub is ready");
                Console.ReadLine();
            }

            Console.WriteLine("Hub is closed");
        }
    }
}
