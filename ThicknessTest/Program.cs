using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Zaber_Track_System;
using Thickness_Test_Settings;
using Thickness_Data;

namespace ThicknessTest
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ZaberCTRL zaber = new ZaberCTRL();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void runRowThicknessTest(ZaberCTRL zaber, Settings settings, int rowNum)
        {
            try
            {
                if(zaber.getPos() != settings.ZaberOrigin)
                {
                    zaber.moveABS(settings.ZaberOrigin);
                }
                for(int count = 0; count < settings.NumOfIntervals; count++)
                {
                    zaber.moveRel(settings.IntervalLengthMM, settings.DirFromOrigin);
                    System.Threading.Thread.Sleep(500);
                    // 
                }
            }
            catch
            {

            }
        }
    }
}
