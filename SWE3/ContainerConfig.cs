using System.Linq;
using System.Reflection;
using Autofac;
using AutofacSerilogIntegration;
using ISWE3;
using Serilog;
using Serilog.Core;

namespace SWE3
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();
            builder.RegisterLogger();

            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterAssemblyTypes(Assembly.Load(nameof(ISWE3)))
                .Where(t => t.Namespace.Contains("Utilities"))
                .As(t => t.GetInterfaces().FirstOrDefault(i => i.Name == "I" + t.Name));
            builder.RegisterType<BusinessLogic>().As<IBusinessLogic>();

            return builder.Build();
        }
    }
}