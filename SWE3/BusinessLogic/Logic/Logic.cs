using Serilog;
using SWE3.BusinessLogic.Interfaces;
using SWE3.DataAccess.Interfaces;

namespace SWE3.BusinessLogic
{
    public class Logic : ILogic
    {
        private readonly ILogger logger;
        private readonly IDataHelper dataHelper;
        private readonly IDataTransmitter dataTransmitter;
        private readonly IDataReceiver dataReceiver;

        public Logic(ILogger logger, IDataHelper dataHelper, IDataTransmitter dataTransmitter, IDataReceiver dataReceiver)
        {
            this.logger = logger;
            this.dataHelper = dataHelper;
            this.dataTransmitter = dataTransmitter;
            this.dataReceiver = dataReceiver;
        }
    }
}