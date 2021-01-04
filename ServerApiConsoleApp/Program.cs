using GeneratorApiLibrary;
using GeneratorApiLibrary.Model;
using System;

namespace ServerApiConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new GeneratorApi("http://localhost:8080");


            var user = api.SignIn("admin", "admin").GetAwaiter().GetResult();
            Console.WriteLine("User token: " + user.token);
        }
    }
}
