using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignotecPadWindowsService
{
    public class LocalHttpServer
    {
        public static void Start(PadManager padManager, CancellationToken token)
        {
            HttpListener listener = new HttpListener();
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
                                Respond(context, "Captura iniciada");
                                break;

                            case "/signature/capture":
                                string base64 = padManager.StopAndGetSignatureBase64(600, 200);
                                Respond(context, base64);
                                break;

                            case "/signature/clear":
                                padManager.ClearSignature();
                                Respond(context, "Firma eliminada");
                                break;

                            case "/signature/close":
                                padManager.Close();
                                Respond(context, "Dispositivo cerrado");
                                break;

                            default:
                                context.Response.StatusCode = 404;
                                Respond(context, "Endpoint no encontrado");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        Respond(context, "Error: " + ex.Message);
                    }
                    finally
                    {
                        context.Response.OutputStream.Close();
                    }
                }, token);
            }

            listener.Stop();
        }

        private static void Respond(HttpListenerContext context, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
}
