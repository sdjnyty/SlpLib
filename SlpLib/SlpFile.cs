using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace YTY.SlpLib
{
  [DebuggerDisplay("{Frames.Count} frames")]
  public class SlpFile
  {
    private byte[] _version;
    private int _player;

    public List<SlpFrame> Frames { get; private set; }
    private byte[] _comment;

    public Color TransparentColor { get; set; } = Color.DarkCyan;

    public Color OutlineColor { get; set; } = Color.Yellow;

    public Color[] Palette { get; set; }

    public int Player
    {
      get => _player;
      set
      {
        if (value < 1 || value > 8)
        {
          throw new ArgumentOutOfRangeException();
        }
        _player = value;
      }
    }

    public bool DrawOutline { get; set; }

    public void Load(string fileName)
    {
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (var br = new BinaryReader(fs))
        {
          // file header
          _version = br.ReadBytes(4);
          var nShapes = br.ReadInt32();
          Frames = new List<SlpFrame>(nShapes);
          _comment = br.ReadBytes(24);

          // frame infos
          for (var i = 0; i < nShapes; i++)
          {
            var frame = new SlpFrame(this)
            {
              _ppCommands = br.ReadUInt32(),
              _pOutline = br.ReadUInt32(),
              _pPalette = br.ReadUInt32(),
              _properties = br.ReadUInt32(),
              Width = br.ReadInt32(),
              Height = br.ReadInt32(),
              HotspotX = br.ReadInt32(),
              HotspotY = br.ReadInt32(),
            };
            frame.Rows = Enumerable.Range(0, frame.Height).Select(_ => new SlpRow()).ToList();
            Frames.Add(frame);
          }

          // left & right transparent pixel counts
          for (var i = 0; i < nShapes; i++)
          {
            for (var r = 0; r < Frames[i].Height; r++)
            {
              Frames[i].Rows[r]._leftPad = br.ReadUInt16();
              Frames[i].Rows[r]._rightPad = br.ReadUInt16();
            }
            for (var r = 0; r < Frames[i].Height; r++)
            {
              Frames[i].Rows[r]._pCommands = br.ReadUInt32();
            }
            for (var r = 0; r < Frames[i].Height; r++)
            {
              var commands = new List<ISlpCommand>();
              Frames[i].Rows[r].Commands = commands;
              byte b;
              while ((b = br.ReadByte()) != 0xf)
              {
                if ((b & 0b11) == 0)
                {
                  var length = b >> 2;
                  commands.Add(new BlockCopy { Block = br.ReadBytes(length) });
                }
                else if ((b & 0b11) == 0b1)
                {
                  var length = b >> 2;
                  if (length == 0)
                  {
                    length = br.ReadByte();
                  }
                  commands.Add(new BlockTransparent { Length = length });
                }
                else
                {
                  switch (b & 0xf)
                  {
                    case 0b10:
                      var length = ((b & 0xf0) << 4) + br.ReadByte();
                      commands.Add(new BlockCopy { Block = br.ReadBytes(length) });
                      break;
                    case 0b11:
                      length = ((b & 0xf0) << 4) + br.ReadByte();
                      commands.Add(new BlockTransparent { Length = length });
                      break;
                    case 0b110:
                      length = b >> 4;
                      if (length == 0)
                      {
                        length = br.ReadByte();
                      }
                      commands.Add(new BlockCopyPlayer(this) { Indexes = br.ReadBytes(length) });
                      break;
                    case 0b111:
                      length = b >> 4;
                      if (length == 0)
                      {
                        length = br.ReadByte();
                      }
                      commands.Add(new Fill { Length = length, Index = br.ReadByte() });
                      break;
                    case 0b1010:
                      length = b >> 4;
                      if (length == 0)
                      {
                        length = br.ReadByte();
                      }
                      commands.Add(new FillPlayer(this) { Length = length, Index = br.ReadByte() });
                      break;
                    case 0b1011:
                      length = b >> 4;
                      if (length == 0)
                      {
                        length = br.ReadByte();
                      }
                      commands.Add(new Shadow { Length = length });
                      break;
                    case 0b1110:
                      switch (b & 0xf0)
                      {
                        case 0x40:
                        case 0x60:
                          commands.Add(new Outline(this) { Length = 1 });
                          break;
                        case 0x50:
                        case 0x70:
                          commands.Add(new Outline(this) { Length = br.ReadByte() });
                          break;
                      }
                      break;
                    case 0b1111:
                      Debug.Assert(!((Frames[i].Rows[r]._leftPad == 0x8000)
                        || (Frames[i].Rows[r]._rightPad == 0x8000))
                        ^ (commands.Count == 0));
                      if (commands.Count == 0)
                      {
                        commands.Add(new TransparentLine { Length = Frames[i].Width });
                      }
                      break;
                  }
                }
              }
            }
          }
        }
      }
    }
  }

  [DebuggerDisplay("{Width}x{Height} @({HotspotX}, {HotspotY})")]
  public class SlpFrame
  {
    private SlpFile _parent;

    internal uint _ppCommands;
    internal uint _pOutline;
    internal uint _pPalette;
    internal uint _properties;

    public int Width { get; set; }

    public int Height { get; set; }

    public int HotspotX { get; set; }

    public int HotspotY { get; set; }

    public List<SlpRow> Rows { get; internal set; }

    internal SlpFrame(SlpFile parent)
    {
      _parent = parent;
    }

    public Bitmap ToBitmap()
    {
      var ret = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
      var palette = ret.Palette;
      _parent.Palette[254] = _parent.OutlineColor;
      _parent.Palette[255] = _parent.TransparentColor;
      _parent.Palette.ToArray().CopyTo(palette.Entries, 0);
      ret.Palette = palette;
      var bmpData = ret.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
      var bytes = new byte[bmpData.Stride * bmpData.Height];
      Marshal.Copy(bmpData.Scan0, bytes, 0, bytes.Length);
      using (var ms = new MemoryStream(bytes))
      using (var bw = new BinaryWriter(ms))
      {
        for (var i = 0; i < Rows.Count; i++)
        {
          ms.Seek(i * bmpData.Stride, SeekOrigin.Begin);
          if (Rows[i]._leftPad != 0x8000)
          {
            bw.Write(Enumerable.Repeat((byte)0xff, Rows[i]._leftPad).ToArray());
          }
          foreach (var command in Rows[i].Commands)
          {
            command.WriteBytes(bw);
          }
          if (Rows[i]._rightPad != 0x8000)
          {
            bw.Write(Enumerable.Repeat((byte)0xff, Rows[i]._rightPad).ToArray());
          }
        }
      }
      Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
      ret.UnlockBits(bmpData);
      return ret;
    }
  }

  [DebuggerDisplay("{_leftPad}, , {_rightPad}")]
  public class SlpRow
  {
    internal ushort _leftPad;
    internal ushort _rightPad;
    internal uint _pCommands;

    public List<ISlpCommand> Commands { get; internal set; }
  }

  public interface ISlpCommand
  {
    void WriteBytes(BinaryWriter bw);
  }

  public class TransparentLine : ISlpCommand
  {
    public int Length { get; set; }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat((byte)0xff, Length).ToArray());
    }
  }

  public class BlockCopy : ISlpCommand
  {
    public byte[] Block { get; set; }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Block);
    }
  }

  public class BlockTransparent : ISlpCommand
  {
    public int Length { get; set; }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat((byte)0xff, Length).ToArray());
    }
  }

  public class BlockCopyPlayer : ISlpCommand
  {
    private SlpFile _parent;

    public byte[] Indexes { get; set; }

    internal BlockCopyPlayer(SlpFile parent)
    {
      _parent = parent;
    }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Indexes.Select(b => (byte)(b + _parent.Player * 16)).ToArray());
    }
  }

  public class Fill : ISlpCommand
  {
    public int Length { get; set; }

    public byte Index { get; set; }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat(Index, Length).ToArray());
    }
  }

  public class FillPlayer : ISlpCommand
  {
    private SlpFile _parent;

    public int Length { get; set; }

    public byte Index { get; set; }

    internal FillPlayer(SlpFile parent)
    {
      _parent = parent;
    }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat((byte)(Index + _parent.Player * 16), Length).ToArray());
    }
  }

  public class Shadow : ISlpCommand
  {
    public int Length { get; set; }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat((byte)0, Length).ToArray());
    }
  }

  public class Outline : ISlpCommand
  {
    private SlpFile _parent;

    public int Length { get; set; }

    internal Outline(SlpFile parent)
    {
      _parent = parent;
    }

    public void WriteBytes(BinaryWriter bw)
    {
      bw.Write(Enumerable.Repeat(_parent.DrawOutline ? (byte)0xfe : (byte)0xff, Length).ToArray());
    }
  }
}
