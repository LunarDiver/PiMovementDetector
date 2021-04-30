using System.Collections.Generic;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Camera;
using System.Timers;
using System;
using System.IO;
using System.Drawing;

namespace PiMovementDetector
{
    internal class Program
    {
        private static Timer _picTaker = new Timer
        {
            AutoReset = true,
            Interval = 5000
        };

        private static Timer _picProcesser = new Timer
        {
            AutoReset = true,
            Interval = 500
        };

        private static readonly object _picLock = new object();

        private static readonly Queue<(DateTime taken, Bitmap image)> _imgData = new Queue<(DateTime taken, Bitmap image)>();

        private static (DateTime taken, Bitmap image) _lastImg;

        private static MovementDetector _detector = new MovementDetector();

        private static void Main(string[] args)
        {
            Directory.CreateDirectory("imgs");

            _picTaker.Elapsed += (_, _) => TakePicture();
            _picProcesser.Elapsed += (_, _) => ProcessPictureQueueNew();
            _picTaker.Start();
            _picProcesser.Start();

            Console.ReadLine();

            _picTaker.Dispose();
            _picProcesser.Dispose();

            _lastImg.image?.Dispose();
            while (_imgData.TryDequeue(out (DateTime taken, Bitmap image) disposingImg))
                disposingImg.image.Dispose();
        }

        private static void TakePicture()
        {
            using var imgStream = new MemoryStream();
            lock (_picLock)
            {
                imgStream.Write(Pi.Camera.CaptureImage(new CameraStillSettings
                {
                    CaptureEncoding = CameraImageEncodingFormat.Png,
                    CaptureWidth = 256,
                    CaptureHeight = 256,
                    CaptureTimeoutMilliseconds = 500,
                    ImageContrast = 100,
                    ImageSaturation = -100,
                    CaptureDisplayPreview = false,
                    CaptureWhiteBalanceControl = CameraWhiteBalanceMode.Cloud,
                    CaptureVideoStabilizationEnabled = false
                }));
            }

            var img = new Bitmap(imgStream);

            _imgData.Enqueue((DateTime.UtcNow, img));
        }

        private static void ProcessPictureQueueNew()
        {
            if (_imgData.Count == 0)
                return;

            if (_lastImg == default)
            {
                _lastImg = _imgData.Dequeue();
                return;
            }

            var currImg = _imgData.Dequeue();

            double movement = _detector.GetMovementPercent(_lastImg.image, currImg.image);

            Console.WriteLine($"[{currImg.taken}]: {Math.Round(movement * 100, 2)}%");

            _lastImg.image.Dispose();
            _lastImg = currImg;
        }
    }
}
