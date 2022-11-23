using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HighShearMixController
{
    public class Controller
    {
        private ThermometerControl therm;
        private VFDriveControl drive;
        private AlarmControl alarm;
        private int alarmLevel;
        private float currentSpeed;
        private float commandSpeed;
        private float currentTemp;
        private float startingTemp;
        private bool manual;
        private bool automatic;
        private bool thermConn;
        private bool driveConn;
        private bool targetTempChanged;     // Flag for speed setting and datapolling
        private bool manualSpeedChanged;    // Flag for speed setting.
        private bool commandSpeedChanged;   // Flag for data polling purposes.
        private System.Diagnostics.Stopwatch timer;
        private List<DataPoint> sampleData;
        private string fileName;
        public string debugMessage = "";    // For testing/debugging only.

        private static int maxAlarmLevel = 2;
        private static System.TimeSpan sampleInterval = new System.TimeSpan(0, 0, 30); // 30 second intervals

        public bool Manual { get => manual; set => manual = value; }
        public bool Automatic { get => automatic; set => automatic = value; }
        public bool AlarmConn { get => alarm.isConnected(); }
        public bool ThermConn { get => thermConn; set => thermConn = value; }
        public bool DriveConn { get => driveConn; set => driveConn = value; }
        public static int MaxAlarmLevel { get => maxAlarmLevel;}
        public float CurrentTemp { get => currentTemp; set => currentTemp = value; }
        public bool ManualSpeedChanged { get => manualSpeedChanged; set => manualSpeedChanged = value; }
        public bool TargetTempChanged { get => targetTempChanged; set => targetTempChanged = value; }
        public float CurrentSpeed { get => currentSpeed; set => currentSpeed = value; }
        public float CommandSpeed { get => commandSpeed; set => commandSpeed = value; }

        public Controller()
        {
            therm = new ThermometerControl();
            drive = new VFDriveControl();
            alarm = new AlarmControl();
            sampleData = new List<DataPoint>();
            timer = new System.Diagnostics.Stopwatch();
            alarmLevel = 0;
            Manual = false;
            Automatic = false;
            manualSpeedChanged = true;
            commandSpeedChanged = true;
            targetTempChanged = true;
            currentSpeed = 0;
            currentTemp = -50;
            startingTemp = -50;
            fileName = "";
            
            //thermConn = therm.isConnected();
            //driveConn = drive.isConnected();
        }

        // Start Mixer
        public bool startDrive()
        {
            bool result = false;            
            result = drive.start();

            if(startingTemp == -50)
            {
                startingTemp = currentTemp;
            }

            if (result)
            {
                manualSpeedChanged = true;
                timer.Start();
                setSpeed();                
            }

            return result;
        }

        // Stop Mixer
        public bool stopDrive()
        {
            bool result = false;
            result = drive.stop();            

            if (result)
            {
                ManualSpeedChanged = true;
            }            

            return result;
        }

        // Gets current temperature from Thermometer Controller
        public float getTemp()
        {
            float temp = therm.getTemp();
            temp = (temp + currentTemp) / 2; // All temp samples will be an average of two samples.
            currentTemp = temp;

            return temp;
        }

        // Gets connection status if any controllers are disconnected
        public string getMessage()
        {
            string message = "";
            if (!therm.isConnected()) message += "Thermometer ";
            if (!drive.isConnected()) message += "VF Drive ";
            if (!alarm.isConnected()) message += "Alarm ";
            if (message != "") message += "is disconnected";

            return message;
        }

        /*
         * Creates a new datapoint from manual speed, temperature, and time 
        */
        public void pollData()
        {
            DataPoint dp;
            float slope = 0;
            float derivative = 0;
            DataPoint last;
            if (sampleData.Count > 0)
            {
                last = sampleData[sampleData.Count - 1];
                slope = (float)((double)(currentTemp - last.Temp) / (timer.Elapsed.Subtract(last.Time).TotalSeconds));

                /*debugMessage = "Controller Debug: ElapsedTime = " + timer.Elapsed.Subtract(last.Time).TotalSeconds +
                    "    Slope = " + slope; //*/// ***** Debugging *****

                if (sampleData.Count > 1)
                {
                    float tempChange = currentTemp - last.Temp;
                    if(tempChange == 0)
                    {
                        tempChange = (float) .0001;
                    }
                    derivative = (float)((double)(slope - last.Slope) / (tempChange));
                }
            }

            dp = new DataPoint(timer.Elapsed, commandSpeed, currentTemp, slope, derivative, getMessage());

            if (commandSpeedChanged)
            {
                //sampleData = new List<DataPoint>();
                commandSpeedChanged = false;
            }
            sampleData.Add(dp);
            //debugMessage = "Controller Debug: SampleData.Count = " + sampleData.Count(); *****Debugging*****
            //debugMessage = "";
        }

        /*
         * When within a set number of degrees from the target temperature, speed begins
         * to adjust with the aim of reaching 0 rate of change when on target temperature. 
        */
        public bool setSpeed()
        {
            float oldCommandSpeed = commandSpeed;
            bool result = false;
            if (manual || automatic)
            {
                if (!automatic && ManualSpeedChanged)
                {
                    commandSpeed = Properties.Settings.Default.ManualSpeed;
                    ManualSpeedChanged = false;
                }
                else if (automatic)
                {
                    if (sampleData.Count < 3)
                    {
                        if (currentTemp < Properties.Settings.Default.TargetTemperature - Properties.Settings.Default.ControlRange)
                        {
                            commandSpeed = Properties.Settings.Default.MaxSpeed;
                        }
                        if (currentTemp > Properties.Settings.Default.TargetTemperature + Properties.Settings.Default.ControlRange)
                        {
                            commandSpeed = Properties.Settings.Default.MinimumAutoSpeed;
                        }
                    }
                    else
                    {
                        if (currentTemp < Properties.Settings.Default.TargetTemperature - Properties.Settings.Default.ControlRange)
                        {
                            commandSpeed = Properties.Settings.Default.MaxSpeed;
                        }
                        else
                        {
                            int size = sampleData.Count - 1;
                            int samples = size;
                            float targetTemp = Properties.Settings.Default.TargetTemperature;
                            float tDiff = 0;
                            float slope = 0;
                            if (samples >= 10)
                            {
                                samples = 10;
                            }
                            for (int count = 1; count < samples; count++)
                            {
                                slope += sampleData[size - count].Slope;
                            }
                            slope /= 10;

                            if(currentTemp < targetTemp)
                            {
                                tDiff = targetTemp - currentTemp + (float) .25;
                            }
                            else
                            {
                                tDiff = targetTemp - currentTemp - (float) .25;
                            }
                            float slopeWanted = (tDiff * (float)0.007) / Properties.Settings.Default.ControlRange;

                            if (slope < slopeWanted && CommandSpeed < Properties.Settings.Default.MaxSpeed)
                            {
                                commandSpeed++;
                            }
                            else if (slope > slopeWanted && CommandSpeed > Properties.Settings.Default.MinimumAutoSpeed + 2)
                            {
                                commandSpeed--;
                                commandSpeed--;
                            }
                            debugMessage = "slope = " + slope +
                            "\nSlope Wanted = " + slopeWanted +
                            "\nTDiff = " + tDiff +
                            "\nCommandSpeed = " + CommandSpeed; // / // ***** Debugging *****
                        }

                    }
                }
            }
            else
            {
                commandSpeed = 0;
            }
            if (oldCommandSpeed != commandSpeed)
            {
                result = drive.setSpeed(commandSpeed);
                commandSpeedChanged = true;
            }
            return result;
        }

        public void getCurrentSpeed()
        {
            if (!drive.isConnected()) return;
            currentSpeed = drive.getSpeed();
        }

        public void setAlarmLevel(int level)
        {
            alarmLevel = level;
            if(level < 0)
            {
                alarmLevel = 0;
            }
            else if (level > MaxAlarmLevel)
            {
                alarmLevel = MaxAlarmLevel;
            }
            
        }

        public int getAlarmLevel()
        {
            return alarmLevel;
        }

        public string getDriveWarning()
        {
            return drive.Warning;
        }

        public bool checkAlarmConn()
        {
            if (!alarm.isConnected())
            {
              alarm.openAlarm();
            }
            bool result = alarm.isConnected();

            return result;
        }

        public bool checkThermConn()
        {
            bool result = therm.isConnected();
            //result = true; // ********************************************* for testing
            ThermConn = result;

            return result; 
        }

        public bool checkDriveConn()
        {
            if(drive == null)
            {
                return drive.openDrive();
            }
            bool result = drive.isConnected();
            //result = true; // ********************************************* for testing
            DriveConn = result;

            return result; 
        }

        public void restoreDrive()
        {
            drive.restore();
        }

        public void alarmStandBy()
        {
            alarm.standBy();
        }

        public void alarmArm()
        {
            alarm.arm();
        }

        public void alarmActivate()
        {
            alarm.activate();
        }

        private string sessionToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(System.DateTime.Now.ToLongDateString() + "," + System.DateTime.Now.ToLongTimeString());
            sb.AppendLine();

            for (int row = 2; row < sampleData.Count()-1; row++)
            {
                TimeSpan ts = sampleData[row].Time;
                string time = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
                sb.Append(time + "," + sampleData[row].Speed + "," + sampleData[row].Temp + "," + sampleData[row].Message);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void setFileName(string name)
        {
            fileName = name;
        }

        public void saveSession()
        {
            if (sampleData.Count() > 3)
            {
                DateTime date = new DateTime();
                date = DateTime.Now;
                TimeSpan ts = timer.Elapsed;
                //string fileName = "Mix1_" + date.Month + "," + date.Day + "," + date.Year + "_" +
                //    String.Format("{0:00},{1:00},{2:00}", ts.Hours, ts.Minutes, ts.Seconds) + ".csv";
                try
                {
                    StreamWriter writer = new StreamWriter(fileName);
                    writer.WriteLine(sessionToString());
                    writer.Close();
                    debugMessage = "Saved.";
                }
                catch
                {
                    debugMessage = fileName + " not saved.";
                }
            }
            else
            {
                debugMessage = "No data to save!";
            }
        }

        /*
         * Starts a new data collecting session.
        */
        public void newSession()
        {
            sampleData = new List<DataPoint>();
            timer.Reset();
            debugMessage = "Data Cleared. New Session.";
        }
    }

    class DataPoint
    {
        private System.TimeSpan time;
        private float speed;
        private float temp;
        private float slope;
        private float derivative;
        private string message;

        public DataPoint(System.TimeSpan ti, float sp, float te, float sl, float de, string msg)
        {
            Time = ti;
            Speed = sp;
            Temp = te;
            Slope = sl;
            Derivative = de;
            message = msg;
        }

        public TimeSpan Time { get => time; set => time = value; }
        public float Speed { get => speed; set => speed = value; }
        public float Temp { get => temp; set => temp = value; }
        public float Slope { get => slope; set => slope = value; }
        public float Derivative { get => derivative; set => derivative = value; }
        public string Message { get => message; set => message = value; }
    }
}
