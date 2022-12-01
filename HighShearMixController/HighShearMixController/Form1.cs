using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;



namespace HighShearMixController
{
  public partial class Form1 : Form
    {
        private Controller controller;
        private int temperatureAlarm;
        private bool lockdown;
        private bool workOrderLock;
        private bool continuePolling;
        private bool pollNow;
        private bool recordingSession;
        private bool processRunning = false;

        public Form1(Controller c)
        {
            controller = c;
            temperatureAlarm = 0;
            lockdown = true;
            workOrderLock = true;
            continuePolling = true;
            pollNow = true;
            recordingSession = false;
            InitializeComponent();

            mixSpeedTextBox.Text = Properties.Settings.Default.ManualSpeed + "";
            tempTextBox.Text = Properties.Settings.Default.TargetTemperature + "";
            label12.Text = Properties.Settings.Default.MinimumAutoSpeed + "Hz - 60.0Hz";

            controller.checkAlarmConn();
            updateStatus();

            runPoller(); // ******** renders debugger unusable - toggle with pollButton for testing.
            //pollButton.Enabled = true; pollButton.Visible = true; // ************************************ for testing
        }


        // Update all labels in groupBox3
        private void updateStatus()
        {
            if (controller.ThermConn)
            {
                thermStatusLabel.Text = "Thermometer Connected";
                thermStatusLabel.ForeColor = Color.DarkGreen;

                float temp = controller.getTemp();
                tempLabel.Text = temp + " C";

                if (temp - Properties.Settings.Default.WarningRange > double.Parse(tempTextBox.Text))
                {
                    temperatureAlarm = 2;
                    tempLabel.ForeColor = Color.Red;
                    label5.ForeColor = Color.Red;
                }
                else if (temp > double.Parse(tempTextBox.Text))
                {
                    temperatureAlarm = 1;
                    tempLabel.ForeColor = Color.Orange;
                    label5.ForeColor = Color.Orange;
                }
                else
                {
                    temperatureAlarm = 0;
                    tempLabel.ForeColor = Color.DarkGreen;
                    label5.ForeColor = Color.DarkGreen;
                }
            }
            else
            {
                tempLabel.ForeColor = Color.Red;
                label5.ForeColor = Color.Red;
                thermStatusLabel.Text = "Thermometer Disconnected";
                thermStatusLabel.ForeColor = Color.Red;
            }

            if (controller.DriveConn)
            {
                vfdStatusLabel.Text = "VFD Connected";
                vfdStatusLabel.ForeColor = Color.DarkGreen;
                speedLabel.Text = "" + controller.CurrentSpeed;
                speedLabel.ForeColor = Color.DarkGreen;

            }
            else
            {
                vfdStatusLabel.Text = "VFD Disconnected";
                vfdStatusLabel.ForeColor = Color.Red;
                speedLabel.ForeColor = Color.Red;
            }

            if (controller.AlarmConn)
            {
                alarmStatusLabel.Text = "Alarm Connected";
                alarmStatusLabel.ForeColor = Color.DarkGreen;
            }
            else
            {
                alarmStatusLabel.Text = "Alarm Disconnected";
                alarmStatusLabel.ForeColor = Color.Red;
            }

            if (controller.getDriveWarning() != "")
            {
                label11.Text = controller.getDriveWarning();
            }
            controller.setAlarmLevel(decideOverallAlarmLevel());
            updateLockdown();
            decideOverallAlarmLevel();
            updateAlarm();
        }

        private void updateAlarm()
        {
            if (!processRunning)
            {
                controller.alarmStandBy();
            }
            else if (!controller.ThermConn || !controller.DriveConn || 
                controller.CurrentTemp > Properties.Settings.Default.TargetTemperature + Properties.Settings.Default.HazardRange)
            {
                controller.alarmActivate();
            }
            else
            {
                controller.alarmArm();
            }
        }

        // If either the drive or thermometer are not connected, disable all start buttons.
        // If drive is disconnected, disable all buttons.
        // If thermometer and drive are connected, disable/enable buttons appropriately
        private void updateLockdown()
        {
            if (!controller.ThermConn || !controller.DriveConn || workOrderLock)
            {
                lockdown = true;
            }
            else
            {
                lockdown = false;
            }


            if (controller.Manual)
            {
                manStartButton.Enabled = false;
                if (!controller.DriveConn)
                {
                    manStopButton.Enabled = false;
                }
                else
                {
                    manStopButton.Enabled = true;
                }
                autStartButton.Enabled = true;
                autStopButton.Enabled = true;

            }
            else if (controller.Automatic)
            {
                manStartButton.Enabled = true;
                manStopButton.Enabled = false;
                autStartButton.Enabled = false;
                if (!controller.DriveConn)
                {
                    autStopButton.Enabled = false;
                }
                else
                {
                    autStopButton.Enabled = true;
                }
            }
            else
            {
                manStopButton.Enabled = false;
                autStopButton.Enabled = false;
                if (lockdown)
                {
                    manStartButton.Enabled = false;
                    autStartButton.Enabled = false;
                }
                else
                {
                    manStartButton.Enabled = true;
                    autStartButton.Enabled = true;
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
                    else if (tempLabel.Text != "Unknown" && controller.Automatic)
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

            if (tempLabel.Text == "Unknown")
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
        private void manStartButton_Click(object sender, EventArgs e)
        {
            controller.Manual = true;
            if (controller.startDrive())
            {
                controller.Automatic = false;
                groupBox1.BackColor = Color.LightGray;
                groupBox2.BackColor = Color.Transparent;
                updateLockdown();
                pollNow = true;
                processRunning = true;
            }
            else
            {
                controller.Manual = false;
            }
        }

        // Manual stop button
        private void manStopButton_Click(object sender, EventArgs e)
        {
            if (controller.stopDrive())
            {
                controller.Manual = false;
                controller.Automatic = false;
                groupBox1.BackColor = Color.Transparent;
                updateLockdown();
                processRunning = false;
            }
        }

        // Automatic start button
        private void autStartButton_Click(object sender, EventArgs e)
        {
            controller.Automatic = true;
            if (controller.startDrive())
            {
                if (!recordingSession)
                {
                    label7.Text = "Started at: " + System.DateTime.Now.ToShortTimeString();
                }
                controller.Manual = false;
                groupBox2.BackColor = Color.LightGray;
                groupBox1.BackColor = Color.Transparent;
                updateLockdown();
                recordingSession = true;
                pollNow = true;
                processRunning = true;
            }
            else
            {
                controller.Automatic = false;
            }
        }

        // Automatic stop button
        private void autStopButton_Click(object sender, EventArgs e)
        {
            if (controller.stopDrive())
            {
                controller.Automatic = false;
                controller.Manual = false;
                groupBox2.BackColor = Color.Transparent;
                updateLockdown();
                processRunning = false;
            }
        }

        // Save Session
        private void setFileName()
        {            
            string fileName = "P:\\Turner MedTech\\ClearShield\\Work Order Data\\" + workOrderTextBox.Text + " batch " + batchTextBox.Text + " mix.csv";
            controller.setFileName(fileName);
        }

        // Manual Speed - User input
        private void mixSpeedTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (mixSpeedTextBox.Text != "")
                {
                    if (Convert.ToDouble(mixSpeedTextBox.Text) >= Properties.Settings.Default.MinimumAutoSpeed
                        && Convert.ToDouble(mixSpeedTextBox.Text) <= Properties.Settings.Default.MaxSpeed)
                    {
                        label12.ForeColor = Color.Black;
                        if (Properties.Settings.Default.ManualSpeed != Convert.ToDouble(mixSpeedTextBox.Text))
                        {
                            controller.ManualSpeedChanged = true;
                            pollNow = true;
                        }
                        Properties.Settings.Default.ManualSpeed = (float)Convert.ToDouble(mixSpeedTextBox.Text);
                    }
                    else
                    {
                        label12.ForeColor = Color.Red;
                    }
                }
            }
            catch
            {
                mixSpeedTextBox.Text = ("" + Properties.Settings.Default.ManualSpeed);
            }
            Update();
            moveCursorToEndOfText(mixSpeedTextBox);
            Properties.Settings.Default.Save();
        }

        // Target temperature - User input ***Unless Controlled***
        private void tempTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (tempTextBox.Text != "")
                {
                    if (Convert.ToDouble(tempTextBox.Text) <= Properties.Settings.Default.MaxTargetTemp)
                    {
                        if (Properties.Settings.Default.TargetTemperature != Convert.ToDouble(tempTextBox.Text))
                        {
                            controller.TargetTempChanged = true;
                            pollNow = true;
                        }
                        Properties.Settings.Default.TargetTemperature = (float)Convert.ToDouble(tempTextBox.Text);
                    }
                    else
                    {
                        tempTextBox.Text = ("" + Properties.Settings.Default.TargetTemperature);
                    }
                }
            }
            catch
            {
                tempTextBox.Text = ("" + Properties.Settings.Default.TargetTemperature);
            }
            Update();
            moveCursorToEndOfText(tempTextBox);
            Properties.Settings.Default.Save();
        }

        // Work Order # - User input
        private void workOrderTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (workOrderTextBox.Text != "")
                {
                    workOrderTextBox.ForeColor = Color.Black;
                    if(batchTextBox.Text != "")
                    {
                        workOrderLock = false;
                    }
                }
                else
                {
                    workOrderTextBox.ForeColor = Color.Red;
                }
            }
            catch
            {
                
            }
            Update();
            setFileName();
        }

        // Batch # - User input
        private void batchTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (batchTextBox.Text != "")
                {
                    batchTextBox.ForeColor = Color.Black;
                    if (workOrderTextBox.Text != "")
                    {
                        workOrderLock = false;
                    }
                }
                else
                {
                    batchTextBox.ForeColor = Color.Red;
                }
            }
            catch
            {

            }
            Update();
            setFileName();
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

                if (pollCounter % Properties.Settings.Default.RecordInterval == 1)
                {                    
                    controller.setSpeed();
                }
                controller.checkAlarmConn();
                controller.checkDriveConn();
                controller.checkThermConn();

                if (recordingSession && pollCounter % Properties.Settings.Default.RecordInterval == 0)
                {
                    controller.pollData();
                }

                //label14.Text = controller.debugMessage; //****** Debugging *****
                //label14.Text = "Loop counter: " + pollCounter;
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
        private void pollButton_Click(object sender, EventArgs e)
        {
            controller.checkDriveConn();
            controller.checkThermConn();
            controller.checkAlarmConn();
            controller.setSpeed();

            if (!(Convert.ToDouble(mixSpeedTextBox.Text) >= Properties.Settings.Default.MinimumAutoSpeed
                && Convert.ToDouble(mixSpeedTextBox.Text) < Properties.Settings.Default.MaxSpeed))
            {
                mixSpeedTextBox.Text = ("" + Properties.Settings.Default.ManualSpeed);
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
