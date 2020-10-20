using System;
using SWE3.BusinessLogic.Interfaces;

namespace SWE3.Setup
{
    public class Application : IApplication
    {
        private readonly ILogic logic;

        public Application(ILogic logic)
        {
            this.logic = logic;
        }

        public void Run()
        {
            Console.WriteLine("Application started.");
        }
    }
}
