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
      var slp = new SlpFile
      {
        Player = 2,
        DrawOutline=false,
      };
      slp.Load(@"D:\Hawkaoc\amt\tools\Turtle Pack\Samples\attack.slp");
      for(var f=0;f< slp.Frames.Count;f++)
      {
        var fileName = $@"c:\slp\{f:0000}.bmp";
        slp.Frames[f].ToBitmap().Save(fileName,  ImageFormat.Bmp);
        Console.WriteLine(fileName);
      }
      Console.ReadKey();
    }
  }
}
