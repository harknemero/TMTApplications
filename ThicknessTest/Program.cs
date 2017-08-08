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
            Profiles profiles = new Profiles();
            ThicknessData data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
            KeyenceCTRL keyence = new KeyenceCTRL();
            keyence.openKeyence();
            ZaberCTRL zaber = new ZaberCTRL();
            zaber.openZaber();
            zaber.goHome();

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Form1 form1 = new Form1(zaber, keyence, settings, data, profiles);
                Application.Run(form1);
            }
            catch
            {
                Console.WriteLine("Unknown Exception Occured");
                zaber.goHome();
                zaber.finishMove();
                zaber.Close();
                keyence.Close();
                return;
            }

            finally {
                zaber.goHome();
                zaber.finishMove();
                zaber.Close();
                keyence.Close();
            }
        }
    }
}
