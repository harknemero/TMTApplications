using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighShearMixController
{
    public class Controller
    {
        private ThermometerControl therm;
        private VFDriveControl drive;
        private int alarmLevel;
        private float predictedEqSpeed;
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
        private List<DataPoint> data;
        private List<DataPoint> sampleData;
        public string debugMessage = "";    // For testing/debugging only.

        private static int maxAlarmLevel = 2;
        private static System.TimeSpan sampleInterval = new System.TimeSpan(0, 0, 30); // 30 second intervals

        public bool Manual { get => manual; set => manual = value; }
        public bool Automatic { get => automatic; set => automatic = value; }
        public bool ThermConn { get => thermConn; set => thermConn = value; }
        public bool DriveConn { get => driveConn; set => driveConn = value; }
        public static int MaxAlarmLevel { get => maxAlarmLevel;}
        public float CurrentTemp { get => currentTemp; set => currentTemp = value; }
        public bool ManualSpeedChanged { get => manualSpeedChanged; set => manualSpeedChanged = value; }
        public bool TargetTempChanged { get => targetTempChanged; set => targetTempChanged = value; }
        public float PredictedEqSpeed { get => predictedEqSpeed; set => predictedEqSpeed = value; }
        public float CurrentSpeed { get => currentSpeed; set => currentSpeed = value; }
        public float CommandSpeed { get => commandSpeed; set => commandSpeed = value; }

        public Controller()
        {
            therm = new ThermometerControl();
            drive = new VFDriveControl();
            data = new List<DataPoint>();
            sampleData = new List<DataPoint>();
            timer = new System.Diagnostics.Stopwatch();
            alarmLevel = 0;
            Manual = false;
            Automatic = false;
            manualSpeedChanged = true;
            commandSpeedChanged = true;
            targetTempChanged = true;
            predictedEqSpeed = -1;
            currentSpeed = 0;
            currentTemp = -50;
            startingTemp = -50;
            
            //thermConn = therm.isConnected();
            //driveConn = drive.isConnected();
        }

        // Start Mixer
        public bool startDrive()
        {
            bool result = false;
            setSpeed();
            result = drive.start();
            if(startingTemp == -50)
            {
                startingTemp = currentTemp;
            }

            if (result)
            {
                timer.Start();
            }

            return result;
        }

        // Stop Mixer
        public bool stopDrive()
        {
            bool result = false;
            result = drive.stop();
            ManualSpeedChanged = true;

            if (result)
            {
                timer.Stop();
            }

            if (result)
            {
                saveSession();
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

            dp = new DataPoint(timer.Elapsed, commandSpeed, currentTemp, slope, derivative);

            if (commandSpeedChanged)
            {
                //sampleData = new List<DataPoint>();
                commandSpeedChanged = false;
            }
            sampleData.Add(dp);
            //debugMessage = "Controller Debug: SampleData.Count = " + sampleData.Count(); *****Debugging*****
        }

        /*
         * 
         * 
        */
        public bool setSpeed()
        {
            float oldCommandSpeed = commandSpeed;
            bool result = false;
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
                    if(currentTemp > Properties.Settings.Default.TargetTemperature + Properties.Settings.Default.ControlRange)
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
                        float tDiff = Properties.Settings.Default.TargetTemperature - currentTemp;
                        float slope = 0;
                        if(samples >= 10)
                        {
                            samples = 10;
                        }                        
                        for(int count = 1; count < samples; count++)
                        {
                            slope += sampleData[size - count].Slope;
                        }
                        slope /= 10;
                        float slopeWanted = (tDiff * (float) 0.005) / Properties.Settings.Default.ControlRange;
                        
                        if(slope < slopeWanted && CommandSpeed < Properties.Settings.Default.MaxSpeed)
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
            if (oldCommandSpeed != commandSpeed)
            {
                result = drive.setSpeed(commandSpeed);
                commandSpeedChanged = true;
            }
            return result;
        }

        public void getCurrentSpeed()
        {
            currentSpeed = drive.getSpeed();
        }

        public double getPredictedEqSpeed()
        {
            return PredictedEqSpeed;
        }

        /*
         * Predicts equilibrium speed using sample data
         * 
         * Algorithm:
        */
        /*public void calculateEqSpeed()
        {
            List<DataPoint> recentSamples = new List<DataPoint>();
            double avgDeriv = 0; // m
            double originalSlope = 0; // b
            double slopeNeeded = 0;
            double speedMultiplier = 1;
            float eqSpeed = -1;
            
            if (PredictedEqSpeed != -1)
            {
                eqSpeed = PredictedEqSpeed;
            }            

            if(sampleData.Count >= 3)
            {
                // Take the last 60 samples.
                for(int count = sampleData.Count - 1; count >= 2 && sampleData.Count - count < 60; count--)
                {
                    recentSamples.Add(sampleData[count]);
                }
                
                // Get average derivative and average original slope value.
                for (int count = 0; count < recentSamples.Count; count++)
                {
                    avgDeriv += recentSamples[count].Derivative;
                    originalSlope += -1 * (avgDeriv * (recentSamples[count].Temp - startingTemp)) + recentSamples[count].Slope;
                }
                avgDeriv /= recentSamples.Count;
                originalSlope /= recentSamples.Count;

                slopeNeeded = -1 * (avgDeriv * Properties.Settings.Default.TargetTemperature - startingTemp);
                speedMultiplier = slopeNeeded / originalSlope;

                eqSpeed = (float)(commandSpeed * speedMultiplier);

                debugMessage = "EQ Temperature = " + (-1 * (originalSlope / avgDeriv) + startingTemp) +
                    "\nEQ Speed = " + eqSpeed +
                    "\nDerivative = " + avgDeriv +
                    "\nOriginalSlope = " + originalSlope +
                    "\nSlopeNeeded = " + slopeNeeded; // / // ***** Debugging *****
            }

            //debugMessage = "Controller Debug: eqSpeed = " + eqSpeed; // ***** Debugging *****
            eqSpeed = 45; // *************** for testing purposes **********************
            PredictedEqSpeed = eqSpeed;
        }//*/

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

        private string sessionToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int row = 2; row < sampleData.Count()-1; row++)
            {
                TimeSpan ts = sampleData[row].Time;
                string time = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
                sb.Append(time + "," + sampleData[row].Speed + "," + sampleData[row].Temp + "\n");
            }

            return sb.ToString();
        }

        public void saveSession()
        {
            DateTime date = new DateTime();
            date = DateTime.Now;
            TimeSpan ts = timer.Elapsed;
            string fileName = "Mix1_" + date.ToLongDateString() + "_" +
                String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            StreamWriter writer = new StreamWriter(fileName);
            writer.WriteLine(sessionToString());

            writer.Close();
        }
    }

    class DataPoint
    {
        private System.TimeSpan time;
        private float speed;
        private float temp;
        private float slope;
        private float derivative;

        public DataPoint(System.TimeSpan ti, float sp, float te, float sl, float de)
        {
            Time = ti;
            Speed = sp;
            Temp = te;
            Slope = sl;
            Derivative = de;
            
        }

        public TimeSpan Time { get => time; set => time = value; }
        public float Speed { get => speed; set => speed = value; }
        public float Temp { get => temp; set => temp = value; }
        public float Slope { get => slope; set => slope = value; }
        public float Derivative { get => derivative; set => derivative = value; }
    }
}
