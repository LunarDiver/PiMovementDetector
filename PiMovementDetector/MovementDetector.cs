using System;
using System.Drawing;

namespace PiMovementDetector
{
    public class MovementDetector
    {
        public byte MovementThreshold { get; set; } = 64;

        public double DetectionPercentMin { get; set; } = 0.1;

        public MovementDetector(byte movementThreshold = 64, double detectionPercentMin = 0.1)
        {
            MovementThreshold = movementThreshold;
            DetectionPercentMin = detectionPercentMin;
        }

        public bool HasMovement(Bitmap prevImage, Bitmap currImage)
        {
            if (prevImage.Width != currImage.Width || prevImage.Height != currImage.Height)
                throw new ArgumentException("Images must be of the same size.");

            using var movementImage = GetMovementImage(prevImage, currImage, MovementThreshold);

            return GetMovementPercent(movementImage) >= DetectionPercentMin;
        }

        public double GetMovementPercent(Bitmap prevImage, Bitmap currImage)
        {
            using var movementImage = GetMovementImage(prevImage, currImage, MovementThreshold);
            return GetMovementPercent(movementImage);
        }

        private static double GetMovementPercent(Bitmap movementImage)
        {
            int totalPixels = movementImage.Width * movementImage.Height;
            int movingPixels = 0;

            for (int x = 0; x < movementImage.Width; x++)
                for (int y = 0; y < movementImage.Height; y++)
                {
                    Color currPixel = movementImage.GetPixel(x, y);

                    if (currPixel.ToArgb() != Color.Black.ToArgb())
                        movingPixels++;
                }

            return (double)movingPixels / totalPixels;
        }

        private static Bitmap GetMovementImage(Bitmap prev, Bitmap curr, byte threshold)
        {
            var newImg = new Bitmap(curr.Width, curr.Height);

            for (int x = 0; x < newImg.Width; x++)
                for (int y = 0; y < newImg.Height; y++)
                {
                    Color lastPixel = prev.GetPixel(x, y);
                    Color currPixel = curr.GetPixel(x, y);

                    int newR = Math.Abs(lastPixel.R - currPixel.R);
                    int newG = Math.Abs(lastPixel.G - currPixel.G);
                    int newB = Math.Abs(lastPixel.B - currPixel.B);

                    if (newR < 64)
                        newR = 0;
                    if (newG < 64)
                        newG = 0;
                    if (newB < 64)
                        newB = 0;

                    newImg.SetPixel(x, y, Color.FromArgb(newR, newG, newB));
                }

            return newImg;
        }
    }
}