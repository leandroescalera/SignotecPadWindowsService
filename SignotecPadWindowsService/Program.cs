using System.ServiceProcess;

namespace SignotecPadWindowsService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new SignotecPadService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
