using System;

namespace GivenDb.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client application...");

            using(var dbRequester = new TestSessionFactory())
            {
                dbRequester.Start();
                Console.WriteLine("Client is ready");
                               
                dbRequester.CreateTestSession("mssql");

                Console.ReadLine();
            }

            Console.WriteLine("Client is closed");
        }
    }
}
