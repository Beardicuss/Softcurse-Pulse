using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace IcoConverter
{
    class Program
    {
        static void Main()
        {
            if (!OperatingSystem.IsWindows()) return;

            var imgPath = @"D:\Dev\Projects\Experiments\Pulse\red_pulse.png";
            var outPath = @"D:\Dev\Projects\Experiments\Pulse\Pulse.App\red_pulse.ico";
            
            var img = new Bitmap(imgPath);
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var pixel = img.GetPixel(x, y);
                    if (pixel.R > 230 && pixel.G > 230 && pixel.B > 230)
                    {
                        img.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
            
            int[] sizes = { 16, 32, 48, 64, 128, 256 };
            var memoryStreams = new List<MemoryStream>();
            
            foreach (var size in sizes)
            {
                var resized = new Bitmap(size, size);
                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(img, 0, 0, size, size);
                }
                var ms = new MemoryStream();
                resized.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                memoryStreams.Add(ms);
            }

            using (var fs = new FileStream(outPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                // Write ICO header
                bw.Write((short)0); // reserved
                bw.Write((short)1); // type
                bw.Write((short)sizes.Length); // count
                
                int offset = 6 + (16 * sizes.Length);
                
                // Write entries
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    byte w = size == 256 ? (byte)0 : (byte)size;
                    bw.Write(w); // width
                    bw.Write(w); // height
                    bw.Write((byte)0); // colors
                    bw.Write((byte)0); // reserved
                    bw.Write((short)1); // planes
                    bw.Write((short)32); // bpp
                    bw.Write((int)memoryStreams[i].Length); // size of image
                    bw.Write(offset); // offset
                    
                    offset += (int)memoryStreams[i].Length;
                }
                
                // Write image data
                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(memoryStreams[i].ToArray());
                    memoryStreams[i].Dispose();
                }
            }
            Console.WriteLine("Done generating multi-size ICO.");
        }
    }
}
