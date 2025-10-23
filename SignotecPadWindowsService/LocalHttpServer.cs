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
                                RespondJson(context, new { message = "Captura iniciada" });
                                break;

                            case "/signature/capture":
                                string base64 = padManager.StopAndGetSignatureBase64(600, 200);
                                RespondJson(context, new { signatureImageBase64 = base64 });
                                break;

                            case "/signature/clear":
                                padManager.ClearSignature();
                                RespondJson(context, new { message = "Firma eliminada" });
                                break;

                            case "/signature/close":
                                padManager.Close();
                                RespondJson(context, new { message = "Dispositivo cerrado" });
                                break;

                            default:
                                context.Response.StatusCode = 404;
                                RespondJson(context, new { message = "Endpoint no encontrado" });
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
