using System;
using System.Windows.Forms;

namespace J2534
{
  internal static class Program
  {
    [STAThread]
    private static void Main()
    {
      ApplicationConfiguration.Initialize();
      Application.Run(new frmMain());
    }
  }
}
