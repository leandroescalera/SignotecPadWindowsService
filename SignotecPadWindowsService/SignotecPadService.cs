using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace SignotecPadWindowsService
{
    public partial class SignotecPadService : ServiceBase
    {
        private PadManager _padManager;
        private CancellationTokenSource _cts;
        private Task _httpServerTask;

        public SignotecPadService()
        {
            this.ServiceName = "SignotecPadService";
        }

        protected override void OnStart(string[] args)
        {
            _padManager = new PadManager();
            _padManager.Open();

            _cts = new CancellationTokenSource();
            _httpServerTask = Task.Run(() => LocalHttpServer.Start(_padManager, _cts.Token));
        }

        protected override void OnStop()
        {
            _cts?.Cancel();
            _httpServerTask?.Wait(2000);
            _padManager?.Dispose();
        }
    }
}
