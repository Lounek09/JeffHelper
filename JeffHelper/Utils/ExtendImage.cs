using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace JeffHelper.Utils;

/// <summary>
/// Provides extension methods for the Image class.
/// </summary>
public static class ExtendImage
{
    extension<T>(Image<T> source) where T : unmanaged, IPixel<T>
    {
        /// <summary>
        /// Trims the image by removing transparent edges.
        /// </summary>
        /// <remarks>Source: https://stackoverflow.com/a/4821100</remarks>
        public void Trim()
        {
            int xMin = int.MaxValue, xMax = int.MinValue, yMin = int.MaxValue, yMax = int.MinValue;
            var foundPixel = false;

            // Find xMin
            for (var x = 0; x < source.Width; x++)
            {
                var stop = false;
                for (var y = 0; y < source.Height; y++)
                {
                    var pixel = source[x, y];
                    if (pixel.A > 0)
                    {
                        xMin = x;
                        stop = true;
                        foundPixel = true;
                        break;
                    }
                }

                if (stop)
                {
                    break;
                }
            }

            // Image is empty
            if (!foundPixel)
            {
                return;
            }

            // Find yMin
            for (var y = 0; y < source.Height; y++)
            {
                var stop = false;
                for (var x = xMin; x < source.Width; x++)
                {
                    var pixel = source[x, y];
                    if (pixel.A > 0)
                    {
                        yMin = y;
                        stop = true;
                        break;
                    }
                }

                if (stop)
                {
                    break;
                }
            }

            // Find xMax
            for (var x = source.Width - 1; x >= xMin; x--)
            {
                var stop = false;
                for (var y = yMin; y < source.Height; y++)
                {
                    var pixel = source[x, y];
                    if (pixel.A > 0)
                    {
                        xMax = x;
                        stop = true;
                        break;
                    }
                }

                if (stop)
                {
                    break;
                }
            }

            // Find yMax
            for (var y = source.Height - 1; y >= yMin; y--)
            {
                var stop = false;
                for (var x = xMin; x <= xMax; x++)
                {
                    var pixel = source[x, y];
                    if (pixel.A > 0)
                    {
                        yMax = y;
                        stop = true;
                        break;
                    }
                }

                if (stop)
                {
                    break;
                }
            }

            var rectangle = Rectangle.FromLTRB(xMin, yMin, xMax + 1, yMax + 1);
            source.Mutate(x => x.Crop(rectangle));
        }

        /// <summary>
        /// Creates a new image that is a resized copy of the source image.
        /// </summary>
        /// <param name="width">The desired width of the output image.</param>
        /// <param name="height">The desired height of the output image.</param>
        /// <returns>A new image that is the resized version of the source image.</returns>
        /// <remarks>Source: https://stackoverflow.com/a/34705992</remarks>
        public Image<Rgba32> CreateResizedCopy(int width, int height)
        {
            var ratioWidth = width / (double)source.Width;
            var ratioHeight = height / (double)source.Height;
            var ratio = Math.Min(ratioWidth, ratioHeight);

            var scaledWidth = (int)(source.Width * ratio);
            var scaledHeight = (int)(source.Height * ratio);

            var posX = (width - scaledWidth) / 2;
            var posY = (height - scaledHeight) / 2;

            Image<Rgba32> output = new(width, height, new Rgba32());

            output.Mutate(x =>
            {
                var resizedImage = source.Clone(ctx => ctx.Resize(scaledWidth, scaledHeight));
                x.DrawImage(resizedImage, new Point(posX, posY), 1f);
            });

            return output;
        }
    }
}
