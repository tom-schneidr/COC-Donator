using COCDonator.Properties;
using COCDonator.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
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
using static System.Windows.Forms.AxHost;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

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

    bool IsGray(Color color) => color.R == color.G && color.G == color.B || (color.R == color.G && color.B == 213) || (color.R == color.G && color.B == 187);

    int sessionDonations = 0;
    DateTime lastNotFoundTime = DateTime.MinValue;

    private Dictionary<string, (Bitmap Image, int Count)> troopImages = new Dictionary<string, (Bitmap Image, int Count)>
    {
      { "Barbarian", (Properties.Resources.Barbarian, 0) },
      { "Giant", (Properties.Resources.Giant, 0) },
      { "Wallbreaker", (Properties.Resources.Wallbreaker, 0) },
      { "Wizard", (Properties.Resources.Wizard, 0) },
      { "Dragon", (Properties.Resources.Dragon, 0) },
      { "Babydragon", (Properties.Resources.Babydragon, 0) },
      { "Edragon", (Properties.Resources.Edragon, 0) },
      { "Dragonrider", (Properties.Resources.Dragonrider, 0) },
      { "Archer", (Properties.Resources.Archer, 0) },
      { "Goblin", (Properties.Resources.Goblin, 0) },
      { "Balloon", (Properties.Resources.Balloon, 0) },
      { "Healer", (Properties.Resources.Healer, 0) },
      { "Pekka", (Properties.Resources.Pekka, 0) },
      { "Miner", (Properties.Resources.Miner, 0) },
      { "Yeti", (Properties.Resources.Yeti, 0) },
      { "Etitan", (Properties.Resources.Etitan, 0) },
      { "Minion", (Properties.Resources.Minion, 0) },
      { "Valk", (Properties.Resources.Valk, 0) },
      { "Witch", (Properties.Resources.Witch, 0) },
      { "Lavahound", (Properties.Resources.Lavahound, 0) },
      { "Icegolem", (Properties.Resources.Icegolem, 0) },
      { "Apprentice", (Properties.Resources.Apprentice, 0) },
      { "Rootrider", (Properties.Resources.Rootrider, 0) },
      { "Hogrider", (Properties.Resources.Hogrider, 0) },
      { "Golem", (Properties.Resources.Golem, 0) },
      { "Bowler", (Properties.Resources.Bowler, 0) },
      { "Headhunter", (Properties.Resources.Headhunter, 0) },
      { "Druid", (Properties.Resources.Druid, 0) },
      { "Thrower", (Properties.Resources.Thrower, 0) },
      { "SuperArcher", (Properties.Resources.SuperArcher, 0) }
    };

    private Dictionary<string, (Bitmap Image, int Count)> spellImages = new Dictionary<string, (Bitmap Image, int Count)>
    {
      { "Lightning", (Properties.Resources.Lightning, 0) },
      { "Rage", (Properties.Resources.Rage, 0) },
      { "Freeze", (Properties.Resources.Freeze, 0) },
      { "Invis", (Properties.Resources.Invis, 0) },
      { "Poison", (Properties.Resources.Poison, 0) },
      { "Haste", (Properties.Resources.Haste, 0) },
      { "Bats", (Properties.Resources.Bats, 0) },
      { "Heal", (Properties.Resources.Heal, 0) },
      { "Jump", (Properties.Resources.Jump, 0) },
      { "Clone", (Properties.Resources.Clone, 0) },
      { "Recall", (Properties.Resources.Recall, 0) },
      { "Earthquake", (Properties.Resources.Earthquake, 0) },
      { "Skeleton", (Properties.Resources.Skeleton, 0) },
      { "Overgrowth", (Properties.Resources.Overgrowth, 0) }
    };

    private Dictionary<string, (Bitmap Image, int Count)> siegeImages = new Dictionary<string, (Bitmap Image, int Count)>
    {
      { "WallWrecker", (Properties.Resources.WallWrecker, 0) },
      { "Blimp", (Properties.Resources.Blimp, 0) },
      { "StoneSlammer", (Properties.Resources.StoneSlammer, 0) },
      { "SiegeBarracks", (Properties.Resources.SiegeBarracks, 0) },
      { "LogLauncher", (Properties.Resources.LogLauncher, 0) },
      { "FlameFlinger", (Properties.Resources.FlameFlinger, 0) }
    };

    public MainWindow()
    {
      InitializeComponent();

      //Thread.Sleep(5000);
      //GetPictures();

      Dispatcher.Invoke(() => CounterTextBox.Text = $"Troops Donated:\n{sessionDonations}");
      Dispatcher.Invoke(() => LogTextBox.AppendText($"Application starting... ({DateTime.Now})\n"));
      StartFindDonateButtonTask();
    }

    private void StartFindDonateButtonTask()
    {
      // Task to run FindDonateButton and handle exceptions
      Task.Run(() =>
      {
        try
        {
          FindDonateButton();
        }
        catch (Exception ex)
        {
          // Log the exception
          Dispatcher.Invoke(() => LogTextBox.AppendText($"Exception in FindDonateButton: {ex.Message}\n"));
          Dispatcher.Invoke(() => LogTextBox.AppendText($"Stack Trace: {ex.StackTrace}\n"));

          // Restart the task if it fails
          Dispatcher.Invoke(() => LogTextBox.AppendText($"Restarting the task... ({DateTime.Now})\n"));
          StartFindDonateButtonTask(); // Restart the task
        }
      });
    }

    public void FindDonateButton()
    {
      DateTime lastTime = DateTime.MinValue;
      DateTime commandTime = DateTime.MinValue;

      bool moveDirection = true; // true = up, false = down
      // Loads current screen into bitmap
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = new Bitmap(bounds.Width, bounds.Height);
      while (true)
      {
        Random random = new Random();
        int randomSleepTime = random.Next(500, 1500);
        Thread.Sleep(randomSleepTime);

        fullscreen?.Dispose(); // Dispose the previous bitmap
        fullscreen = ScreenCapture.CaptureScreen(bounds);

        for (int y = 0; y < 1080; y++)
        {
          Color fullPixel = fullscreen.GetPixel(480, y);
          if (IsColorMatch(fullPixel, 221, 246, 133))
          {
            if (!IsColorMatch(GetPixelColor(480, y), 221, 246, 133)) continue;
            ClickPosition(480, y);
            DonateTroops();
            y += 50;
          }
        }
        if (moveDirection)
        {
          moveDirection = NextDonationUp(fullscreen.GetPixel(570, 100));
          if (!moveDirection) NextDonationDown(fullscreen.GetPixel(570, 935));
        }
        else
        {
          moveDirection = !NextDonationDown(fullscreen.GetPixel(570, 935));
          if (moveDirection) NextDonationUp(fullscreen.GetPixel(570, 100));
        }

        if ((DateTime.Now - lastTime).TotalMinutes >= 1)
        {
          ReconnectToGame();
          if ((GetTotalCount(troopImages) + GetTotalCount(spellImages) + GetTotalCount(siegeImages)) > 0)
          {
            NavigateToTrainTroops();
            TrainTroops();
            NavigateFromTroopsToTrainSpells();
            TrainSpells();
            NavigateFromSpellsToSiege();
            TrainSiege();
            NavigateToChat();
          }
          lastTime = DateTime.Now;
          commandTime = lastTime.AddSeconds(15);
        }

        // Check if 15 seconds have passed since last reconnect
        if (DateTime.Now >= commandTime)
        {
          NavigateToChat();
          commandTime = DateTime.MaxValue; // Prevent further execution until next reconnect
        }
      }
    }

    private int GetTotalCount(Dictionary<string, (Bitmap Image, int Count)> items)
    {
      return items.Values.Sum(item => item.Count);
    }

    void ReconnectToGame()
    {
      // Time based kick, kicked by player, lost connection
      ClickPosition(600, 600);
      // Reload after downtime
      ClickPosition(1500, 950);
    }

    public void DonateTroops()
    {
      int x = 700;
      int y = 120;

      Point ?p = FindExitButton();
      if (p == null)
      {
        ClickPosition(1835, 40);
        return;
      }
      FillTroops(x, y + p.Value.Y - 13);
      FillSpells(x, y + 347 + p.Value.Y - 13);
      ClickPosition(p.Value.X + 25, p.Value.Y + 25);
    }

    private void FillTroops(int x, int y)
    {
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = null;

      try
      {
        fullscreen = ScreenCapture.CaptureScreen(bounds);

        bool movingDown = true; // Track the direction: down or up
        int counter = 0;

        while (true)
        {
          // Check if the counter exceeds the threshold
          if (counter > 13)
          {
            break;
          }

          Color fullPixel = fullscreen.GetPixel(x, y);

          // Click the position until it becomes gray
          while (!IsGray(fullPixel))
          {
            if (!IsGray(fullscreen.GetPixel(630, 450)))
            {
              return; // Exit if donation window is closed
            }

            if (CountColor(fullPixel))
            {
              ClickPosition(x, y);
            }
            else
            {
              break; // Exit the loop if condition fails
            }

            Dispatcher.Invoke(() => CounterTextBox.Text = $"Troops Donated:\n{++sessionDonations}");

            // Capture a new screen and update the pixel
            fullscreen?.Dispose(); // Dispose of the previous bitmap safely
            fullscreen = ScreenCapture.CaptureScreen(bounds);
            fullPixel = fullscreen.GetPixel(x, y);
          }

          // Move to the next position in a zigzag pattern
          if (movingDown)
          {
            // Move to the second row
            y += 146;
            movingDown = false;
          }
          else
          {
            // Move back to the first row and to the next column
            y -= 146;
            x += 114;
            movingDown = true;
          }

          counter++;
        }
      }
      catch (Exception ex)
      {
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Exception in FillTroops: {ex.Message}\n"));
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Stack Trace: {ex.StackTrace}\n"));
      }
      finally
      {
        // Ensure fullscreen bitmap is disposed of properly
        fullscreen?.Dispose();
      }
    }

    void FillSpells(int x, int y)
    {
      Rectangle bounds = Screen.PrimaryScreen.Bounds;
      Bitmap fullscreen = null;

      try
      {
        fullscreen = ScreenCapture.CaptureScreen(bounds);

        int counter = 0;

        while (true)
        {
          // Check if the counter exceeds the threshold
          if (counter > 6)
          {
            break;
          }

          Color fullPixel = fullscreen.GetPixel(x, y);

          // Click the position until it becomes gray
          while (!IsGray(fullPixel))
          {
            if (!IsGray(fullscreen.GetPixel(630, 450)))
            {
              return; // Exit if donation window is closed
            }

            if (CountColor(fullPixel))
            {
              ClickPosition(x, y);
            }
            else
            {
              break; // Exit the loop if condition fails
            }

            Dispatcher.Invoke(() => CounterTextBox.Text = $"Troops Donated:\n{++sessionDonations}");

            // Capture a new screen and update the pixel
            fullscreen?.Dispose(); // Dispose of the previous bitmap safely
            fullscreen = ScreenCapture.CaptureScreen(bounds);
            fullPixel = fullscreen.GetPixel(x, y);
          }

          // Move to the next column
          x += 114;
          counter++;
        }
      }
      catch (Exception ex)
      {
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Exception in FillSpells: {ex.Message}\n"));
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Stack Trace: {ex.StackTrace}\n"));
      }
      finally
      {
        // Ensure fullscreen bitmap is disposed of properly
        fullscreen?.Dispose();
      }
    }


    public bool CountColor(Color color)
    {
      // Match the color against known RGB values
      switch ((color.R, color.G, color.B))
      {
        case var _ when IsColorMatch(color, 255, 213, 89): IncrementTroopCounter("Barbarian"); Console.WriteLine("Barbarian"); return true;//
        case var _ when IsColorMatch(color, 151, 85, 59): IncrementTroopCounter("Giant"); Console.WriteLine("Giant"); return true;//
        case var _ when IsColorMatch(color, 159, 88, 61): IncrementTroopCounter("Giant"); Console.WriteLine("Giant"); return true;//
        case var _ when IsColorMatch(color, 43, 41, 40): IncrementTroopCounter("Wallbreaker"); Console.WriteLine("Wallbreaker"); return true;//
        case var _ when IsColorMatch(color, 211, 188, 161): IncrementTroopCounter("Balloon"); Console.WriteLine("Balloon"); return true;//
        case var _ when IsColorMatch(color, 204, 183, 157): IncrementTroopCounter("Balloon"); Console.WriteLine("Balloon"); return true;//
        case var _ when IsColorMatch(color, 255, 212, 187): IncrementTroopCounter("Healer"); Console.WriteLine("Healer"); return true;
        case var _ when IsColorMatch(color, 22, 19, 28): IncrementTroopCounter("Pekka"); Console.WriteLine("Pekka"); return true;//
        case var _ when IsColorMatch(color, 255, 182, 142): IncrementTroopCounter("Miner"); Console.WriteLine("Miner"); return true;//
        case var _ when IsColorMatch(color, 66, 68, 87): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 66, 68, 87): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 84, 88, 107): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 71, 74, 94): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 105, 110, 131): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 89, 94, 114): IncrementTroopCounter("Yeti"); Console.WriteLine("Yeti"); return true;//
        case var _ when IsColorMatch(color, 61, 79, 109): IncrementTroopCounter("Etitan"); Console.WriteLine("Etitan"); return true;//
        case var _ when IsColorMatch(color, 35, 92, 137): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 61, 71, 95): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 31, 80, 119): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 33, 85, 126): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 31, 71, 108): IncrementTroopCounter("Minion"); Console.WriteLine("Minion"); return true;//
        case var _ when IsColorMatch(color, 170, 119, 109): IncrementTroopCounter("Valk"); Console.WriteLine("Valk"); return true;//
        case var _ when IsColorMatch(color, 165, 111, 97): IncrementTroopCounter("Valk"); Console.WriteLine("Valk"); return true;//
        case var _ when IsColorMatch(color, 63, 70, 150): IncrementTroopCounter("Witch"); Console.WriteLine("Witch"); return true;//
        case var _ when IsColorMatch(color, 49, 37, 36): IncrementTroopCounter("Lavahound"); Console.WriteLine("Lavahound"); return true;//
        case var _ when IsColorMatch(color, 239, 239, 230): IncrementTroopCounter("Icegolem"); Console.WriteLine("Icegolem"); return true;//
        case var _ when IsColorMatch(color, 91, 88, 93): IncrementTroopCounter("Icegolem"); Console.WriteLine("Icegolem"); return true;//
        case var _ when IsColorMatch(color, 255, 120, 26): IncrementTroopCounter("Apprentice"); Console.WriteLine("Apprentice"); return true;
        case var _ when IsColorMatch(color, 165, 103, 86): IncrementTroopCounter("Archer"); Console.WriteLine("Archer"); return true;//
        case var _ when IsColorMatch(color, 175, 110, 92): IncrementTroopCounter("Archer"); Console.WriteLine("Archer"); return true;//
        case var _ when IsColorMatch(color, 104, 131, 50): IncrementTroopCounter("Goblin"); Console.WriteLine("Goblin"); return true;//
        case var _ when IsColorMatch(color, 111, 141, 55): IncrementTroopCounter("Goblin"); Console.WriteLine("Goblin"); return true;//
        case var _ when IsColorMatch(color, 242, 175, 150): IncrementTroopCounter("Wizard"); Console.WriteLine("Wizard"); return true;//
        case var _ when IsColorMatch(color, 245, 95, 108): IncrementTroopCounter("Dragon"); Console.WriteLine("Dragon"); return true;//
        case var _ when IsColorMatch(color, 170, 95, 88): IncrementTroopCounter("Babydragon"); Console.WriteLine("Babydragon"); return true;//
        case var _ when IsColorMatch(color, 48, 91, 164): IncrementTroopCounter("Edragon"); Console.WriteLine("Edragon"); return true;//
        case var _ when IsColorMatch(color, 245, 239, 216): IncrementTroopCounter("Dragonrider"); Console.WriteLine("Dragonrider"); return true;//
        case var _ when IsColorMatch(color, 169, 93, 77): IncrementTroopCounter("Rootrider"); Console.WriteLine("Rootrider"); return true;//
        case var _ when IsColorMatch(color, 96, 91, 77): IncrementTroopCounter("Hogrider"); Console.WriteLine("Hogrider"); return true;//
        case var _ when IsColorMatch(color, 163, 146, 127): IncrementTroopCounter("Golem"); Console.WriteLine("Golem"); return true;//
        case var _ when IsColorMatch(color, 147, 133, 118): IncrementTroopCounter("Golem"); Console.WriteLine("Golem"); return true;//
        case var _ when IsColorMatch(color, 69, 60, 189): IncrementTroopCounter("Bowler"); Console.WriteLine("Bowler"); return true;//
        case var _ when IsColorMatch(color, 31, 21, 20): IncrementTroopCounter("Headhunter"); Console.WriteLine("Headhunter"); return true;//
        case var _ when IsColorMatch(color, 247, 117, 69): IncrementTroopCounter("Druid"); Console.WriteLine("Druid"); return true;//
        case var _ when IsColorMatch(color, 255, 125, 73): IncrementTroopCounter("Druid"); Console.WriteLine("Druid"); return true;//

        case var _ when IsColorMatch(color, 255, 192, 173): IncrementTroopCounter("SuperArcher"); Console.WriteLine("SuperArcher"); return true;//

        case var _ when IsColorMatch(color, 15, 79, 255): IncrementSpellCounter("Lightning"); Console.WriteLine("Lightning"); return true;//
        case var _ when IsColorMatch(color, 83, 45, 121): IncrementSpellCounter("Rage"); Console.WriteLine("Rage"); return true;//
        case var _ when IsColorMatch(color, 245, 126, 75): IncrementSpellCounter("Freeze"); Console.WriteLine("Freeze"); return true;//
        case var _ when IsColorMatch(color, 99, 201, 234): IncrementSpellCounter("Freeze"); Console.WriteLine("Freeze"); return true;//
        case var _ when IsColorMatch(color, 68, 191, 129): IncrementSpellCounter("Invis"); Console.WriteLine("Invis"); return true;//
        case var _ when IsColorMatch(color, 72, 198, 133): IncrementSpellCounter("Invis"); Console.WriteLine("Invis"); return true;//
        case var _ when IsColorMatch(color, 245, 137, 93): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;//
        case var _ when IsColorMatch(color, 244, 157, 119): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;//
        case var _ when IsColorMatch(color, 245, 147, 106): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;//
        case var _ when IsColorMatch(color, 245, 142, 99): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;//
        case var _ when IsColorMatch(color, 247, 124, 67): IncrementSpellCounter("Poison"); Console.WriteLine("Poison"); return true;//
        case var _ when IsColorMatch(color, 252, 104, 171): IncrementSpellCounter("Haste"); Console.WriteLine("Haste"); return true;
        case var _ when IsColorMatch(color, 78, 45, 107): IncrementSpellCounter("Bats"); Console.WriteLine("Bats"); return true;
        case var _ when IsColorMatch(color, 255, 255, 237): IncrementSpellCounter("Heal"); Console.WriteLine("Heal"); return true;
        case var _ when IsColorMatch(color, 139, 245, 30): IncrementSpellCounter("Jump"); Console.WriteLine("Jump"); return true;//
        case var _ when IsColorMatch(color, 48, 226, 228): IncrementSpellCounter("Clone"); Console.WriteLine("Clone"); return true;//
        case var _ when IsColorMatch(color, 222, 149, 191): IncrementSpellCounter("Recall"); Console.WriteLine("Recall"); return true;
        case var _ when IsColorMatch(color, 179, 122, 82): IncrementSpellCounter("Earthquake"); Console.WriteLine("Earthquake"); return true;//
        case var _ when IsColorMatch(color, 123, 51, 40): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;//
        case var _ when IsColorMatch(color, 138, 51, 40): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;//
        case var _ when IsColorMatch(color, 127, 41, 29): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;//
        case var _ when IsColorMatch(color, 119, 32, 18): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;//
        case var _ when IsColorMatch(color, 126, 33, 17): IncrementSpellCounter("Skeleton"); Console.WriteLine("Skeleton"); return true;//
        case var _ when IsColorMatch(color, 255, 255, 161): IncrementSpellCounter("Overgrowth"); Console.WriteLine("Overgrowth"); return true;//

        case var _ when IsColorMatch(color, 122, 127, 134): IncrementSiegeCounter("FlameFlinger"); Console.WriteLine("FlameFlinger"); return true;
        case var _ when IsColorMatch(color, 199, 99, 79): IncrementSiegeCounter("Blimp"); Console.WriteLine("Blimp"); return true;//
        case var _ when IsColorMatch(color, 206, 94, 77): IncrementSiegeCounter("Blimp"); Console.WriteLine("Blimp"); return true;//
        case var _ when IsColorMatch(color, 114, 119, 145): IncrementSiegeCounter("LogLauncher"); Console.WriteLine("LogLauncher"); return true;//
        case var _ when IsColorMatch(color, 115, 125, 152): IncrementSiegeCounter("LogLauncher"); Console.WriteLine("LogLauncher"); return true;//
        case var _ when IsColorMatch(color, 166,  59, 50): IncrementSiegeCounter("StoneSlammer"); Console.WriteLine("StoneSlasmmer"); return true;//
        case var _ when IsColorMatch(color, 154, 47, 46): IncrementSiegeCounter("SiegeBarracks"); Console.WriteLine("SiegeBarracks"); return true;//


        default:
          Dispatcher.Invoke(() => LogTextBox.AppendText("Unknown color encountered: " + color + "\n"));
          return false; ;
      }
    }

    private bool IsColorMatch(Color color, byte r, byte g, byte b, int tolerance = 6)
    {
      return Math.Abs(color.R - r) <= tolerance &&
             Math.Abs(color.G - g) <= tolerance &&
             Math.Abs(color.B - b) <= tolerance;
    }


    bool AreBitmapsSimilar(Bitmap bmp1, Bitmap bmp2, int tolerance = 6, double matchPercentage = 0.8)
    {
      if (bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height)
        return false;

      int totalPixels = bmp1.Width * bmp1.Height;
      int matchingPixels = 0;

      for (int y = 0; y < bmp1.Height; y++)
      {
        for (int x = 0; x < bmp1.Width; x++)
        {
          Color color1 = bmp1.GetPixel(x, y);
          Color color2 = bmp2.GetPixel(x, y);

          if (IsColorMatch(color1, color2.R, color2.G, color2.B, tolerance))
            matchingPixels++;
        }
      }

      double similarity = (double)matchingPixels / totalPixels;
      return similarity >= matchPercentage;
    }

    static Color GetPixelColor(int x, int y)
    {
      using (Bitmap screenshot = new Bitmap(1, 1))
      {
        using (Graphics g = Graphics.FromImage(screenshot))
        {
          // Copy the pixel from the screen to the bitmap
          g.CopyFromScreen(x, y, 0, 0, new Size(1, 1));
        }
        // Get the color of the pixel
        return screenshot.GetPixel(0, 0);
      }
    }

    void NavigateToTrainTroops()
    {
      ClickPosition(650, 500);
      ClickPosition(100, 800);
      ClickPosition(600, 150);
    }

    void NavigateFromTroopsToTrainSpells()
    {
      ClickPosition(850, 150);
    }

    private void NavigateFromSpellsToSiege()
    {
      ClickPosition(1200, 150);
    }

    void NavigateToChat()
    {
      ClickPosition(1600, 150);
      ClickPosition(100, 500);
      ClickPosition(650, 200);
      ClickPosition(30, 950);
    }

    void ScrollToRight()
    {
      Drag(1600, 730, 300, 730);
    }

    void ScrollToLeft()
    {
      Drag(300, 730, 1600, 730);
    }

    void TrainTroops()
    {
      var keys = troopImages.Keys.ToList();
      int troopsNotFound = 0;

      for (int j = 0; j < keys.Count; j++)
      {
        int scrolled = 0;
        string key = keys[j];
        int count = troopImages[key].Count;

        if (count != 0)
        {
          // Check if got disconnected
          Color disconnectColorCheck = GetPixelColor(100, 500);
          if (disconnectColorCheck.R == 25 && disconnectColorCheck.G == 28 && disconnectColorCheck.B == 30) return;

          Point? position = SearchRows(620, troopImages[key].Image);

          if (position == null)
          {
            position = SearchRows(620 + 171, troopImages[key].Image);
          }

          if (position == null)
          {
            ScrollToRight();
            scrolled++;
            position = SearchRows(620, troopImages[key].Image);
          }

          if (position == null)
          {
            position = SearchRows(620 + 171, troopImages[key].Image);
          }

          if (position == null)
          {
            ScrollToRight();
            scrolled++;
            position = SearchRows(620, troopImages[key].Image);
          }

          if (position == null)
          {
            position = SearchRows(620 + 171, troopImages[key].Image);
          }

          if (position == null)
          {
            for (int i = 0; i < scrolled; i++)
            {
              ScrollToLeft();
            }
            Dispatcher.Invoke(() => LogTextBox.AppendText("Troop " + key + " not found when trying to recruit\n"));
            troopsNotFound++;
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          troopImages[key] = (troopImages[key].Image, 0);
          for (int i = 0; i < scrolled; i++)
          {
            ScrollToLeft();
          }
        }
      }
      if (troopsNotFound >= 3 && (DateTime.Now - lastNotFoundTime).TotalMinutes >= 15)
      {
        lastNotFoundTime = DateTime.Now;
        CloseClashOfClans();
        StartClashOfClans();
        Thread.Sleep(120000);
        return;
      }
    }

    void TrainSpells()
    {
      var keys = spellImages.Keys.ToList();

      for (int j = 0; j < keys.Count; j++)
      {
        string key = keys[j];
        int count = spellImages[key].Count;

        if (count != 0)
        {
          // Check if got disconnected
          Color disconnectColorCheck = GetPixelColor(100, 500);
          if (disconnectColorCheck.R == 25 && disconnectColorCheck.G == 28 && disconnectColorCheck.B == 30) return;

          Point? position = SearchRows(620, spellImages[key].Image);

          if (position == null)
          {
            position = SearchRows(620 + 171, spellImages[key].Image);
          }

          if (position == null)
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText("Spell " + key + " not found when trying to recruit\n"));
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          spellImages[key] = (spellImages[key].Image, 0);
        }
      }
    }

    void TrainSiege()
    {
      var keys = siegeImages.Keys.ToList();

      for (int j = 0; j < keys.Count; j++)
      {
        bool scrolled = false;
        string key = keys[j];
        int count = siegeImages[key].Count;

        if (count != 0)
        {
          // Check if got disconnected
          Color disconnectColorCheck = GetPixelColor(100, 500);
          if (disconnectColorCheck.R == 25 && disconnectColorCheck.G == 28 && disconnectColorCheck.B == 30) return;

          Point? position = SearchRows(620, siegeImages[key].Image);

          if (position == null)
          {
            ScrollToRight();
            scrolled = true;
            position = SearchRows(620, siegeImages[key].Image);
          }

          if (position == null)
          {
            ScrollToLeft();
            Dispatcher.Invoke(() => LogTextBox.AppendText("Siege machine " + key + " not found when trying to recruit\n"));
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          siegeImages[key] = (siegeImages[key].Image, 0);
          if (scrolled) ScrollToLeft();
        }
      }
    }

    Point? SearchRows(int y, Bitmap searchedBitmap, int tolerance = 15, double matchPercentage = 0.8)
    {
      int searchWidth = searchedBitmap.Width;
      int searchHeight = searchedBitmap.Height;

      // Define the capture area based on the height of the searchedBitmap
      Rectangle captureArea = new Rectangle(0, y, 1920, searchHeight);
      using Bitmap screenBitmap = ScreenCapture.CaptureScreen(captureArea);

      // Precompute pixel differences for the searched bitmap
      int totalPixels = searchWidth * searchHeight;
      int requiredMatches = (int)(totalPixels * matchPercentage);

      // Loop through each starting point in the row (sliding window)
      for (int x = 0; x <= screenBitmap.Width - searchWidth; x++)
      {
        int matchingPixelCount = 0;

        // Compare blocks of pixels instead of individual pixels
        for (int py = 0; py < searchHeight; py++)
        {
          for (int px = 0; px < searchWidth; px++)
          {
            // Get pixels from both images
            Color capturedPixel = screenBitmap.GetPixel(x + px, py);
            Color searchedPixel = searchedBitmap.GetPixel(px, py);

            // Compare the pixel with the tolerance
            if (AreColorsSimilar(capturedPixel, searchedPixel, tolerance))
            {
              matchingPixelCount++;
            }

            // Early exit if we don't meet the required match percentage
            if (totalPixels - (px + py * searchWidth) + matchingPixelCount < requiredMatches)
            {
              break;
            }
          }
        }

        // If sufficient matching pixels are found
        if (matchingPixelCount >= requiredMatches)
        {
          return new Point(x, y); // Return the starting point of the match
        }
      }

      return null; // No match found
    }

    // Helper function to compare two colors with a tolerance
    bool AreColorsSimilar(Color color1, Color color2, int tolerance)
    {
      int rDiff = Math.Abs(color1.R - color2.R);
      int gDiff = Math.Abs(color1.G - color2.G);
      int bDiff = Math.Abs(color1.B - color2.B);

      // Return true if the difference between the colors is within the tolerance range
      return rDiff <= tolerance && gDiff <= tolerance && bDiff <= tolerance;
    }


    void IncrementTroopCounter(string key)
    {
      if (troopImages.ContainsKey(key))
      {
        var (color, count) = troopImages[key];
        troopImages[key] = (color, count + 1);
      }
    }

    void IncrementSpellCounter(string key)
    {
      if (spellImages.ContainsKey(key))
      {
        var (color, count) = spellImages[key];
        spellImages[key] = (color, count + 1);
      }
    }

    void IncrementSiegeCounter(string key)
    {
      if (siegeImages.ContainsKey(key))
      {
        var (color, count) = siegeImages[key];
        siegeImages[key] = (color, count + 1);
      }
    }

    private void ClickPosition(int x, int y)
    {
      SetCursorPosition(x, y);
      mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
      mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
      Random random = new Random();
      int randomSleepTime = random.Next(600, 900);
      Thread.Sleep(randomSleepTime);
    }

    public static void Drag(int startX, int startY, int endX, int endY)
    {
      SetCursorPosition(startX, startY);
      mouse_event(MOUSEEVENTF_LEFTDOWN, startX, startY, 0, 0);
      Thread.Sleep(100);
      SetCursorPosition(endX, endY);
      Thread.Sleep(500);
      mouse_event(MOUSEEVENTF_LEFTUP, endX, endY, 0, 0);
      Thread.Sleep(1500);
    }

    public static void SetCursorPosition(int x, int y)
    {
      System.Windows.Forms.Cursor.Position = new Point(x, y);
      Thread.Sleep(100);
    }

    Point? FindExitButton()
    {
      // Capture the screen section where we expect to find the button
      Bitmap fullscreen = ScreenCapture.CaptureScreen(new Rectangle(1413, 13, 4, 1080));
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

            if (!IsColorMatch(screenPixel, buttonPixel.R, buttonPixel.G, buttonPixel.B, 5))
            {
              matchFound = false;
              break;
            }
          }

          if (!matchFound)
          {
            break;
          }
        }

        // If a match is found, return the top-left position of the match
        if (matchFound)
        {
          fullscreen.Dispose();
          buttonImage.Dispose();
          exitButton.Dispose();
          return new Point(1413, y + 13);
        }
      }

      fullscreen.Dispose();
      buttonImage.Dispose();
      exitButton.Dispose();
      // Return null if no match is found
      return null;
    }

    bool NextDonationUp(Color color)
    {
      if (IsColorMatch(color, 140, 199, 22))
      {
        ClickPosition(570, 100);
        return true;
      }
      return false;
    }

    bool NextDonationDown(Color color)
    {
      if (IsColorMatch(color, 163, 215, 17))
      {
        ClickPosition(570, 935);
        return true;
      }
      return false;
    }

    void CloseClashOfClans()
    {
      string clashOfClansProcessName = "Clash of Clans";

      try
      {
        // Search for a process with the name or title containing "Clash of Clans"
        var targetProcess = Process.GetProcesses()
            .FirstOrDefault(p => p.MainWindowTitle.Contains(clashOfClansProcessName) ||
                                 p.ProcessName.Contains("Clash"));

        if (targetProcess != null)
        {
          Dispatcher.Invoke(() => LogTextBox.AppendText($"Closing Clash of Clans... ({DateTime.Now})\n"));
          targetProcess.Kill(); // Forcefully terminate
          targetProcess.WaitForExit(); // Wait for the process to exit
          Dispatcher.Invoke(() => LogTextBox.AppendText("Clash of Clans closed successfully.\n"));
        }
        else
        {
          Dispatcher.Invoke(() => LogTextBox.AppendText("Clash of Clans process not found.\n"));
        }
      }
      catch (Exception ex)
      {
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Error closing Clash of Clans: {ex.Message}\n"));
      }
    }


    void StartClashOfClans()
    {
      // Get the path to the Start Menu Programs folder
      string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

      // Combine with the relative path to the shortcut
      string shortcutPath = System.IO.Path.Combine(startMenuPath, @"Programs\Google Play Games\Clash of Clans.lnk");

      // Start the shortcut
      if (File.Exists(shortcutPath))
      {
        Process.Start(shortcutPath);
      }
      else
      {
        Dispatcher.Invoke(() => LogTextBox.AppendText("Shortcut not found: " + shortcutPath + "\n"));
      }
    }

    void GetPictures()
    {
      NavigateToTrainTroops();
      int topLeftX = 350;
      int topLeftY = 620;

      SaveTroopImages(topLeftX, topLeftY, 8, 166, troopImages, "Top Troops");
      SaveTroopImages(topLeftX, topLeftY + 171, 8, 166, troopImages, "Bottom Troops");
      ScrollToRight();
      SaveTroopImages(topLeftX, topLeftY, 8, 166, troopImages, "Top Troops 2");
      SaveTroopImages(topLeftX, topLeftY + 171, 8, 166, troopImages, "Bottom Troops 2");
      ScrollToRight();
      SaveTroopImages(topLeftX, topLeftY, 8, 166, troopImages, "Top Troops 3");
      SaveTroopImages(topLeftX, topLeftY + 171, 8, 166, troopImages, "Bottom Troops 3");

      NavigateFromTroopsToTrainSpells();
      SaveTroopImages(topLeftX, topLeftY, 8, 166, spellImages, "Top Spells");
      SaveTroopImages(topLeftX, topLeftY + 171, 8, 166, spellImages, "Bottom Spells");

      NavigateFromSpellsToSiege();
      SaveTroopImages(topLeftX, topLeftY, 5, 295, siegeImages, "Top Siege");
      ScrollToRight();
      SaveTroopImages(topLeftX + 200, topLeftY, 5, 295, siegeImages, "Top Siege 2");
    }

    void SaveTroopImages(int startX, int startY, int count, int stepX, Dictionary<string, (Bitmap Image, int Count)> colorMap, string rowType)
    {
      for (int i = 0; i < count; i++)
      {
        // Define the capture area for a 20x20 region
        Rectangle captureArea = new Rectangle(startX + stepX * i, startY, 50, 50);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        // Save the image to the current directory with a unique name
        string fileName = $"{rowType}_Image_{i}_X{startX + stepX * i}_Y{startY}.png";
        image.Save(fileName, ImageFormat.Png);

        Dispatcher.Invoke(() => LogTextBox.AppendText($"{rowType} {i}: Image saved as {fileName}\n"));

        // Attempt to match the dominant color in the region to a known color
        bool found = false;
        foreach (string key in colorMap.Keys)
        {
          if (AreBitmapsSimilar(image, colorMap[key].Image))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText($"{key} identified\n"));
            found = true;
            break;
          }
        }
        if (!found) Dispatcher.Invoke(() => LogTextBox.AppendText($"Not identified\n"));

        image.Dispose();
      }
    }

    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      LogTextBox.ScrollToEnd();
    }
  }
}