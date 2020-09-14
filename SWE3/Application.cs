using System;
using System.Collections.Generic;
using System.Text;
using ISWE3;

namespace SWE3
{
    public class Application : IApplication
    {
        private IBusinessLogic businessLogic;

        public Application(IBusinessLogic businessLogic)
        {
            this.businessLogic = businessLogic;
        }

        public void Run()
        {
            businessLogic.MyFunction();
        }
    }
}
