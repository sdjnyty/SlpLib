using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using YTY.SlpLib;

namespace ConsoleTest
{
  class Program
  {
    static void Main(string[] args)
    {
      var slp = new SlpFile();
      slp.Load(@"C:\Program Files (x86)\Age of Empires II\Manager\tools\Turtle Pack\Samples\attack.slp");
      foreach (var frame in slp.Frames)
      {
        var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format8bppIndexed);
        var palette = bmp.Palette;
        var pal = new PalFile();
        pal.Load(@"C:\Program Files (x86)\Age of Empires II\Manager\tools\Turtle Pack\palettes\50500.pal");
        pal.Palette.ToArray().CopyTo(palette.Entries, 0);
        bmp.Palette = palette;
        var bmpData = bmp.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        for (var x=0;x< frame.Height;x++)
        {
          for (var y = 0; y < frame.Width; y++)
          {
            
          }
        }
        bmp.UnlockBits(bmpData);
        bmp.Save(@"c:\1.bmp");
      }
      Console.ReadKey();
    }
  }
}
