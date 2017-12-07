﻿using System;
using System.Collections.Generic;
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
        private System.Timers.Timer timer;
        private List<DataPoint> data;
        private List<DataPoint> sampleData;

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
            alarmLevel = 0;
            Manual = false;
            Automatic = false;
            manualSpeedChanged = true;
            commandSpeedChanged = true;
            targetTempChanged = true;
            currentSpeed = 0;
            currentTemp = -50;
            startingTemp = -50;
            data = new List<DataPoint>();
            sampleData = new List<DataPoint>();
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

            return result;
        }

        // Stop Mixer
        public bool stopDrive()
        {
            bool result = false;
            result = drive.stop();
            ManualSpeedChanged = true;

            return result;
        }

        // Gets current temperature from Thermometer Controller
        public float getTemp()
        {
            float temp = therm.getTemp();

            return temp;
        }

        /*
         * Creates a new datapoint from manual speed, temperature, and time 
        */
        public void pollData()
        {
            DataPoint dp;            
            System.DateTime time = new System.DateTime();
            time.ToLocalTime();
            float slope = 0;
            float derivative = 0;
            DataPoint last;
            if (sampleData.Count > 0)
            {
                last = sampleData[sampleData.Count - 1];
                slope = (float)((double)(currentTemp - last.Temp) / (time.Subtract(last.Time).TotalSeconds)); 
                
                if(sampleData.Count > 1)
                {                    
                    derivative = (float)((double)(slope - last.Slope) / (currentTemp - last.Temp));
                }
            }

            dp = new DataPoint(time, commandSpeed, currentTemp, slope, derivative);

            if (commandSpeedChanged)
            {
                sampleData = new List<DataPoint>();
                commandSpeedChanged = false;
            }
            sampleData.Add(dp);
        }

        // Sets the speed of the VF Drive based on whether Auto or Manual is activated
        // If Auto is activated, then it sets the speed based on the predicted equilibrium speed as
        // as well as on the difference between the actual and target temperatures.
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

                if(currentTemp < Properties.Settings.Default.TargetTemperature - 5)
                {
                    commandSpeed = Properties.Settings.Default.MaxSpeed;                    
                }
                else
                {
                    // Every degree difference between target and actual results in a 10% offset.
                    // The offset overcorrects for differences in order to get on target faster.
                    float offset = (1 + ((Properties.Settings.Default.TargetTemperature - currentTemp) / 10));

                    // If the actual temp is higher than the target temp, then this offset is multiplied
                    // by 1 + the difference in degrees.
                    // A difference of 1 degree = -20%     2 = -60%     3 = -75%
                    if(currentTemp > Properties.Settings.Default.TargetTemperature)
                    {
                        offset = 1 - (1 - offset) * (2 + currentTemp - Properties.Settings.Default.TargetTemperature);
                    }

                    if (PredictedEqSpeed * offset >= Properties.Settings.Default.MaxSpeed)
                    {
                        commandSpeed = Properties.Settings.Default.MaxSpeed;
                    }
                    else if (PredictedEqSpeed * offset <= Properties.Settings.Default.MinimumAutoSpeed)
                    {
                        commandSpeed = Properties.Settings.Default.MinimumAutoSpeed;
                    }
                    else
                    {
                        commandSpeed = PredictedEqSpeed * offset;
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
        public void calculateEqSpeed()
        {
            List<DataPoint> recentSamples = new List<DataPoint>();
            double avgDeriv = 0; // m
            double originalSlope = 0; // b
            double slopeNeeded = 0;
            double speedMultiplier = 1;
            float eqSpeed = -1;

            if(PredictedEqSpeed != -1)
            {
                eqSpeed = PredictedEqSpeed;
            }            

            if(sampleData.Count >= 3)
            {
                // Take and average up to the last 15 sample derivatives.
                for(int count = sampleData.Count - 1; count > 2 && sampleData.Count - count < 15; count--)
                {
                    recentSamples.Add(sampleData[count]);
                }

                // Get average derivative and average original slope value.
                for(int count = 0; count < recentSamples.Count; count++)
                {
                    avgDeriv += recentSamples[count].Derivative;
                    originalSlope += -1 * (avgDeriv * (recentSamples[count].Temp - startingTemp)) + recentSamples[count].Slope;
                }
                avgDeriv /= recentSamples.Count;
                originalSlope /= recentSamples.Count;

                slopeNeeded = -1 * (avgDeriv * Properties.Settings.Default.TargetTemperature - startingTemp);
                speedMultiplier = slopeNeeded / originalSlope;

                PredictedEqSpeed = (float) (commandSpeed * speedMultiplier);
            }


            PredictedEqSpeed = eqSpeed;
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
    }

    class DataPoint
    {
        private System.DateTime time;
        private float speed;
        private float temp;
        private float slope;
        private float derivative;

        public DataPoint(System.DateTime ti, float sp, float te, float sl, float de)
        {
            Time = ti;
            Speed = sp;
            Temp = te;
            Slope = sl;
            Derivative = de;
            
        }

        public DateTime Time { get => time; set => time = value; }
        public float Speed { get => speed; set => speed = value; }
        public float Temp { get => temp; set => temp = value; }
        public float Slope { get => slope; set => slope = value; }
        public float Derivative { get => derivative; set => derivative = value; }
    }
}
