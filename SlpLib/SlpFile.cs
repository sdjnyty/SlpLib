using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace YTY.SlpLib
{
  [DebuggerDisplay("{Frames.Count} frames")]
  public class SlpFile
  {
    private byte[] _version;
    public List<SlpFrame> Frames { get; private set; }
    private byte[] _comment;

    public void Load(string fileName)
    {
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,FileShare.Read))
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
            var frame = new SlpFrame
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
              var commands = new List<SlpCommand>();
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
                      commands.Add(new BlockCopyPlayer { Indexes = br.ReadBytes(length) });
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
                      commands.Add(new FillPlayer { Length = length, Index = br.ReadByte() });
                      break;
                    case 0b1011:
                      length = b >> 4;
                      if (length == 0)
                      {
                        length = br.ReadByte();
                      }
                      commands.Add(new FillPlayer { Length = length });
                      break;
                    case 0b1110:
                      switch (b & 0xf0)
                      {
                        case 0x50:
                        case 0x70:
                          br.ReadByte();
                          break;
                      }
                      break;
                    case 0b1111:
                      Debug.Assert(!((Frames[i].Rows[r]._leftPad == 0x8000)
                        || (Frames[i].Rows[r]._rightPad == 0x8000))
                        ^ (commands.Count == 0));
                      if (commands.Count == 0)
                      {
                        commands.Add(new TransparentLine());
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
    internal uint _ppCommands;
    internal uint _pOutline;
    internal uint _pPalette;
    internal uint _properties;
    public int Width { get; set; }

    public int Height { get; set; }

    public int HotspotX { get; set; }

    public int HotspotY { get; set; }

    public List<SlpRow> Rows { get; internal set; }
  }

  [DebuggerDisplay("{_leftPad}, , {_rightPad}")]
  public class SlpRow
  {
    internal ushort _leftPad;
    internal ushort _rightPad;
    internal uint _pCommands;

    public List<SlpCommand> Commands { get; internal set; }
  }

  public abstract class SlpCommand
  {

  }

  public class TransparentLine : SlpCommand
  {

  }

  public class BlockCopy : SlpCommand
  {
    public byte[] Block { get; set; }
  }

  public class BlockTransparent : SlpCommand
  {
    public int Length { get; set; }
  }

  public class BlockCopyPlayer : SlpCommand
  {
    public byte[] Indexes { get; set; }
  }

  public class Fill : SlpCommand
  {
    public int Length { get; set; }

    public int Index { get; set; }
  }

  public class FillPlayer : SlpCommand
  {
    public int Length { get; set; }

    public int Index { get; set; }
  }

  public class Shadow : SlpCommand
  {
    public int Length { get; set; }
  }
}
