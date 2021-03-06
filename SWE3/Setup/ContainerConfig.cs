﻿using Autofac;
using AutofacSerilogIntegration;
using Serilog;
using SWE3.BusinessLogic;
using SWE3.BusinessLogic.Interfaces;
using SWE3.DataAccess;
using SWE3.DataAccess.Interfaces;
using IContainer = Autofac.IContainer;

namespace SWE3.Setup
{
    /// <summary>
    /// Dependency Injection Configuration
    /// </summary>
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(@"Logs\log.txt", rollingInterval: RollingInterval.Hour)
                .WriteTo.ColoredConsole()
                .CreateLogger();
            builder.RegisterLogger();

            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<Logic>().As<ILogic>();
            builder.RegisterType<DataHelper>().As<IDataHelper>();
            builder.RegisterType<DataTransmitter>().As<IDataTransmitter>();

            return builder.Build();
        }
    }
}