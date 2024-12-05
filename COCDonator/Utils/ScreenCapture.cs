using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace COCDonator.Utils
{

  public class ScreenCapture
  {
    public static Bitmap CaptureScreen(Rectangle bounds)
    {
      Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
      using (Graphics g = Graphics.FromImage(bitmap))
      {
        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
      }
      return bitmap;
    }
  }
}