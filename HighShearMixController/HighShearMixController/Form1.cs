using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        private bool pollNow;
        private bool recordingSession;

        public Form1(Controller c)
        {
            controller = c;
            temperatureAlarm = 0;
            lockdown = false; // *********** initialize this to true after development ********************
            continuePolling = true;
            pollNow = true;
            recordingSession = false;
            InitializeComponent();

            textBox1.Text = Properties.Settings.Default.ManualSpeed + "";
            textBox2.Text = Properties.Settings.Default.TargetTemperature + "";
            label12.Text = Properties.Settings.Default.MinimumAutoSpeed + "Hz - 60.0Hz";

            updateStatus();

            runPoller(); // ******** renders debugger unusable - comment out to use debugger.
            //button5.Enabled = true; button5.Visible = true; // ************************************ for testing
        }


        // Update all labels in groupBox3
        private void updateStatus()
        {
            if (controller.ThermConn)
            {
                label8.Text = "Thermometer Connected";
                label8.ForeColor = Color.DarkGreen;

                float temp = controller.getTemp();
                labelTemp.Text = temp + " C";

                if (temp - Properties.Settings.Default.WarningRange > double.Parse(textBox2.Text))
                {
                    temperatureAlarm = 2;
                    labelTemp.ForeColor = Color.Red;
                    label5.ForeColor = Color.Red;
                }
                else if (temp > double.Parse(textBox2.Text))
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
                label8.Text = "Thermometer Disconnected";
                label8.ForeColor = Color.Red;
            }
            if (controller.DriveConn)
            {
                label9.Text = "VFD Connected";
                label9.ForeColor = Color.DarkGreen;
                labelSpeed.Text = "" + controller.CurrentSpeed;
                labelSpeed.ForeColor = Color.DarkGreen;

            }
            else
            {
                label9.Text = "VFD Disconnected";
                label9.ForeColor = Color.Red;
                labelSpeed.ForeColor = Color.Red;
            }

            if (controller.getDriveWarning() != "")
            {
                label11.Text = controller.getDriveWarning();
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
            if (!controller.ThermConn || !controller.DriveConn)
            {
                lockdown = true;
            }
            else
            {
                lockdown = false;
            }


            if (controller.Manual)
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
                button3.Enabled = true;
                button4.Enabled = true;
                button7.Enabled = false;

            }
            else if (controller.Automatic)
            {
                button1.Enabled = true;
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
                button7.Enabled = false;
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
                button7.Enabled = true;
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

            if (overallAlarm > Controller.MaxAlarmLevel)
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
            controller.Manual = true;
            if (controller.startDrive())
            {
                controller.Automatic = false;
                groupBox1.BackColor = Color.LightGray;
                groupBox2.BackColor = Color.Transparent;
                updateLockdown();
                recordingSession = true;
                pollNow = true;
            }
            else
            {
                controller.Manual = false;
            }
        }

        // Manual stop button
        private void button2_Click(object sender, EventArgs e)
        {
            if (controller.stopDrive())
            {
                controller.Manual = false;
                controller.Automatic = false;
                groupBox1.BackColor = Color.Transparent;
                updateLockdown();
            }
        }

        // Automatic start button
        private void button3_Click(object sender, EventArgs e)
        {
            controller.Automatic = true;
            if (controller.startDrive())
            {
                controller.Manual = false;
                groupBox2.BackColor = Color.LightGray;
                groupBox1.BackColor = Color.Transparent;
                updateLockdown();
                recordingSession = true;
                pollNow = true;
            }
            else
            {
                controller.Automatic = false;
            }
        }

        // Automatic stop button
        private void button4_Click(object sender, EventArgs e)
        {
            if (controller.stopDrive())
            {
                controller.Automatic = false;
                controller.Manual = false;
                groupBox2.BackColor = Color.Transparent;
                updateLockdown();
            }
        }

        // Save Session
        private void button6_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.AddExtension = false;
            saveFileDialog1.InitialDirectory = Properties.Settings.Default.DefaultSaveLoc;
            //saveFileDialog1.DefaultExt = ".csv";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

                saveFileDialog1.FileName += ".csv";
                Properties.Settings.Default.DefaultSaveLoc = Path.GetDirectoryName(saveFileDialog1.FileName);
                controller.saveSession(saveFileDialog1.FileName);
            }
        }

        // New Session
        private void button7_Click(object sender, EventArgs e)
        {
            controller.newSession();
            recordingSession = false;
        }

        // Manual Speed - User input
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text != "")
                {
                    if (Convert.ToDouble(textBox1.Text) >= Properties.Settings.Default.MinimumAutoSpeed
                        && Convert.ToDouble(textBox1.Text) < Properties.Settings.Default.MaxSpeed)
                    {
                        label12.ForeColor = Color.Black;
                        if (Properties.Settings.Default.ManualSpeed != Convert.ToDouble(textBox1.Text))
                        {
                            controller.ManualSpeedChanged = true;
                            pollNow = true;
                        }
                        Properties.Settings.Default.ManualSpeed = (float)Convert.ToDouble(textBox1.Text);
                    }
                    else
                    {
                        label12.ForeColor = Color.Red;
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

        // Target temperature - User input
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text != "")
                {
                    if (Convert.ToDouble(textBox2.Text) <= Properties.Settings.Default.MaxTargetTemp)
                    {
                        if (Properties.Settings.Default.TargetTemperature != Convert.ToDouble(textBox2.Text))
                        {
                            controller.TargetTempChanged = true;
                            pollNow = true;
                        }
                        Properties.Settings.Default.TargetTemperature = (float)Convert.ToDouble(textBox2.Text);
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
            bool thermConn = false;
            while (continuePolling)
            {
                pollCounter++;

                // If pollNow flag is set, unset flag. Otherwise, sleep.
                if (pollNow)
                {
                    pollCounter = 1;
                    pollNow = false;
                }
                else
                {
                    System.Threading.Thread.Sleep(Properties.Settings.Default.PollingInterval);
                }

                if (driveConn != controller.DriveConn || thermConn != controller.ThermConn)
                {
                    updateStatus();
                }

                if (pollCounter % Properties.Settings.Default.RecordInterval == 1)
                {
                    controller.checkDriveConn();
                    controller.checkThermConn();
                    controller.setSpeed();
                }

                if (recordingSession && pollCounter % Properties.Settings.Default.RecordInterval == 0)
                {
                    controller.pollData();
                }

                label14.Text = controller.debugMessage; //****** Debugging *****
                controller.getCurrentSpeed();
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


        /*
         * Poller for Debugging only.
         */
        private void button5_Click(object sender, EventArgs e)
        {
            controller.checkDriveConn();
            controller.checkThermConn();
            controller.setSpeed();

            if (!(Convert.ToDouble(textBox1.Text) >= Properties.Settings.Default.MinimumAutoSpeed
                && Convert.ToDouble(textBox1.Text) < Properties.Settings.Default.MaxSpeed))
            {
                textBox1.Text = ("" + Properties.Settings.Default.ManualSpeed);
                label12.ForeColor = Color.Red;
            }
            else
            {
                label12.ForeColor = Color.Black;
            }

            controller.pollData();
            updateStatus();
        }
    }       
}
