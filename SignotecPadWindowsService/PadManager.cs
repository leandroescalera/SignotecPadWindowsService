using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using signotec.STPadLibNet;

namespace SignotecPadWindowsService
{
    public class PadManager : IDisposable
    {
        private STPadLib _pad;
        private bool _isOpened = false;
        private bool _isCapturing = false;

        public PadManager()
        {
            _pad = new STPadLib();
        }

        // Abre el dispositivo si no está abierto
        public void Open(int index = 0, bool eraseDisplay = true)
        {
            if (!_isOpened)
            {
                _pad.DeviceOpen(index, eraseDisplay);
                _isOpened = true;
            }
        }

        // Configura pantalla/lápiz e inicia captura
        public void StartSignature(int index = 0, bool eraseDisplay = true)
        {
            Open(index, eraseDisplay);

            // Configuración de pantalla y lápiz
            _pad.DisplayErase();
            _pad.DisplaySetFont(new Font("Arial", 20));
            _pad.DisplaySetFontColor(Color.Black);
            _pad.DisplaySetText(10, 10, TextAlignment.Left, "Por favor firme aquí");
            _pad.DisplayConfigPen(2, Color.Black);

            // Iniciar captura
            _pad.SignatureStart();
            _isCapturing = true;
        }

        // Detener captura y obtener Base64
        public string StopAndGetSignatureBase64(int width, int height)
        {
            if (!_isCapturing) throw new InvalidOperationException("No hay captura iniciada.");

            _pad.SignatureStop();
            _isCapturing = false;

            var bmp = _pad.SignatureSaveAsStreamEx(
                resolution: 300,
                width: width,
                height: height,
                penWidth: 2,
                penColor: Color.Black,
                options: SignatureImageFlag.DontCrop | SignatureImageFlag.BackImage
            );

            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        // Limpiar firma
        public void ClearSignature()
        {
            if (!_isOpened) return;
            _pad.SignatureRetry();
            _isCapturing = false;

            // Iniciar captura
            _pad.SignatureStart();
            _isCapturing = true;
        }

        // Cerrar dispositivo
        public void Close()
        {
            if (!_isOpened) return;
            try { _pad.DeviceClose(0); } catch { }
            _isOpened = false;
            _isCapturing = false;
        }

        public void Dispose()
        {
            Close();
            _pad?.Dispose();
            _pad = null;
        }
    }
}
