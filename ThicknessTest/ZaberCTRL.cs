using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;

// vbCr = "\r"
// vbLf = "\n"
// vbCrLf = "\r\n"


namespace Zaber_Track_System
{
    public class ZaberCTRL
    {
        SerialPort motor;
        bool portBusy = false;
        int currentPos = 0;
        int MaxSpeed = 200000;
        int MaxAccel = 40;
        string mData = "";
        string wStr = "";
        const int stepsPerMM = 237;
               
        public ZaberCTRL()
        {
            motor = new SerialPort();
            motor.BaudRate = 115200;
            motor.DataBits = 8;
            motor.Parity = Parity.None;
            motor.StopBits = StopBits.One;
            motor.DiscardNull = false;
            motor.DtrEnable = false;
            motor.Handshake = Handshake.None;
            motor.NewLine = "\r\n";
            motor.ReadTimeout = 1000;    //Timeout after 1 second
        }
        public bool openZaber()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                motor.PortName = port;
                try
                {
                    motor.Open();
                    motor.DiscardInBuffer();
                    motor.Write("/get deviceid\r\n");
                    mData = motor.ReadLine();
                    mData = mData.Substring(mData.Length - 5, 5);
                    if (mData == "50186")
                    {
                        mData = sendCMD("/set maxspeed " + MaxSpeed);
                        mData = sendCMD("/set accel " + MaxAccel);
                        return (true);
                    }
                    motor.Close();
                }
                catch (Exception ex)
                {
                    wStr = ex.Message;
                    motor.Close();
                }
            }
            return (false);
        }
        public bool goHome()
        {
            mData = sendCMD("/home");
            return (mData.Contains("OK"));
        }
        public bool Parking
        {
            get
            {
                mData = sendCMD("/tools parking state");
                return Convert.ToBoolean(getValue(mData));
            }
            set
            {
                switch (value)
                {
                    case true:
                        mData = sendCMD("/tools parking park");
                        break;
                    case false:
                        mData = sendCMD("/tools parking unpark");
                        break;
                }
            }
        }

        public int getPos()
        {
            try
            {
                mData = sendCMD("/get pos");
                currentPos = Convert.ToInt32(mData);
            }
            catch
            {

            }            
            return currentPos;
        }

        // Moves the zaber to a given position on the track. Track positions range from 0 (home) to about 354,371.
        public bool moveABS(int steps)
        {
            mData = sendCMD("/move abs " + steps);
            return (mData.Contains("OK"));
        }

        // Moves the zaber a given distance.
        // direction should have a value of either 1 (away from home) or -1 (toward home).
        public bool moveRel(double millimeters, int direction)
        {
            mData = sendCMD("/move abs " + (direction * Convert.ToInt32(millimeters * stepsPerMM)));
            return (mData.Contains("OK"));
        }
        public int getMaxSpeed()
        {
            mData = sendCMD("/get maxspeed");
            return Convert.ToInt32(getValue(mData));
        }
        public void clearWarnings()
        {
            mData = sendCMD("/warnings clear");
        }
        public bool isMotorBusy()
        {
            try
            {
                mData = sendCMD("/get pos");
            }
            catch
            {

            }
            return (mData.Contains("BUSY"));
        }
        private string getValue(string value)
        {
            char[] chrSpace = " ".ToCharArray();
            return (value.Substring(value.LastIndexOfAny(chrSpace)).Trim());
        }
        private string sendCMD(string cmd)
        {
            string ReturnValue = null;
            if (!isPortBusy())
            {
                portBusy = true;
                try
                {
                    motor.DiscardInBuffer();
                    motor.Write(cmd + "\r\n");
                    motor.ReadTimeout = 1000;
                    ReturnValue = motor.ReadLine();
                }
                catch
                {
                    return (null);
                }
                portBusy = false;
            }
            else
            {
                ReturnValue = "-1";
            }
            return (ReturnValue);
        }
        private bool isPortBusy()
        {
            Stopwatch msw = new Stopwatch();        //Define a stopwatch
            long timeout = 1000;        //Time out in 1 second
            msw.Start();                //Start stopwatch   
            while (msw.ElapsedMilliseconds < timeout)          //Check if time out
            {
                if (!portBusy) return false;     //if port is not busy return a false
            }
            msw.Stop();
            return (true);  //port is lock and timed out
            
        }
        public void Close()
        {
            motor.Close();
        }

    }
}
