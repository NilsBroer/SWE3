using Autofac;
using SWE3.Setup;

namespace SWE3
{
    internal static class Program
    {
        private static void Main()
        {
            var container = ContainerConfig.Configure();
            using var scope = container.BeginLifetimeScope();
            var app = scope.Resolve<IApplication>();
            app.Run();
        }
    }
}
