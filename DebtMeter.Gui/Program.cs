using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DebtMeter.Gui
{
    internal static class Program
    {
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}


internal static class NativeMethods
{
    [DllImport("DebtMeter.Native.dll", ExactSpelling = true)]
    internal static extern int ProjectDebt(
        [In] double[] debt,
        [In] double[] rate,
        int n,
        double periodDivisor,
        [Out] double[] outDebt);
}
