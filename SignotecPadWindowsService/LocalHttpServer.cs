using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignotecPadWindowsService
{
    public class LocalHttpServer
    {
        public static void Start(PadManager padManager, CancellationToken token)
        {
            HttpListener listener = new HttpListener();

            // ✅ AHORA: Escuchar en todas las interfaces de red
            listener.Prefixes.Add("http://*:5000/");

            listener.Start();

            // Obtener IPs de la máquina para logging
            string localIP = GetLocalIPAddress();
            Console.WriteLine($"[LocalHttpServer] Servidor HTTP iniciado:");
            Console.WriteLine($"[LocalHttpServer] - Local: http://localhost:5000/");
            Console.WriteLine($"[LocalHttpServer] - Red: http://{localIP}:5000/");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var contextTask = listener.GetContextAsync();
                    contextTask.Wait(token);
                    var context = contextTask.Result;

                    Task.Run(() =>
                    {
                        ProcessRequest(context, padManager);
                    }, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LocalHttpServer] Error: {ex.Message}");
                }
            }

            listener.Stop();
            Console.WriteLine("[LocalHttpServer] Servidor HTTP detenido");
        }

        private static void ProcessRequest(HttpListenerContext context, PadManager padManager)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath.ToLower();
                string clientIP = context.Request.RemoteEndPoint?.Address?.ToString();
                Console.WriteLine($"[LocalHttpServer] Request from {clientIP}: {path}");

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
                RespondJson(context, new { error = ex.Message, timestamp = DateTime.UtcNow });
                Console.WriteLine($"[LocalHttpServer] Error procesando request: {ex.Message}");
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private static void RespondJson(HttpListenerContext context, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}