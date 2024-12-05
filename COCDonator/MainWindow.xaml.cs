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

    bool IsGray(Color color) => color.R == color.G && color.G == color.B || (color.R == color.G && color.B == 213) || (color.R == color.G && color.B == 187);

    int sessionDonations = 0;

    private Dictionary<string, (Color Color, int Count)> troopColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Barbarian", (Color.FromArgb(255, 235, 94), 0) },
      { "Giant", (Color.FromArgb(220, 134, 90), 0) },
      { "Wallbreaker", (Color.FromArgb(156, 146, 127), 0) },
      { "Wizard", (Color.FromArgb(255, 197, 170), 0) },
      { "Dragon", (Color.FromArgb(62, 47, 114), 0) },
      { "Babydragon", (Color.FromArgb(210, 116, 97), 0) },
      { "Edragon", (Color.FromArgb(154, 47, 86), 0) },
      { "Dragonrider", (Color.FromArgb(82, 30, 15), 0) },
      { "Archer", (Color.FromArgb(254, 166, 136), 0) },
      { "Goblin", (Color.FromArgb(150, 131, 74), 0) },
      { "Balloon", (Color.FromArgb(104, 99, 99), 0) },
      { "Healer", (Color.FromArgb(253, 177, 152), 0) },
      { "Pekka", (Color.FromArgb(86, 110, 148), 0) },
      { "Miner", (Color.FromArgb(138, 186, 254), 0) },
      { "Yeti", (Color.FromArgb(214, 228, 247), 0) },
      { "Etitan", (Color.FromArgb(151, 114, 123), 0) },
      { "Minion", (Color.FromArgb(105, 197, 250), 0) },
      { "Valk", (Color.FromArgb(227, 149, 121), 0) },
      { "Witch", (Color.FromArgb(125, 107, 131), 0) },
      { "Lavahound", (Color.FromArgb(55, 49, 51), 0) },
      { "Icegolem", (Color.FromArgb(94, 91, 95), 0) },
      { "Apprentice", (Color.FromArgb(137, 70, 77), 0) },
      { "Rootrider", (Color.FromArgb(188, 99, 78), 0) },
      { "Hogrider", (Color.FromArgb(74, 67, 56), 0) },
      { "Golem", (Color.FromArgb(101, 93, 85), 0) },
      { "Bowler", (Color.FromArgb(131, 123, 249), 0) },
      { "Headhunter", (Color.FromArgb(226, 181, 59), 0) },
      { "Druid", (Color.FromArgb(164, 84, 67), 0) },

      { "SuperArcher", (Color.FromArgb(230, 146, 134), 0) }
    };


    private Dictionary<string, (Color Color, int Count)> spellColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "Lightning", (Color.FromArgb(96, 242, 255), 0) },
      { "Rage", (Color.FromArgb(172, 66, 218), 0) },
      { "Freeze", (Color.FromArgb(204, 255, 255), 0) },
      { "Invis", (Color.FromArgb(201, 214, 207), 0) },
      { "Poison", (Color.FromArgb(252, 226, 215), 0) },
      { "Haste", (Color.FromArgb(255, 235, 255), 0) },
      { "Bats", (Color.FromArgb(240, 207, 237), 0) },
      { "Heal", (Color.FromArgb(255, 241, 138), 0) },
      { "Jump", (Color.FromArgb(209, 255, 50), 0) },
      { "Clone", (Color.FromArgb(46, 249, 235), 0) },
      { "Recall", (Color.FromArgb(255, 200, 223), 0) },
      { "Earthquake", (Color.FromArgb(67, 57, 47), 0) },
      { "Skeleton", (Color.FromArgb(231, 170, 144), 0) },
      { "Overgrowth", (Color.FromArgb(228, 249, 96), 0) }
    };


    private Dictionary<string, (Color Color, int Count)> siegeColors = new Dictionary<string, (Color Color, int Count)>
    {
      { "WallWrecker", (Color.FromArgb(145, 49, 43), 0) },
      { "Blimp", (Color.FromArgb(124, 68, 46), 0) },
      { "StoneSlammer", (Color.FromArgb(140, 120, 89), 0) },
      { "SiegeBarracks", (Color.FromArgb(154, 48, 50), 0) },
      { "LogLauncher", (Color.FromArgb(129, 45, 37), 0) },
      { "FlameFlinger", (Color.FromArgb(113, 67, 44), 0) }
    };

    private int TroopsToRecruit = 0;

    public MainWindow()
    {
      InitializeComponent();

      //Thread.Sleep(5000);
      //GetPictures();
      Dispatcher.Invoke(() => CounterTextBox.Text = $"Troops Donated:\n{sessionDonations}");
      Dispatcher.Invoke(() => LogTextBox.AppendText("Application starting...\n"));
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
          Dispatcher.Invoke(() => LogTextBox.AppendText("Restarting the task...\n"));
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
          lastTime = DateTime.Now;
          commandTime = lastTime.AddSeconds(15);
          if (TroopsToRecruit > 0)
          {
            NavigateToTrainTroops();
            TrainTroops();
            NavigateFromTroopsToTrainSpells();
            TrainSpells();
            NavigateFromSpellsToSiege();
            TrainSiege();
            NavigateToChat();
            TroopsToRecruit = 0;
          }
        }

        // Check if 15 seconds have passed since last reconnect
        if (DateTime.Now >= commandTime)
        {
          NavigateToChat();
          commandTime = DateTime.MaxValue; // Prevent further execution until next reconnect
        }
      }
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
            TroopsToRecruit++;

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
            TroopsToRecruit++;

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
        case var _ when IsColorMatch(color, 166,  59, 50): IncrementSiegeCounter("StoneSlammer"); Console.WriteLine("StoneSlasmmer"); return true;//
        case var _ when IsColorMatch(color, 154, 47, 46): IncrementSiegeCounter("SiegeBarracks"); Console.WriteLine("SiegeBarracks"); return true;//


        default:
          Dispatcher.Invoke(() => LogTextBox.AppendText("Unknown color encountered: " + color + "\n"));
          return false; ;
      }
    }

    private bool IsColorMatch(Color color, byte r, byte g, byte b, int tolerance = 6)
    {
      return Math.Abs(color.R - r) <= tolerance && Math.Abs(color.G - g) <= tolerance && Math.Abs(color.B - b) <= tolerance;
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
      var keys = troopColors.Keys.ToList();
      DateTime lastTime = DateTime.MinValue;

      for (int j = 0; j < keys.Count; j++)
      {
        bool scrolled = false;
        string key = keys[j];
        int count = troopColors[key].Count;

        if (count != 0)
        {
          Point? position = SearchRows(650, troopColors[key].Color);

          if (position == null)
          {
            position = SearchRows(650 + 171, troopColors[key].Color);
          }

          if (position == null)
          {
            ScrollToRight();
            scrolled = true;
            position = SearchRows(650, troopColors[key].Color);
          }

          if (position == null)
          {
            position = SearchRows(650 + 171, troopColors[key].Color);
          }

          if (position == null)
          {
            ScrollToLeft();
            Dispatcher.Invoke(() => LogTextBox.AppendText("Troop " + key + " not found when trying to recruit\n"));
            if (!key.Equals("SuperArcher") && (DateTime.Now - lastTime).TotalMinutes >= 10)
            {
              lastTime = DateTime.Now;
              CloseClashOfClans();
              StartClashOfClans();
              Thread.Sleep(120000);
              return;
            }
            continue;
          }

          for (int i = 0; i < count; i++)
          {
            ClickPosition(position.Value.X, position.Value.Y);
          }

          troopColors[key] = (troopColors[key].Color, 0);
          if (scrolled) ScrollToLeft();
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
          Point? position = SearchRows(650, spellColors[key].Color);

          if (position == null)
          {
            position = SearchRows(650 + 171, spellColors[key].Color);
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

          spellColors[key] = (spellColors[key].Color, 0);
        }
      }
    }

    void TrainSiege()
    {
      var keys = siegeColors.Keys.ToList();

      for (int j = 0; j < keys.Count; j++)
      {
        bool scrolled = false;
        string key = keys[j];
        int count = siegeColors[key].Count;

        if (count != 0)
        {
          Point? position = SearchRows(650, siegeColors[key].Color);

          if (position == null)
          {
            ScrollToRight();
            scrolled = true;
            position = SearchRows(650, siegeColors[key].Color);
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

          siegeColors[key] = (siegeColors[key].Color, 0);
          if (scrolled) ScrollToLeft();
        }
      }
    }

    Point? SearchRows(int y, Color searchedColor)
    {
      Rectangle captureArea = new Rectangle(0, y, 1920, 1);
      Bitmap image = ScreenCapture.CaptureScreen(captureArea);

      // Loop through each pixel and check for matching RGB values
      for (int x = 0; x < image.Width; x++)
      {
        Color pixelColor = image.GetPixel(x, 0);
        
        if (searchedColor.Equals(pixelColor))
        {
        image.Dispose();
          return new Point(x, y);
        }
      }
      image.Dispose();
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
      SetCursorPosition(x, y);
      mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
      mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

      Random random = new Random();
      int randomSleepTime = random.Next(300, 700);
      Thread.Sleep(randomSleepTime);
    }

    public static void Drag(int startX, int startY, int endX, int endY)
    {
      SetCursorPosition(startX, startY);
      mouse_event(MOUSEEVENTF_LEFTDOWN, startX, startY, 0, 0);
      Thread.Sleep(100);
      SetCursorPosition(endX, endY);
      mouse_event(MOUSEEVENTF_LEFTUP, endX, endY, 0, 0);
      Random random = new Random();
      int randomSleepTime = random.Next(1500, 2500);
      Thread.Sleep(randomSleepTime);
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
          Dispatcher.Invoke(() => LogTextBox.AppendText($"Closing Clash of Clans (PID: {targetProcess.Id})...\n"));
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
      int topLeftX = 380;
      int topLeftY = 650;

      // Troops
      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in troopColors.Keys)
        {
          if (troopColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY + 171, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Bottom Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in troopColors.Keys)
        {
          if (troopColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      ScrollToRight();

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in troopColors.Keys)
        {
          if (troopColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY + 171, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Bottom Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in troopColors.Keys)
        {
          if (troopColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      NavigateFromTroopsToTrainSpells();

      // Spells
      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in spellColors.Keys)
        {
          if (spellColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      for (int i = 0; i < 8; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 166 * i, topLeftY + 171, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Bottom Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in spellColors.Keys)
        {
          if (spellColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      NavigateFromSpellsToSiege();

      // Síege
      for (int i = 0; i < 5; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 295 * i, topLeftY, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in siegeColors.Keys)
        {
          if (siegeColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }

      ScrollToRight();

      for (int i = 0; i < 5; i++)
      {
        Rectangle captureArea = new Rectangle(topLeftX + 200 + 295 * i, topLeftY, 1, 1);
        Bitmap image = ScreenCapture.CaptureScreen(captureArea);

        Color pixelColor = image.GetPixel(0, 0);
        Dispatcher.Invoke(() => LogTextBox.AppendText($"Top Row {i}: RGB = {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\n"));
        foreach (string key in siegeColors.Keys)
        {
          if (siegeColors[key].Color.Equals(pixelColor))
          {
            Dispatcher.Invoke(() => LogTextBox.AppendText(key + "\n"));
          }
        }
      }
    }
  }
}