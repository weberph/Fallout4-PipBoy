using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipBoy
{
    public struct MapPoint
    {
        public MapPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X;
        public float Y;
    }

    public class LocalMapData
    {
        public int Height { get; }
        public MapPoint TopLeft { get; }
        public MapPoint TopRight { get; }
        public MapPoint BottomLeft { get; }
        public MapPoint BottomRight { get; }
        public int Width { get; }
        public byte[] Data { get; set; }

        private readonly int _dataWidth;

        public LocalMapData(int width, int height, MapPoint topLeft, MapPoint topRight, MapPoint bottomLeft, byte[] data)
        {
            // the actual width of the image that contains usable data might be smaller than the line width in `data`
            var widthMatchesData = data.Length % (width * height) == 0;
            if (width <= 0 || height <= 0 || (data.Length % height != 0 && !widthMatchesData))
            {
                throw new Exception("Invalid width, height or data size");
            }

            Height = height;
            Width = width;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            Data = data;
            BottomRight = new MapPoint(TopRight.X + BottomLeft.X, TopRight.Y + BottomLeft.Y);
            _dataWidth = widthMatchesData ? width : (data.Length / height);
        }

        // TODO: base/hud color
        public Bitmap CreateBitmap()
        {
            var bitmap = new Bitmap(Width, Height);
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    int pixelColor = Data[y * _dataWidth + x];
                    bitmap.SetPixel(x, y, Color.FromArgb(pixelColor, pixelColor, pixelColor));
                }
            }
            return bitmap;
        }

        public Bitmap CreateBitmap(int scale)
        {
            var bitmap = new Bitmap(Width, Height);
            var graphic = Graphics.FromImage(bitmap);
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    int pixelColor = Data[y * _dataWidth + x];
                    var sb = new SolidBrush(Color.FromArgb(pixelColor, pixelColor, pixelColor));
                    graphic.FillRectangle(sb, x * scale, y * scale, scale, scale);
                }
            }
            return bitmap;
        }
    }
}
