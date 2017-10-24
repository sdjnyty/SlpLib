using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace YTY.SlpLib
{
  public class PalFile
  {
    public List<Color> Palette { get; private set; }

    public void Load(string fileName)
    {
      using (var sr = new StreamReader( fileName, Encoding.ASCII))
      {
        _signature= sr.ReadLine();
        _version = sr.ReadLine();
        var nPalette = int.Parse(sr.ReadLine());
        Palette = Enumerable.Range(0, nPalette).Select(_ => new Color()).ToList();
        for(var i=0;i<nPalette;i++)
        {
          var ary = sr.ReadLine().Split().Select(int.Parse).ToArray();
          Palette[i] = Color.FromArgb(ary[0], ary[1], ary[2]);
        }
      }
    }

    private string _signature;
    private string _version;
  }
}
