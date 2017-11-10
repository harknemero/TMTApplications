using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighShearMixController
{
    class Controller
    {
        private ThermometerControl therm;
        private VFDriveControl drive;
        private int alarmLevel;
        private double predictedEqSpeed;
        private double targetTemp;
        private double currentTemp;
        private double manualSpeed;
        private bool manual;
        private bool automatic;
        private bool thermConn;
        private bool driveConn;

        private static int maxAlarmLevel = 2;

        public bool Manual { get => manual; set => manual = value; }
        public bool Automatic { get => automatic; set => automatic = value; }
        public bool ThermConn { get => thermConn; set => thermConn = value; }
        public bool DriveConn { get => driveConn; set => driveConn = value; }
        public static int MaxAlarmLevel { get => maxAlarmLevel;}
        public double CurrentTemp { get => currentTemp; set => currentTemp = value; }

        public Controller()
        {
            therm = new ThermometerControl();
            drive = new VFDriveControl();
            alarmLevel = 0;
            targetTemp = 0;
            manualSpeed = 0;
            Manual = false;
            Automatic = false;
            ThermConn = checkThermConn();
            DriveConn = checkDriveConn();
        }

        // Start Mixer
        public bool startDrive()
        {
            bool result = false;
            setSpeed();
            result = drive.start();

            return result;
        }

        // Stop Mixer
        public bool stopDrive()
        {
            bool result = false;
            result = drive.stop();

            return result;
        }


        // Gets current temperature from Thermometer Controller
        public double getTemp()
        {
            double temp = therm.getTemp();

            return temp;
        }

        // Sets the speed of the VF Drive based on whether Auto or Manual is activated
        // If Auto is activated, then it sets the speed based on the predicted equilibrium speed as
        // as well as on the difference between the actual and target temperatures.
        public bool setSpeed()
        {
            bool result = false;
            if (manual)
            {
                result = drive.setSpeed(manualSpeed);
            }
            else if (automatic)
            {

                if(currentTemp < targetTemp - 2)
                {
                    result = drive.setSpeed(Properties.Settings.Default.MaxSpeed);
                }
                else
                {
                    // Every degree difference between target and actual results in a 5% offset.
                    // The offset overcorrects for differences in order to get actual on target faster.
                    double offset = (1 + ((targetTemp - currentTemp) / 20));

                    // If the actual temp is higher than the target temp, then this offset is multiplied
                    // by 2 + the difference in degrees.
                    // A difference of 1 degree = -15%     2 = -40%     3 = -75%
                    if(currentTemp > targetTemp)
                    {
                        offset = 1 - (1 - offset) * (2 + currentTemp - targetTemp);
                    }

                    if (predictedEqSpeed * offset >= Properties.Settings.Default.MaxSpeed)
                    {
                        result = drive.setSpeed(Properties.Settings.Default.MaxSpeed);
                    }
                    else if(predictedEqSpeed * offset <= Properties.Settings.Default.MinimumAutoSpeed)
                    {
                        result = drive.setSpeed(Properties.Settings.Default.MinimumAutoSpeed);
                    }
                    else
                    {
                        result = drive.setSpeed(predictedEqSpeed * offset);
                    }
                }
            }

            return result;
        }

        public double getPredictedEqSpeed()
        {
            return predictedEqSpeed;
        }

        public void calculateEqSpeed()
        {
            double eqSpeed = 0;

            predictedEqSpeed = eqSpeed;
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

        public bool checkThermConn()
        {
            bool result = therm.isConnected();
            ThermConn = true;

            return true; //********************************************** for testing
        }

        public bool checkDriveConn()
        {
            bool result = drive.isConnected();
            DriveConn = true;

            return true; // ********************************************* for testing
        }
    }
}
