using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace COCDonator
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {

    [DllImport("user32.dll")]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;
    private const uint MOUSEEVENTF_MOVE = 0x01;

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                                      IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private const int SRCCOPY = 0x00CC0020;

    bool IsGray(Color color) => color.R == color.G && color.G == color.B || (color.R == color.G && color.B == 213);

    int sessionDonations = 0;

    private Dictionary<string, (Color Color, int Count)> troopColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Barbarian", (Color.FromArgb(188, 103, 82), 0) },
      { "Giant", (Color.FromArgb(244, 153, 112), 0) },
      { "Wallbreaker", (Color.FromArgb(66, 63, 58), 0) },
      { "Balloon", (Color.FromArgb(237, 219, 189), 0) },
      { "Healer", (Color.FromArgb(255, 208, 181), 0) },
      { "Pekka", (Color.FromArgb(20, 16, 20), 0) },
      { "Miner", (Color.FromArgb(255, 212, 173), 0) },
      { "Yeti", (Color.FromArgb(79, 79, 110), 0) },
      { "Etitan", (Color.FromArgb(117, 87, 94), 0) },
      { "Minion", (Color.FromArgb(62, 146, 212), 0) },
      { "Valk", (Color.FromArgb(194, 125, 105), 0) },
      { "Witch", (Color.FromArgb(197, 78, 157), 0) },
      { "Lavahound", (Color.FromArgb(88, 89, 98), 0) },
      { "Icegolem", (Color.FromArgb(149, 146, 154), 0) },
      { "Apprentice", (Color.FromArgb(140, 28, 130), 0) },
      { "Archer", (Color.FromArgb(151, 101, 89), 0) },
      { "Goblin", (Color.FromArgb(91, 115, 42), 0) },
      { "Superwallbreaker", (Color.FromArgb(92, 75, 64), 0) },
      { "Wizard", (Color.FromArgb(220, 154, 128), 0) },
      { "Dragon", (Color.FromArgb(255, 117, 183), 0) },
      { "Babydragon", (Color.FromArgb(65, 32, 28), 0) },
      { "Edragon", (Color.FromArgb(48, 95, 170), 0) },
      { "Dragonrider", (Color.FromArgb(74, 30, 27), 0) },
      { "Rootrider", (Color.FromArgb(209, 108, 82), 0) },
      { "Hogrider", (Color.FromArgb(39, 34, 31), 0) },
      { "Golem", (Color.FromArgb(100, 92, 84), 0) },
      { "Bowler", (Color.FromArgb(137, 126, 250), 0) },
      { "Headhunter", (Color.FromArgb(13, 13, 13), 0) },
      { "Druid", (Color.FromArgb(143, 48, 36), 0) }
    };

    private Dictionary<string, (Color Color, int Count)> spellColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Lightning", (Color.FromArgb(59, 218, 255), 0) },
      { "Rage", (Color.FromArgb(80, 50, 120), 0) },
      { "Freeze", (Color.FromArgb(139, 221, 253), 0) },
      { "Invis", (Color.FromArgb(125, 204, 178), 0) },
      { "Poison", (Color.FromArgb(255, 216, 27), 0) },
      { "Haste", (Color.FromArgb(255, 240, 253), 0) },
      { "Bats", (Color.FromArgb(145, 108, 169), 0) },
      { "Heal", (Color.FromArgb(207, 140, 62), 0) },
      { "Jump", (Color.FromArgb(95, 206, 21), 0) },
      { "Clone", (Color.FromArgb(27, 188, 205), 0) },
      { "Recall", (Color.FromArgb(251, 77, 108), 0) },
      { "Earthquake", (Color.FromArgb(230, 172, 121), 0) },
      { "Skeleton", (Color.FromArgb(172, 41, 28), 0) },
      { "Overgrowth", (Color.FromArgb(243, 255, 132), 0) }
    };

    private Dictionary<string, (Color Color, int Count)> siegeColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Flameflinger", (Color.FromArgb(243, 255, 132), 0) },
      { "Blimp", (Color.FromArgb(243, 255, 132), 0) },
      { "Loglauncher", (Color.FromArgb(243, 255, 132), 0) }
    };

    private int TroopsToRecruit = 0;

    public MainWindow()
    {
      InitializeComponent();

      Task.Run(() => FindDonateButton());
      //Thread.Sleep(5000);
      //Task.Run(() => TrainTroops());
      //Thread.Sleep(5000);
      //GetPictures();
    }

    public void FindDonateButton()
    {
      DateTime lastTime = DateTime.Now;
      bool moveDirection = true; // true = up, false = down
      // Loads current screen into bitmap
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = new Bitmap(bounds.Width, bounds.Height);
      while (true)
      {
        Thread.Sleep(1000);
        fullscreen = CaptureScreen(bounds);

        for (int y = 0; y < 1080; y++)
        {
          Color fullPixel = fullscreen.GetPixel(580, y);
          if (fullPixel.R == 221 && fullPixel.G == 246 && fullPixel.B == 133)
          {
            ClickPosition(580, y);
            DonateTroops();
            y += 50;
          }
        }
        if (moveDirection)
        {
          moveDirection = NextDonationUp(fullscreen.GetPixel(700, 150));
          if (!moveDirection) NextDonationDown(fullscreen.GetPixel(700, 880));
        }
        else
        {
          moveDirection = !NextDonationDown(fullscreen.GetPixel(700, 880));
          if (moveDirection) NextDonationUp(fullscreen.GetPixel(700, 150));
        }

        if (TroopsToRecruit > 5)
        {
          NavigateToTrainTroops();
          TrainTroops();
          NavigateFromTroopsToTrainSpells();
          TrainSpells();
          NavigateToChat();
          TroopsToRecruit = 0;
        }
        if ((DateTime.Now - lastTime).TotalMinutes >= 3)
        {
          ClickPosition(150, 900);
          NavigateToChat();
          lastTime = DateTime.Now;
        }
      }
    }

    public void DonateTroops()
    {
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = CaptureScreen(bounds);

      int x = 820;
      int y = 150;
      if (fullscreen.GetPixel(1530, 700).R == 200) // 1643, 45 // 254, 151, 154
      {
        FillTroops(x, y);
        FillSpells(x, y + 400);
        ClickPosition(1642, 45);
      }
      else if (fullscreen.GetPixel(1530, 730).R == 146) // 1643, 72 // 250, 111, 114
      {
        y += 69;
        FillTroops(x, y);
        FillSpells(x, y + 400);
        ClickPosition(1642, 72);
      }
      else if (fullscreen.GetPixel(1530, 800).R == 177 || fullscreen.GetPixel(1530, 800).R == 173) // 1643, 135 // 251, 146, 149
      {
        y += 89;
        FillTroops(x, y);
        FillSpells(x, y + 400);
        ClickPosition(1642, 135);
      }
      else
      {
        ClickPosition(1835, 40);
      }
    }

    private void FillTroops(int x, int y)
    {
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = CaptureScreen(bounds);

      bool movingDown = true; // Track the direction: down or up
      int counter = 0;
      while (true)
      {
        Color fullPixel = fullscreen.GetPixel(x, y);
        // Check if the current position is gray; if so, stop the loop
        if (counter > 13)
        {
          break;
        }

        // Click the position until it becomes gray
        while (!IsGray(fullPixel))
        {
          if (!IsGray(fullscreen.GetPixel(770, 500))) return; // If donation window is closed

          if (CountColor(fullPixel))
            ClickPosition(x, y);
          else break;
          Console.WriteLine(++sessionDonations);
          TroopsToRecruit++;
          Thread.Sleep(50);

          fullscreen = CaptureScreen(bounds);
          fullPixel = fullscreen.GetPixel(x, y);
          // Move to the next position in a zigzag pattern
          if (movingDown)
          {
            // Move to the second row
            y += 163;
            movingDown = false;
          }
          else
          {
            // Move back to the first row and to the next column
            y -= 163;
            x += 127;
            movingDown = true;
          }
          counter++;
        }
      }
    }

    void FillSpells(int x, int y)
    {
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = CaptureScreen(bounds);

      int counter = 0;
      while (true)
      {
        Color fullPixel = fullscreen.GetPixel(x, y);
        // Check if the current position is gray; if so, stop the loop
        if (counter > 6)
        {
          break;
        }

        // Click the position until it becomes gray
        while (!IsGray(fullPixel))
        {
          if (!IsGray(fullscreen.GetPixel(770, 500))) return; // If donation window is closed

          if (CountColor(fullPixel))
            ClickPosition(x, y);
          else break;
          Console.WriteLine(++sessionDonations);
          TroopsToRecruit++;
          Thread.Sleep(50);

          fullscreen = CaptureScreen(bounds);
          fullPixel = fullscreen.GetPixel(x, y);
        }
        x += 127;
        counter++;
      }
    }

    public bool CountColor(Color color)
    {
      // Match the color against known RGB values
      switch ((color.R, color.G, color.B))
      {
        case var _ when IsColorMatch(color, 222, 208, 207): IncrementTroopCounter("Barbarian"); return true;
        case var _ when IsColorMatch(color, 188, 115, 83): IncrementTroopCounter("Giant"); return true;
        case var _ when IsColorMatch(color, 100, 92, 86): IncrementTroopCounter("Wallbreaker"); return true;
        case var _ when IsColorMatch(color, 109, 99, 93): IncrementTroopCounter("Wallbreaker"); return true;
        case var _ when IsColorMatch(color, 183, 164, 154): IncrementTroopCounter("Balloon"); return true;
        case var _ when IsColorMatch(color, 176, 151, 141): IncrementTroopCounter("Balloon"); return true;
        case var _ when IsColorMatch(color, 198, 160, 118): IncrementTroopCounter("Healer"); return true;
        case var _ when IsColorMatch(color, 193, 153, 108): IncrementTroopCounter("Healer"); return true;
        case var _ when IsColorMatch(color, 211, 203, 254): IncrementTroopCounter("Pekka"); return true;
        case var _ when IsColorMatch(color, 158, 217, 248): IncrementTroopCounter("Pekka"); return true;
        case var _ when IsColorMatch(color, 97, 65, 54): IncrementTroopCounter("Miner"); return true;
        case var _ when IsColorMatch(color, 148, 160, 193): IncrementTroopCounter("Yeti"); return true;
        case var _ when IsColorMatch(color, 254, 242, 250): IncrementTroopCounter("Etitan"); return true;
        case var _ when IsColorMatch(color, 252, 235, 244): IncrementTroopCounter("Etitan"); return true;
        case var _ when IsColorMatch(color, 16, 54, 89): IncrementTroopCounter("Minion"); return true;
        case var _ when IsColorMatch(color, 107, 38, 21): IncrementTroopCounter("Valk"); return true;
        case var _ when IsColorMatch(color, 126, 42, 20): IncrementTroopCounter("Valk"); return true;
        case var _ when IsColorMatch(color, 135, 43, 19): IncrementTroopCounter("Valk"); return true;
        case var _ when IsColorMatch(color, 101, 37, 21): IncrementTroopCounter("Valk"); return true;
        case var _ when IsColorMatch(color, 115, 38, 20): IncrementTroopCounter("Valk"); return true;
        case var _ when IsColorMatch(color, 100, 98, 233): IncrementTroopCounter("Witch"); return true;
        case var _ when IsColorMatch(color, 117, 139, 255): IncrementTroopCounter("Witch"); return true;
        case var _ when IsColorMatch(color, 40, 30, 72): IncrementTroopCounter("Witch"); return true;
        case var _ when IsColorMatch(color, 49, 38, 87): IncrementTroopCounter("Witch"); return true;
        case var _ when IsColorMatch(color, 53, 39, 92): IncrementTroopCounter("Witch"); return true;
        case var _ when IsColorMatch(color, 191, 173, 189): IncrementTroopCounter("Lavahound"); return true;
        case var _ when IsColorMatch(color, 72, 69, 76): IncrementTroopCounter("Lavahound"); return true;
        case var _ when IsColorMatch(color, 239, 239, 230): IncrementTroopCounter("Icegolem"); return true;
        case var _ when IsColorMatch(color, 232, 231, 220): IncrementTroopCounter("Icegolem"); return true;
        case var _ when IsColorMatch(color, 212, 80, 255): IncrementTroopCounter("Apprentice"); return true;
        case var _ when IsColorMatch(color, 209, 90, 170): IncrementTroopCounter("Archer"); return true;
        case var _ when IsColorMatch(color, 119, 85, 56): IncrementTroopCounter("Goblin"); return true;
        case var _ when IsColorMatch(color, 111, 93, 88): IncrementTroopCounter("Wizard"); return true;
        case var _ when IsColorMatch(color, 152, 217, 246): IncrementTroopCounter("Wizard"); return true;
        case var _ when IsColorMatch(color, 93, 33, 92): IncrementTroopCounter("Dragon"); return true;
        case var _ when IsColorMatch(color, 90, 42, 111): IncrementTroopCounter("Dragon"); return true;
        case var _ when IsColorMatch(color, 106, 188, 101): IncrementTroopCounter("Babydragon"); return true;
        case var _ when IsColorMatch(color, 105, 185, 96): IncrementTroopCounter("Babydragon"); return true;
        case var _ when IsColorMatch(color, 123, 177, 252): IncrementTroopCounter("Edragon"); return true;
        case var _ when IsColorMatch(color, 145, 128, 113): IncrementTroopCounter("Dragonrider"); return true;
        case var _ when IsColorMatch(color, 59, 41, 52): IncrementTroopCounter("Rootrider"); return true;
        case var _ when IsColorMatch(color, 31, 25, 22): IncrementTroopCounter("Hogrider"); return true;
        case var _ when IsColorMatch(color, 39, 38, 39): IncrementTroopCounter("Hogrider"); return true;
        case var _ when IsColorMatch(color, 249, 255, 216): IncrementTroopCounter("Hogrider"); return true;
        case var _ when IsColorMatch(color, 73, 78, 162): IncrementTroopCounter("Golem"); return true;
        case var _ when IsColorMatch(color, 104, 118, 129): IncrementTroopCounter("Golem"); return true;
        case var _ when IsColorMatch(color, 149, 179, 185): IncrementTroopCounter("Bowler"); return true;
        case var _ when IsColorMatch(color, 85, 81, 130): IncrementTroopCounter("Headhunter"); return true;
        case var _ when IsColorMatch(color, 188, 113, 76): IncrementTroopCounter("Druid"); return true;
        case var _ when IsColorMatch(color, 169, 124, 124): IncrementTroopCounter("Druid"); return true;

        case var _ when IsColorMatch(color, 209, 174, 164): IncrementTroopCounter("Superwallbreaker"); return true;
        case var _ when IsColorMatch(color, 196, 175, 193): IncrementTroopCounter("Superwitch"); return true;

        case var _ when IsColorMatch(color, 68, 236, 255): IncrementSpellCounter("Lightning"); return true;
        case var _ when IsColorMatch(color, 249, 249, 252): IncrementSpellCounter("Rage"); return true;
        case var _ when IsColorMatch(color, 212, 219, 238): IncrementSpellCounter("Rage"); return true;
        case var _ when IsColorMatch(color, 88, 252, 255): IncrementSpellCounter("Freeze"); return true;
        case var _ when IsColorMatch(color, 138, 210, 192): IncrementSpellCounter("Invis"); return true;
        case var _ when IsColorMatch(color, 240, 118, 18): IncrementSpellCounter("Poison"); return true;
        case var _ when IsColorMatch(color, 244, 124, 18): IncrementSpellCounter("Poison"); return true;
        case var _ when IsColorMatch(color, 254, 217, 242): IncrementSpellCounter("Haste"); return true;
        case var _ when IsColorMatch(color, 226, 217, 246): IncrementSpellCounter("Bats"); return true;
        case var _ when IsColorMatch(color, 138, 138, 181): IncrementSpellCounter("Bats"); return true;
        case var _ when IsColorMatch(color, 200, 181, 139): IncrementSpellCounter("Heal"); return true;
        case var _ when IsColorMatch(color, 247, 239, 196): IncrementSpellCounter("Heal"); return true;
        case var _ when IsColorMatch(color, 255, 255, 239): IncrementSpellCounter("Jump"); return true;
        case var _ when IsColorMatch(color, 250, 255, 230): IncrementSpellCounter("Jump"); return true;
        case var _ when IsColorMatch(color, 148, 167, 204): IncrementSpellCounter("Clone"); return true;
        case var _ when IsColorMatch(color, 222, 149, 191): IncrementSpellCounter("Recall"); return true;
        case var _ when IsColorMatch(color, 255, 252, 255): IncrementSpellCounter("Recall"); return true;
        case var _ when IsColorMatch(color, 81, 65, 52): IncrementSpellCounter("Earthquake"); return true;
        case var _ when IsColorMatch(color, 69, 60, 51): IncrementSpellCounter("Earthquake"); return true;
        case var _ when IsColorMatch(color, 203, 53, 50): IncrementSpellCounter("Skeleton"); return true;
        case var _ when IsColorMatch(color, 245, 160, 153): IncrementSpellCounter("Skeleton"); return true;
        case var _ when IsColorMatch(color, 238, 126, 119): IncrementSpellCounter("Skeleton"); return true;
        case var _ when IsColorMatch(color, 145, 178, 84): IncrementSpellCounter("Overgrowth"); return true;
        case var _ when IsColorMatch(color, 51, 23, 15): IncrementSpellCounter("Overgrowth"); return true;
        case var _ when IsColorMatch(color, 73, 56, 36): IncrementSpellCounter("Overgrowth"); return true;

        case var _ when IsColorMatch(color, 91, 43, 13): IncrementSiegeCounter("Flameflinger"); return true;
        case var _ when IsColorMatch(color, 235, 118, 122): IncrementSiegeCounter("Blimp"); return true;
        case var _ when IsColorMatch(color, 75, 75, 84): IncrementSiegeCounter("Loglauncher"); return true;
        case var _ when IsColorMatch(color, 75, 74, 78): IncrementSiegeCounter("Loglauncher"); return true;
        case var _ when IsColorMatch(color, 70, 67, 69): IncrementSiegeCounter("Loglauncher"); return true;

        default:
          Console.WriteLine("Unknown color encountered: " + color);
          return false; ;
      }
    }

    private bool IsColorMatch(Color color, byte r, byte g, byte b, int tolerance = 4)
    {
      // Check if the given color is within a certain tolerance of the target color
      return Math.Abs(color.R - r) <= tolerance && Math.Abs(color.G - g) <= tolerance && Math.Abs(color.B - b) <= tolerance;
    }


    void NavigateToTrainTroops()
    {
      Thread.Sleep(2000);
      ClickPosition(800, 500);
      ClickPosition(100, 800);
      ClickPosition(600, 100);
    }

    void NavigateFromTroopsToTrainSpells()
    {
      ClickPosition(850, 100);
    }

    void NavigateToChat()
    {
      ClickPosition(1650, 100);
      ClickPosition(100, 500);
    }

    void ScrollToRight()
    {
      Drag(1645, 740, 250, 740);
    }

    void ScrollToLeft()
    {
      Drag(250, 740, 1645, 740);
    }

    void TrainTroops()
    {
      var keys = troopColors.Keys.ToList();

      for (int j = 0; j < keys.Count; j++)
      {
        string key = keys[j];
        int count = troopColors[key].Count;

        if (count != 0)
        {
          Point? position = SearchRows(0, key, troopColors);

          if (position == null)
          {
            position = SearchRows(1, key, troopColors);
          }

          if (position == null)
          {
            ScrollToRight();
            position = SearchRows(0, key, troopColors);
          }

          if (position == null)
          {
            position = SearchRows(1, key, troopColors);
          }

          if (position == null)
          {
            Console.WriteLine("Troop " + key + " not found when trying to recruit");
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          troopColors[key] = (troopColors[key].Color, 0);
          ScrollToLeft();
        }
      }
    }

    void TrainSpells()
    {
      var keys = spellColors.Keys.ToList();

      for (int j = 0; j < keys.Count; j++)
      {
        string key = keys[j];
        int count = spellColors[key].Count;

        if (count != 0)
        {
          Point? position = SearchRows(0, key, spellColors);

          if (position == null)
          {
            position = SearchRows(1, key, spellColors);
          }

          if (position == null)
          {
            Console.WriteLine("Spell " + key + " not found when trying to recruit");
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          spellColors[key] = (spellColors[key].Color, 0);
        }
      }
    }

    Point? SearchRows(int rowNum, string key, Dictionary<string, (Color Color, int Count)> colorDictionary)
    {
      int y = 646 + (rowNum * 187);
      Rectangle captureArea = new Rectangle(0, y, 1920, 1);
      Bitmap image = CaptureScreen(captureArea);

      // Loop through each pixel and check for matching RGB values
      for (int i = 0; i < image.Width; i++)
      {
        Color pixelColor = image.GetPixel(i, 0);

        if (colorDictionary.ContainsKey(key) && pixelColor == colorDictionary[key].Color)
        {
          Console.WriteLine($"{key} found at X position {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");
          return new Point(i, y);
        }
      }
      return null;
    }

    void IncrementTroopCounter(string key)
    {
      if (troopColors.ContainsKey(key))
      {
        var (color, count) = troopColors[key];
        troopColors[key] = (color, count + 1);
      }
    }

    void IncrementSpellCounter(string key)
    {
      if (spellColors.ContainsKey(key))
      {
        var (color, count) = spellColors[key];
        spellColors[key] = (color, count + 1);
      }
    }

    void IncrementSiegeCounter(string key)
    {
      if (siegeColors.ContainsKey(key))
      {
        var (color, count) = siegeColors[key];
        siegeColors[key] = (color, count + 1);
      }
    }

    private void ClickPosition(int x, int y)
    {
      System.Windows.Forms.Cursor.Position = new Point(x, y);
      Thread.Sleep(200);
      mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
      mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
      Thread.Sleep(500);
    }

    public static void Drag(int startX, int startY, int endX, int endY)
    {
      SetCursorPosition(startX, startY);
      Thread.Sleep(100);
      mouse_event(MOUSEEVENTF_LEFTDOWN, startX, startY, 0, 0);
      Thread.Sleep(100);
      SetCursorPosition(endX, endY);
      Thread.Sleep(100);
      mouse_event(MOUSEEVENTF_LEFTUP, endX, endY, 0, 0);
      Thread.Sleep(2000);
    }

    public static void SetCursorPosition(int x, int y)
    {
      System.Windows.Forms.Cursor.Position = new Point(x, y);
    }

    public Bitmap CaptureScreen(Rectangle bounds)
    {
      IntPtr desktopWnd = GetDesktopWindow();
      IntPtr desktopDC = GetWindowDC(desktopWnd);
      IntPtr memoryDC = CreateDC("DISPLAY", null, null, IntPtr.Zero);

      Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
      using (Graphics g = Graphics.FromImage(bitmap))
      {
        IntPtr bitmapDC = g.GetHdc();
        BitBlt(bitmapDC, 0, 0, bounds.Width, bounds.Height, desktopDC, bounds.X, bounds.Y, SRCCOPY);
        g.ReleaseHdc(bitmapDC);
      }

      ReleaseDC(desktopWnd, desktopDC);
      DeleteDC(memoryDC);

      return bitmap;
    }

    bool NextDonationUp(Color color)
    {
      if (color.R == 163 && color.G == 215 && color.B == 17)
      {
        ClickPosition(700, 150);
        Thread.Sleep(500);
        return true;
      }
      return false;
    }

    bool NextDonationDown(Color color)
    {
      if (color.R == 162 && color.G == 213 && color.B == 15)
      {
        ClickPosition(700, 880);
        Thread.Sleep(500);
        return true;
      }
      return false;
    }

    void GetPictures()
    {
      int topLeftX = 291;
      int topLeftY = 646;

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 181 * i, topLeftY, 1, 1);
        Bitmap image = CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Console.WriteLine($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");

        image.Save("screenshotTop" + i + ".png", ImageFormat.Png);
        Thread.Sleep(500);
      }

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 181 * i, topLeftY + 187, 10, 10);
        Bitmap image = CaptureScreen(captureArea);

        image.Save("screenshotBottom" + i + ".png", ImageFormat.Png);
        Thread.Sleep(500);
      }
    }
  }
}