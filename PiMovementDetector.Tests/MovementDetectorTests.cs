using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PiMovementDetector.Tests
{
    [TestClass]
    public class MovementDetectorTests
    {
        [TestMethod]
        public void IsMovementPercentAccurate()
        {
            using var img1 = new Bitmap(8, 8);
            using var img2 = new Bitmap(8, 8);

            img2.SetPixel(0, 0, Color.White);
            img2.SetPixel(1, 1, Color.White);
            img2.SetPixel(2, 2, Color.White);
            img2.SetPixel(3, 3, Color.White);

            var detector = new MovementDetector();

            double movement = detector.GetMovementPercent(img1, img2);

            double expectedMovement = 4d / (8d * 8d);

            Assert.AreEqual(expectedMovement, movement);
        }

        [TestMethod]
        public void IsDetectorFilteringPixels()
        {
            using var img1 = new Bitmap(8, 8);
            using var img2 = new Bitmap(8, 8);

            img2.SetPixel(0, 0, Color.White);
            img2.SetPixel(1, 1, Color.FromArgb(32, 32, 32));
            img2.SetPixel(2, 2, Color.White);
            img2.SetPixel(3, 3, Color.White);

            var detector = new MovementDetector(64);

            double movement = detector.GetMovementPercent(img1, img2);

            double expectedMovement = 3d / (8d * 8d);

            Assert.AreEqual(expectedMovement, movement);
        }

        [TestMethod]
        public void IsDetectorFilteringMovement()
        {
            using var img1 = new Bitmap(8, 8);
            using var img2 = new Bitmap(8, 8);

            img2.SetPixel(0, 0, Color.White);
            img2.SetPixel(1, 1, Color.White);
            img2.SetPixel(2, 2, Color.White);
            img2.SetPixel(3, 3, Color.White);

            var strictDetector = new MovementDetector(detectionPercentMin: 0.5);
            var preciseDetector = new MovementDetector(detectionPercentMin: 4d / (8d * 8d));
            var tooPreciseDetector = new MovementDetector(detectionPercentMin: 5d / (8d * 8d));

            Assert.IsFalse(strictDetector.HasMovement(img1, img2));
            Assert.IsTrue(preciseDetector.HasMovement(img1, img2));
            Assert.IsFalse(tooPreciseDetector.HasMovement(img1, img2));
        }
    }
}
