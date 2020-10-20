using Serilog;
using SWE3.BusinessLogic.Interfaces;
using SWE3.DataAccess.Interfaces;

namespace SWE3.BusinessLogic
{
    public class Logic : ILogic
    {
        private readonly ILogger logger;
        private readonly IDataHelper dataHelper;
        private readonly ISqlMapper sqlMapper;

        public Logic(ILogger logger, IDataHelper dataHelper, ISqlMapper sqlMapper)
        {
            this.logger = logger;
            this.dataHelper = dataHelper;
            this.sqlMapper = sqlMapper;
        }
    }
}