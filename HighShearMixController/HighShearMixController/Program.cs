﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HighShearMixController
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Controller controller = new Controller();
            Form1 form = new Form1(controller);
            Application.Run(form);

            controller.restoreDrive();
            controller.saveSession();
            Properties.Settings.Default.Save();
        }
    }
}
