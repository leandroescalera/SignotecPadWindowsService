using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace SignotecPadWindowsService
{
    public class LocalHttpServer
    {
        public static void Start(PadManager padManager, CancellationToken token)
        {
            // Obtener el nombre del host de la máquina
            string hostName = Dns.GetHostName();

            // Intentar obtener la IP IPv4 local (para que sea accesible en LAN)
            string localIp = Dns.GetHostAddresses(hostName)
                                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                                .ToString() ?? "127.0.0.1";

            // Mostrar información útil en logs/eventos
            System.Diagnostics.EventLog.WriteEntry("SignotecPadService",
                $"Iniciando servidor HTTP en http://{hostName}:5000/ (IP: {localIp})",
                System.Diagnostics.EventLogEntryType.Information);

            // Crear el listener
            HttpListener listener = new HttpListener();

            // Escucha tanto por hostname como por IP local y localhost
            listener.Prefixes.Add($"http://{hostName}:5000/");
            listener.Prefixes.Add($"http://{localIp}:5000/");
            listener.Prefixes.Add("http://localhost:5000/");

            listener.Start();

            while (!token.IsCancellationRequested)
            {
                var contextTask = listener.GetContextAsync();
                contextTask.Wait(token);
                var context = contextTask.Result;

                Task.Run(() =>
                {
                    try
                    {
                        string path = context.Request.Url.AbsolutePath.ToLower();

                        switch (path)
                        {
                            case "/signature/start":
                                padManager.StartSignature();
                                RespondJson(context, new { message = "Captura iniciada", timestamp = DateTime.UtcNow });
                                break;

                            case "/signature/capture":
                                var result = padManager.StopAndGetSignatureBase64(600, 200);
                                RespondJson(context, new
                                {
                                    signatureImageBase64 = result.imageBase64,
                                    rsaSignDataBase64 = result.rsaSignDataBase64,
                                    timestamp = DateTime.UtcNow
                                });
                                break;

                            case "/signature/clear":
                                padManager.ClearSignature();
                                RespondJson(context, new { message = "Firma eliminada", timestamp = DateTime.UtcNow });
                                break;

                            case "/signature/close":
                                padManager.Close();
                                RespondJson(context, new { message = "Dispositivo cerrado", timestamp = DateTime.UtcNow });
                                break;

                            default:
                                context.Response.StatusCode = 404;
                                RespondJson(context, new { error = "Endpoint no encontrado", path });
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        RespondJson(context, new { error = ex.Message });
                    }
                    finally
                    {
                        context.Response.OutputStream.Close();
                    }
                }, token);
            }

            listener.Stop();
        }

        private static void RespondJson(HttpListenerContext context, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
}
