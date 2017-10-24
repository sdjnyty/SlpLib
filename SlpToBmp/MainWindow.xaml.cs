using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using Path = System.IO.Path;
using YTY.SlpLib;

namespace YTY.SlpToBmp
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private string _folder;
    private string[] _slpFiles;
    private string[] _palFiles;
    private Timer _timer = new Timer(100);
    private int _frame;

    public int PlayerNumber { get; set; }

    public IEnumerable<int> PlayerNumbers => Enumerable.Range(1, 8);

    public string[] SlpFiles
    {
      get => _slpFiles;
      set
      {
        _slpFiles = value;
        OnPropertyChanged();
      }
    }

    public string[] PalFiles
    {
      get => _palFiles;
      set
      {
        _palFiles = value;
        OnPropertyChanged();
      }
    }

    public string PaletteFile { get; set; }

    public bool DrawOutline { get; set; }

    public MainWindow()
    {
      InitializeComponent();
      _timer.Elapsed += _timer_Elapsed;
      _timer.Start();
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      
    }

    private void SelectFolder(object sender, RoutedEventArgs e)
    {
      var fbd = new FolderBrowserDialog
      {
        SelectedPath = Environment.CurrentDirectory,
      };
      if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        _folder = fbd.SelectedPath;
        SlpFiles = Directory.GetFiles(_folder, "*.slp", SearchOption.TopDirectoryOnly);
        PalFiles = Directory.GetFiles(_folder, "*.pal", SearchOption.TopDirectoryOnly);

      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ListBox_Selected(object sender, RoutedEventArgs e)
    {
      var listbox = (ListBox)sender;
      var pal = (string)listbox.SelectedItem;
      Console.WriteLine(pal);
    }

    private void Export(object sender, RoutedEventArgs e)
    {
      var pal = new PalFile();
      pal.Load(PaletteFile);

      foreach (var slpFile in _slpFiles)
      {
        var slp = new SlpFile
        {
          DrawOutline = DrawOutline,
          Player = PlayerNumber,
          Palette = pal.Palette.ToArray(),
        };
        slp.Load(slpFile);
        for (var i = 0; i < slp.Frames.Count; i++)
        {
          slp.Frames[i].ToBitmap().Save(Path.Combine(_folder, $"{slpFile}_{i:0000}.bmp"));
        }
      }
      MessageBox.Show("Done");
    }
  }
}
