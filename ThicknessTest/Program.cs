using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Zaber_Track_System;
using Thickness_Test_Settings;
using Thickness_Data;
using Keyence_Laser;

namespace ThicknessTest
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Settings settings = new Settings();
            ThicknessData data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
            KeyenceCTRL keyence = new KeyenceCTRL();
            ZaberCTRL zaber = new ZaberCTRL();
            zaber.openZaber();
            zaber.goHome();
            System.Threading.Thread.Sleep(200);
            if(zaber.getPos() == 0)
            {
                zaber.moveABS(settings.ZaberOrigin);
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Form1 form1 = new Form1(zaber, keyence, settings, data);
                Application.Run(form1);
            }
            catch
            {
                zaber.goHome();
                zaber.finishMove();
                zaber.Close();
                return;
            }

            zaber.goHome();
            zaber.finishMove();
            zaber.Close();
        }
    }
}
