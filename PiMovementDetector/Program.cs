using System.Threading;
using System.Collections.Generic;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Camera;
using System;
using System.IO;
using System.Drawing;
using Timer = System.Timers.Timer;
using System.Drawing.Imaging;
using TcpSubLib;

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

        private static readonly Queue<(DateTime taken, Bitmap image)> _imgData = new Queue<(DateTime taken, Bitmap image)>();

        private static Bitmap _lastImg;

        private static readonly MovementDetector _detector = new MovementDetector();

        private static readonly TcpConnector _tcp = new TcpConnector(out _tcpPort, true);

        private static readonly ushort _tcpPort;

        private const string _tcpPortFile = "tcpport";

        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogException;
            AppDomain.CurrentDomain.UnhandledException += (_, _) => Close();

            try
            {
                File.WriteAllText(_tcpPortFile, _tcpPort.ToString());
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Could not write TCP port to file: {exc.Message}");
                return 1;
            }

            if (double.TryParse(args[0], out double detectionPercent))
                _detector.DetectionPercentMin = detectionPercent;

            _picTaker.Elapsed += (_, _) => TakePicture();
            _picProcesser.Elapsed += (_, _) => ProcessPictureQueue();
            _picTaker.Start();
            _picProcesser.Start();

            Console.CancelKeyPress += (_, _) => Close();

            while (true)
                Console.ReadLine();
        }

        private static void LogException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception occurred:\n" + e.ExceptionObject);
        }

        private static void Close()
        {
            _picTaker.Dispose();
            _picProcesser.Dispose();

            _lastImg?.Dispose();
            while (_imgData.TryDequeue(out (DateTime taken, Bitmap image) disposingImg))
                disposingImg.image.Dispose();

            try
            {
                File.Delete(_tcpPortFile);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Could not delete TCP port file: {exc.Message}");
                Environment.Exit(2);
                return;
            }

            Environment.Exit(Environment.ExitCode);
        }

        private static void TakePicture()
        {
            using var m = new Mutex(true, $"{nameof(Pi)}.{nameof(Pi.Camera)}");

            using var imgStream = new MemoryStream();

            if (m.WaitOne(TimeSpan.FromSeconds(5)))
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
                    CaptureVideoStabilizationEnabled = false,
                    HorizontalFlip = true,
                    VerticalFlip = true
                }));

                m.ReleaseMutex();
            }
            else return;

            var img = new Bitmap(imgStream);

            _imgData.Enqueue((DateTime.UtcNow, img));
        }

        private static void ProcessPictureQueue()
        {
            if (_imgData.Count == 0)
                return;

            if (_lastImg == default)
            {
                _lastImg = _imgData.Dequeue().image;
                return;
            }

            var currImg = _imgData.Dequeue();

            if (_detector.HasMovement(_lastImg, currImg.image))
            {
                Console.WriteLine($"Movement detected at {currImg.taken}!");

                using var memory = new MemoryStream();
                currImg.image.Save(memory, ImageFormat.Png);

                _tcp.Write(memory.ToArray());
            }

            // double movement = _detector.GetMovementPercent(_lastImg.image, currImg.image);

            // Console.WriteLine($"[{currImg.taken}]: {Math.Round(movement * 100, 2)}%");

            _lastImg.Dispose();
            _lastImg = currImg.image;
        }
    }
}
