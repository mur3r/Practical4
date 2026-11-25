using System;
using System.Windows.Forms;

namespace Pong.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // запускаємо головну форму в циклі обробки подій вікон
            Application.Run(new Form1());
        }
    }
}
