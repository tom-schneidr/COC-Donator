using COCDonator.Properties;
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
      { "Barbarian", (Color.FromArgb(240, 158, 113), 0) },
      { "Giant", (Color.FromArgb(255, 180, 129), 0) },
      { "Wallbreaker", (Color.FromArgb(31, 29, 24), 0) },
      { "Balloon", (Color.FromArgb(126, 95, 83), 0) },
      { "Healer", (Color.FromArgb(255, 235, 216), 0) },
      { "Pekka", (Color.FromArgb(90, 57, 135), 0) },
      { "Miner", (Color.FromArgb(132, 103, 94), 0) },
      { "Yeti", (Color.FromArgb(81, 85, 118), 0) },
      { "Archer", (Color.FromArgb(210, 58, 118), 0) },
      { "Goblin", (Color.FromArgb(180, 207, 104), 0) },
      { "Superwallbreaker", (Color.FromArgb(255, 255, 245), 0) },
      { "Wizard", (Color.FromArgb(255, 220, 196), 0) },
      { "Dragon", (Color.FromArgb(67, 25, 82), 0) },
      { "Babydragon", (Color.FromArgb(102, 55, 48), 0) },
      { "Edragon", (Color.FromArgb(50, 84, 131), 0) },
      { "Dragonrider", (Color.FromArgb(135, 76, 46), 0) },
      { "Etitan", (Color.FromArgb(71, 63, 61), 0) },
      { "Minion", (Color.FromArgb(39, 39, 39), 0) },
      { "Valk", (Color.FromArgb(81, 34, 20), 0) },
      { "Witch", (Color.FromArgb(48, 50, 105), 0) },
      { "Lavaloon", (Color.FromArgb(101, 100, 108), 0) },
      { "Icegolem", (Color.FromArgb(238, 236, 228), 0) },
      { "Apprentice", (Color.FromArgb(216, 90, 255), 0) },
      { "Rootrider", (Color.FromArgb(116, 60, 54), 0) },
      { "Hogrider", (Color.FromArgb(86, 53, 45), 0) },
      { "Golem", (Color.FromArgb(168, 51, 159), 0) },
      { "Superwitch", (Color.FromArgb(255, 162, 73), 0) },
      { "Bowler", (Color.FromArgb(158, 139, 248), 0) },
      { "Headhunter", (Color.FromArgb(15, 15, 17), 0) },
      { "Druid", (Color.FromArgb(235, 136, 79), 0) }
    };


    private Dictionary<string, (Color Color, int Count)> spellColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Lightning", (Color.FromArgb(15, 231, 255), 0) },
      { "Rage", (Color.FromArgb(102, 43, 157), 0) },
      { "Freeze", (Color.FromArgb(77, 224, 255), 0) },
      { "Invis", (Color.FromArgb(122, 244, 188), 0) },
      { "Poison", (Color.FromArgb(254, 196, 163), 0) },
      { "Haste", (Color.FromArgb(255, 117, 182), 0) },
      { "Bats", (Color.FromArgb(72, 46, 113), 0) },
      { "Heal", (Color.FromArgb(242, 203, 100), 0) },
      { "Jump", (Color.FromArgb(81, 208, 19), 0) },
      { "Clone", (Color.FromArgb(43, 236, 231), 0) },
      { "Recall", (Color.FromArgb(253, 118, 154), 0) },
      { "Earthquake", (Color.FromArgb(199, 146, 97), 0) },
      { "Skeleton", (Color.FromArgb(219, 129, 118), 0) },
      { "Overgrowth", (Color.FromArgb(103, 120, 25), 0) }
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

      //Thread.Sleep(5000);
      //var keys = troopColors.Keys.ToList();

      //for (int j = 0; j < keys.Count; j++)
      //{
      //  string key = keys[j];
      //  IncrementTroopCounter(key);
      //}

      //keys = spellColors.Keys.ToList();

      //for (int j = 0; j < keys.Count; j++)
      //{
      //  string key = keys[j];
      //  IncrementSpellCounter(key);
      //}
      //TrainTroops();
      //NavigateFromTroopsToTrainSpells();
      //TrainSpells();
      Task.Run(() => FindDonateButton());
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
          if (!moveDirection) NextDonationDown(fullscreen.GetPixel(700, 910));
        }
        else
        {
          moveDirection = !NextDonationDown(fullscreen.GetPixel(700, 910));
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

      int x = 870;
      int y = 130;

      Point ?p = FindExitButton();
      if (p == null)
      {
        Console.WriteLine("Exit button not found");
        ClickPosition(1835, 40);
        return;
      }
      FillTroops(x, y + p.Value.Y - 17);
      FillSpells(x - 20, y + 410 + p.Value.Y - 17);
      ClickPosition(p.Value.X + 25, p.Value.Y + 25);
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
          if (!IsGray(fullscreen.GetPixel(760, 400))) return; // If donation window is closed

          if (CountColor(fullPixel))
            ClickPosition(x, y);
          else break;
          Console.WriteLine(++sessionDonations);
          TroopsToRecruit++;

          fullscreen = CaptureScreen(bounds);
          fullPixel = fullscreen.GetPixel(x, y);
        }
        // Move to the next position in a zigzag pattern
        if (movingDown)
        {
          // Move to the second row
          y += 176;
          movingDown = false;
        }
        else
        {
          // Move back to the first row and to the next column
          y -= 176;
          x += 137;
          movingDown = true;
        }
        counter++;
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
          if (!IsGray(fullscreen.GetPixel(760, 400))) return; // If donation window is closed

          if (CountColor(fullPixel))
            ClickPosition(x, y);
          else break;
          Console.WriteLine(++sessionDonations);
          TroopsToRecruit++;

          fullscreen = CaptureScreen(bounds);
          fullPixel = fullscreen.GetPixel(x, y);
          if (IsGray(fullPixel)) Console.WriteLine("Gray: " + fullPixel);
        }
        x += 137;
        counter++;
      }
    }

    public bool CountColor(Color color)
    {
      // Match the color against known RGB values
      switch ((color.R, color.G, color.B))
      {
        case var _ when IsColorMatch(color, 255, 190, 136): IncrementTroopCounter("Barbarian"); Console.WriteLine("Barbarian"); return true;//
        case var _ when IsColorMatch(color, 255, 184, 130): IncrementTroopCounter("Giant"); Console.WriteLine("Giant"); return true;//
        case var _ when IsColorMatch(color, 124, 116, 101): IncrementTroopCounter("Wallbreaker"); Console.WriteLine("Wallbreaker"); return true;//
        case var _ when IsColorMatch(color, 79, 65, 144): IncrementTroopCounter("Wallbreaker"); Console.WriteLine("Wallbreaker"); return true;//
        case var _ when IsColorMatch(color, 131, 103, 91): IncrementTroopCounter("Balloon"); Console.WriteLine("Balloon"); return true;//
        case var _ when IsColorMatch(color, 255, 212, 187): IncrementTroopCounter("Healer"); Console.WriteLine("Healer"); return true;//
        case var _ when IsColorMatch(color, 70, 47, 38): IncrementTroopCounter("Pekka"); Console.WriteLine("Pekka"); return true;//
        case var _ when IsColorMatch(color, 56, 55, 53): IncrementTroopCounter("Miner"); Console.WriteLine("Miner"); return true;//
        case var _ when IsColorMatch(color, 90, 93, 130): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 79, 82, 116): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 67, 45, 45): IncrementTroopCounter("Etitan"); Console.WriteLine("Etitan"); return true;
        case var _ when IsColorMatch(color, 46, 100, 139): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 53, 96, 130): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 81, 98, 108): IncrementTroopCounter("Valk"); Console.WriteLine("Valk"); return true;//
        case var _ when IsColorMatch(color, 134, 47, 89): IncrementTroopCounter("Witch"); Console.WriteLine("Witch"); return true;//
        case var _ when IsColorMatch(color, 133, 137, 152): IncrementTroopCounter("Lavahound"); Console.WriteLine("Lavahound"); return true;
        case var _ when IsColorMatch(color, 239, 239, 230): IncrementTroopCounter("Icegolem"); Console.WriteLine("Icegolem"); return true;
        case var _ when IsColorMatch(color, 255, 120, 26): IncrementTroopCounter("Apprentice"); Console.WriteLine("Apprentice"); return true;
        case var _ when IsColorMatch(color, 32, 66, 83): IncrementTroopCounter("Archer"); Console.WriteLine("Archer"); return true;//
        case var _ when IsColorMatch(color, 214, 227, 121): IncrementTroopCounter("Goblin"); Console.WriteLine("Goblin"); return true;//
        case var _ when IsColorMatch(color, 255, 196, 171): IncrementTroopCounter("Wizard"); Console.WriteLine("Wizard"); return true;//
        case var _ when IsColorMatch(color, 105, 203, 92): IncrementTroopCounter("Dragon"); Console.WriteLine("Dragon"); return true;//
        case var _ when IsColorMatch(color, 113, 206, 99): IncrementTroopCounter("Babydragon"); Console.WriteLine("Babydragon"); return true;//
        case var _ when IsColorMatch(color, 47, 91, 144): IncrementTroopCounter("Edragon"); Console.WriteLine("Edragon"); return true;
        case var _ when IsColorMatch(color, 182, 174, 150): IncrementTroopCounter("Dragonrider"); Console.WriteLine("Dragonrider"); return true;
        case var _ when IsColorMatch(color, 30, 33, 33): IncrementTroopCounter("Rootrider"); Console.WriteLine("Rootrider"); return true;//
        case var _ when IsColorMatch(color, 161, 97, 63): IncrementTroopCounter("Hogrider"); Console.WriteLine("Hogrider"); return true;//
        case var _ when IsColorMatch(color, 87, 33, 78): IncrementTroopCounter("Golem"); Console.WriteLine("Golem"); return true;
        case var _ when IsColorMatch(color, 170, 155, 245): IncrementTroopCounter("Bowler"); Console.WriteLine("Bowler"); return true;//
        case var _ when IsColorMatch(color, 130, 106, 145): IncrementTroopCounter("Headhunter"); Console.WriteLine("Headhunter"); return true;//
        case var _ when IsColorMatch(color, 183, 83, 80): IncrementTroopCounter("Druid"); Console.WriteLine("Druid"); return true;//

        case var _ when IsColorMatch(color, 121, 176, 246): IncrementTroopCounter("Superwallbreaker"); Console.WriteLine("Superwallbreaker"); return true;//
        case var _ when IsColorMatch(color, 196, 175, 193): IncrementTroopCounter("Superwitch"); Console.WriteLine("Superwitch"); return true;

        case var _ when IsColorMatch(color, 14, 108, 255): IncrementSpellCounter("Lightning"); Console.WriteLine("Lightning"); return true;
        case var _ when IsColorMatch(color, 248, 229, 245): IncrementSpellCounter("Rage"); Console.WriteLine("Rage"); return true;//
        case var _ when IsColorMatch(color, 137, 221, 253): IncrementSpellCounter("Freeze"); Console.WriteLine("Freeze"); return true;//
        case var _ when IsColorMatch(color, 141, 211, 207): IncrementSpellCounter("Invis"); Console.WriteLine("Invis"); return true;//
        case var _ when IsColorMatch(color, 255, 129, 23): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;
        case var _ when IsColorMatch(color, 252, 104, 171): IncrementSpellCounter("Haste"); Console.WriteLine("Haste"); return true;
        case var _ when IsColorMatch(color, 78, 45, 107): IncrementSpellCounter("Bats"); Console.WriteLine("Bats"); return true;
        case var _ when IsColorMatch(color, 255, 255, 237): IncrementSpellCounter("Heal"); Console.WriteLine("Heal"); return true;
        case var _ when IsColorMatch(color, 77, 186, 29): IncrementSpellCounter("Jump"); Console.WriteLine("Jump"); return true;
        case var _ when IsColorMatch(color, 30, 216, 221): IncrementSpellCounter("Clone"); Console.WriteLine("Clone"); return true;
        case var _ when IsColorMatch(color, 222, 149, 191): IncrementSpellCounter("Recall"); Console.WriteLine("Recall"); return true;
        case var _ when IsColorMatch(color, 148, 116, 88): IncrementSpellCounter("Earthquake"); Console.WriteLine("Earthquake"); return true;//
        case var _ when IsColorMatch(color, 145, 25, 27): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;
        case var _ when IsColorMatch(color, 115, 147, 39): IncrementSpellCounter("Overgrowth"); Console.WriteLine("Overgrowth"); return true;

        case var _ when IsColorMatch(color, 122, 127, 134): IncrementSiegeCounter("Flameflinger"); Console.WriteLine("Flameflinger"); return true;
        case var _ when IsColorMatch(color, 229, 92, 78): IncrementSiegeCounter("Blimp"); Console.WriteLine("Blimp"); return true;
        case var _ when IsColorMatch(color, 129, 31, 20): IncrementSiegeCounter("Loglauncher"); Console.WriteLine("Loglauncher"); return true;


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
      ClickPosition(1700, 100);
      ClickPosition(100, 500);
    }

    void ScrollToRight()
    {
      Drag(1700, 765, 200, 765);
    }

    void ScrollToLeft()
    {
      Drag(200, 765, 1700, 765);
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
          Point? position = SearchRows(646, key, troopColors);

          if (position == null)
          {
            position = SearchRows(646 + 198, key, troopColors);
          }

          if (position == null)
          {
            ScrollToRight();
            position = SearchRows(646, key, troopColors);
          }

          if (position == null)
          {
            position = SearchRows(646 + 198, key, troopColors);
          }

          if (position == null)
          {
            ScrollToLeft();
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
          Point? position = SearchRows(690, key, spellColors);

          if (position == null)
          {
            position = SearchRows(690 + 198, key, spellColors);
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

    Point? SearchRows(int y, string key, Dictionary<string, (Color Color, int Count)> colorDictionary)
    {
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

    Point? FindExitButton()
    {
      // Capture the screen section where we expect to find the button
      Bitmap fullscreen = CaptureScreen(new Rectangle(1697, 17, 5, 1080));
      Image buttonImage = Properties.Resources.ResourceManager.GetObject("exit_button") as Image;
      Bitmap exitButton = new Bitmap(buttonImage);

      int searchWidth = exitButton.Width;
      int searchHeight = exitButton.Height;
      int fullscreenHeight = fullscreen.Height;

      // Iterate through each vertical position in the captured screen area
      for (int y = 0; y <= fullscreenHeight - searchHeight; y++)
      {
        bool matchFound = true;

        // Compare each pixel in the area of fullscreen with exitButton
        for (int i = 0; i < searchHeight; i++)
        {
          for (int x = 0; x < searchWidth; x++)
          {
            Color screenPixel = fullscreen.GetPixel(x, y + i);
            Color buttonPixel = exitButton.GetPixel(x, i);

            if (!IsColorMatch(screenPixel, buttonPixel.R, buttonPixel.G, buttonPixel.B, 20))
            {
              matchFound = false;
              break;
            }
          }

          if (!matchFound)
            break;
        }

        // If a match is found, return the top-left position of the match
        if (matchFound)
          return new Point(1687, 17 + y);
      }

      // Return null if no match is found
      return null;
    }

    bool NextDonationUp(Color color)
    {
      if (color.R == 127)
      {
        ClickPosition(700, 150);
        return true;
      }
      return false;
    }

    bool NextDonationDown(Color color)
    {
      if (color.R == 137)
      {
        ClickPosition(700, 910);
        return true;
      }
      return false;
    }

    void GetPictures()
    {
      int topLeftX = 260; //Troops: 291 Spells: 260
      int topLeftY = 690; //Troops: 646 Spells: 690

      //for (int i = 0; i < 8; i++)
      //{
      //  Rectangle captureArea = new Rectangle(topLeftX + 192 * i, topLeftY, 1, 1);
      //  Bitmap image = CaptureScreen(captureArea);

      //  Color pixelColor = image.GetPixel(0, 0);
      //  Console.WriteLine($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");
      //  Thread.Sleep(100);
      //}

      //for (int i = 0; i < 8; i++)
      //{
      //  Rectangle captureArea = new Rectangle(topLeftX + 192 * i, topLeftY + 198, 1, 1);
      //  Bitmap image = CaptureScreen(captureArea);

      //  Color pixelColor = image.GetPixel(0, 0);
      //  Console.WriteLine($"Bottom Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");
      //  Thread.Sleep(100);
      //}

      //ScrollToRight();

      for (int i = 0; i < 7; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 192 * i, topLeftY, 1, 1);
        Bitmap image = CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Console.WriteLine($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");
        Thread.Sleep(100);
      }

      for (int i = 0; i < 7; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 192 * i, topLeftY + 198, 1, 1);
        Bitmap image = CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Console.WriteLine($"Bottom Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}");

      }
    }
  }
}