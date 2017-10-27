using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HighShearMixController
{
    public partial class Form1 : Form
    {
        private Controller controller;
        private int temperatureAlarm;
        private bool lockdown;
        private bool continuePolling;
        private bool recordingSession;

        public Form1()
        {
            controller = new Controller();
            temperatureAlarm = 0;
            lockdown = false; // *********** initialize this to true after development ********************
            continuePolling = true;
            recordingSession = false;
            InitializeComponent();

            textBox1.Text = Properties.Settings.Default.ManualSpeed + "";
            textBox2.Text = Properties.Settings.Default.TargetTemperature + "";

            updateStatus();
        }


        // Update all labels in groupBox3
        private void updateStatus()
        {
            if (controller.ThermConn)
            {
                label8.Text = "Thermometer Connected";
                label8.ForeColor = Color.DarkGreen;

                double temp = controller.getTemp();
                labelTemp.Text = temp + " C";     
                
                if(temp - Properties.Settings.Default.WarningRange > double.Parse(textBox2.Text))
                {
                    temperatureAlarm = 2;
                    labelTemp.ForeColor = Color.Red;
                    label5.ForeColor = Color.Red;
                }
                else if(temp > double.Parse(textBox2.Text))
                {
                    temperatureAlarm = 1;
                    labelTemp.ForeColor = Color.Orange;
                    label5.ForeColor = Color.Orange;
                }
                else
                {
                    temperatureAlarm = 0;
                    labelTemp.ForeColor = Color.DarkGreen;                    
                    label5.ForeColor = Color.DarkGreen;
                }
            }
            else
            {
                labelTemp.ForeColor = Color.Red;
                label5.ForeColor = Color.Red;                
            }
            if (controller.DriveConn)
            {
                label9.Text = "VFD Connected";
                label9.ForeColor = Color.DarkGreen;
            }
            else
            {
                label9.Text = "VFD Disconnected";
                label9.ForeColor = Color.Red;
            }

            controller.setAlarmLevel(decideOverallAlarmLevel());
            updateLockdown();
            decideOverallAlarmLevel();
        }

        // If either the drive or thermometer are not connected, disable all start buttons.
        // If drive is disconnected, disable all buttons.
        // If thermometer and drive are connected, disable/enable buttons appropriately
        private void updateLockdown()
        {
            if(!controller.ThermConn || !controller.DriveConn)
            {
                lockdown = true;
            }
            else
            {
                lockdown = false;
            }

            
            if(controller.Manual)
            {
                button1.Enabled = false;
                if (!controller.DriveConn)
                {
                    button2.Enabled = false;
                }
                else
                {
                    button2.Enabled = true;
                }
                button3.Enabled = false;
                button4.Enabled = false;
                
            }
            else if (controller.Automatic)
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                if (!controller.DriveConn)
                {
                    button4.Enabled = false;
                }
                else
                {
                    button4.Enabled = true;
                }
            }
            else
            {
                button2.Enabled = false;
                button4.Enabled = false;
                if (lockdown)
                {
                    button1.Enabled = false;
                    button3.Enabled = false;
                }
                else
                {
                    button1.Enabled = true;
                    button3.Enabled = true;
                }
            }            

        }

        private int decideOverallAlarmLevel()
        {
            int overallAlarm = temperatureAlarm;
            bool drive = controller.DriveConn;
            bool therm = controller.ThermConn;

            if (!drive || !therm)
            {
                if (controller.Manual || controller.Automatic)
                {
                    if (!controller.DriveConn)
                    { // Drive is running, but not connected
                        overallAlarm += 2;
                    }
                    else if (labelTemp.Text != "Unknown" && controller.Automatic)
                    { // Therm was connected before, but has become disconnected, running on automatic
                        overallAlarm += 2;
                    }
                    else
                    { // Therm disconnected, running on manual.
                        overallAlarm++;
                    }
                }
                else
                { // Both drive and therm are not connected, but mixer is not running.
                    overallAlarm++;
                }
            }

            if(overallAlarm > Controller.MaxAlarmLevel)
            {
                overallAlarm = Controller.MaxAlarmLevel;
            }
            controller.setAlarmLevel(overallAlarm);

            if (labelTemp.Text == "Unknown")
            {
                label10.Text = "Alarm Level " + overallAlarm + "     Process not initiatied.";
            }
            else
            {
                label10.Text = "Alarm Level " + overallAlarm;
            }

            return overallAlarm;
        }

        // Manual start button
        private void button1_Click(object sender, EventArgs e)
        {
            controller.startDrive();
            controller.Manual = true;
            groupBox1.BackColor = Color.LightGray;
            updateLockdown();
        }

        // Manual stop button
        private void button2_Click(object sender, EventArgs e)
        {
            controller.stopDrive();
            controller.Manual = false;
            groupBox1.BackColor = Color.Transparent;
            updateLockdown();
        }

        // Automatic start button
        private void button3_Click(object sender, EventArgs e)
        {
            controller.startDrive();
            controller.Automatic = true;
            groupBox2.BackColor = Color.LightGray;
            updateLockdown();
            recordingSession = true;
        }

        // Automatic stop button
        private void button4_Click(object sender, EventArgs e)
        {
            controller.stopDrive();
            controller.Automatic = false;
            groupBox2.BackColor = Color.Transparent;
            updateLockdown();
            recordingSession = false;
        }

        // Manual Speed - User input
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text != "")
                {
                    if (Convert.ToDouble(textBox1.Text) >= 1 && Convert.ToDouble(textBox1.Text) < Properties.Settings.Default.MaxSpeed)
                    {
                        Properties.Settings.Default.ManualSpeed = Convert.ToDouble(textBox1.Text);
                    }
                    else
                    {
                        textBox1.Text = ("" + Properties.Settings.Default.ManualSpeed);
                    }
                }
            }
            catch
            {
                textBox1.Text = ("" + Properties.Settings.Default.ManualSpeed);
            }
            Update();
            moveCursorToEndOfText(textBox1);
            Properties.Settings.Default.Save();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text != "")
                {
                    if (Convert.ToDouble(textBox2.Text) <= Properties.Settings.Default.MaxTargetTemp)
                    {
                        Properties.Settings.Default.TargetTemperature = Convert.ToDouble(textBox2.Text);
                    }
                    else
                    {
                        textBox2.Text = ("" + Properties.Settings.Default.TargetTemperature);
                    }
                }
            }
            catch
            {
                textBox2.Text = ("" + Properties.Settings.Default.TargetTemperature);
            }
            Update();
            moveCursorToEndOfText(textBox2);
            Properties.Settings.Default.Save();
        }

        private void runPoller()
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_doWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_finished);

            backgroundWorker.RunWorkerAsync();
        }

        // Runs the test routine on a separate thread
        private void backgroundWorker_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int pollCounter = 0;
            bool driveConn = false;
            bool thermConn = true;
            while (continuePolling)
            {
                pollCounter++;
                System.Threading.Thread.Sleep(Properties.Settings.Default.PollingInterval);
                driveConn = controller.checkDriveConn();
                thermConn = controller.checkThermConn();

                if (recordingSession && pollCounter % Properties.Settings.Default.RecordInterval == 0)
                {
                    if (driveConn)
                    {

                    }
                    if (thermConn)
                    {

                    }
                }

                updateStatus();
            }
        }

        // Resets buttons when the background worker finishes
        private void backgroundWorker_finished(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        // Moves Cursor to the end of the text in any text box.
        private void moveCursorToEndOfText(TextBox tbox)
        {
            if (tbox.Text != "")
            {
                tbox.SelectionStart = tbox.Text.Length;
                tbox.SelectionLength = 0;
            }
        }
    }
}
