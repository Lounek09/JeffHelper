using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JeffHelper.Utils;

public static class ExtendImage
{
    public static Image Trim(this Image source)
    {
        return new Bitmap(source).Trim();
    }

    //src : https://stackoverflow.com/a/4821100
    public static Image Trim(this Bitmap source)
    {
        var rectangle = new Rectangle(0, 0, source.Width, source.Height);
        var data = source.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var buffer = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
        source.UnlockBits(data);

        int xMin = int.MaxValue,
            xMax = int.MinValue,
            yMin = int.MaxValue,
            yMax = int.MinValue;

        var foundPixel = false;

        // Find xMin
        for (var x = 0; x < data.Width; x++)
        {
            var stop = false;
            for (var y = 0; y < data.Height; y++)
            {
                var alpha = buffer[y * data.Stride + 4 * x + 3];
                if (alpha != 0)
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
            return source;
        }

        // Find yMin
        for (var y = 0; y < data.Height; y++)
        {
            var stop = false;
            for (var x = xMin; x < data.Width; x++)
            {
                var alpha = buffer[y * data.Stride + 4 * x + 3];
                if (alpha != 0)
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
        for (var x = data.Width - 1; x >= xMin; x--)
        {
            var stop = false;
            for (var y = yMin; y < data.Height; y++)
            {
                var alpha = buffer[y * data.Stride + 4 * x + 3];
                if (alpha != 0)
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
        for (var y = data.Height - 1; y >= yMin; y--)
        {
            var stop = false;
            for (var x = xMin; x <= xMax; x++)
            {
                var alpha = buffer[y * data.Stride + 4 * x + 3];
                if (alpha != 0)
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

        rectangle = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
        rectangle.Width += 1;
        rectangle.Height += 1;

        Bitmap output = new(rectangle.Width, rectangle.Height);

        using var graphics = Graphics.FromImage(output);
        graphics.DrawImage(source, 0, 0, rectangle, GraphicsUnit.Pixel);

        return output;
    }

    //src : https://stackoverflow.com/a/34705992
    public static Image Resize(this Image image, int width, int height)
    {
        var ratioW = width / (double)image.Width;
        var ratioH = height / (double)image.Height;
        var ratio = Math.Min(ratioW, ratioH);

        var scaledWidth = (int)(image.Width * ratio);
        var scaledHeight = (int)(image.Height * ratio);

        var posX = (width - scaledWidth) / 2;
        var posY = (height - scaledHeight) / 2;

        Rectangle rectangle = new(posX, posY, scaledWidth, scaledHeight);

        Bitmap output = new(width, height, image.PixelFormat);

        using var graphics = Graphics.FromImage(output);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(image, rectangle);

        return output;
    }
}
