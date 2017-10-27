using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;

namespace HighShearMixController
{
    class VFDriveControl
    {
        private SerialPort drive;
        private bool portBusy;
        private string rsData;

        private static string idResponse = ""; // ******* This needs to be determined *******
        private static byte networkAddress = 0x01;
        private static byte writeRegister = 0x06;
        private static byte password = 0x00; // For locking and unlocking register/parameter access

        public VFDriveControl()
        {
            portBusy = false;
            rsData = "";
            drive = new SerialPort();
            drive.BaudRate = 115200;
            drive.DataBits = 8;
            drive.Parity = Parity.None;
            drive.StopBits = StopBits.One;
            drive.DiscardNull = false;
            drive.DtrEnable = false;
            drive.Handshake = Handshake.None;
            drive.NewLine = "\r\n";
            drive.ReadTimeout = 1000;    //Timeout after 1 second
        }
        public bool openDrive()
        {
            portBusy = false;
            drive = new SerialPort();
            drive.BaudRate = 115200;
            drive.DataBits = 8;
            drive.Parity = Parity.None;
            drive.StopBits = StopBits.One;
            drive.DiscardNull = false;
            drive.DtrEnable = false;
            drive.Handshake = Handshake.None;
            drive.NewLine = "\r\n";
            drive.ReadTimeout = 1000;    //Timeout after 1 second
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                drive.PortName = port;
                try
                {
                    if (!drive.IsOpen)
                    {
                        drive.Open();
                        drive.DiscardInBuffer();
                        drive.Write("/get deviceid\r\n");
                        rsData = drive.ReadLine();
                        rsData = rsData.Substring(rsData.Length - 5, 5);
                        if (rsData == idResponse)
                        {
                            // any initialization?
                            return (true);
                        }
                        drive.Close();
                    }
                }
                catch (Exception ex)
                {
                    string wStr = ex.Message;
                    drive.Close();
                }
            }
            return (false);
        }

        // Sends pre-built command to VFDrive.
        private bool sendCommand(List<byte> bytes)
        {
            bool result = false;

            return result;
        }

        /*  Start Mixer.
         *  Register # 1
         *  Set bit[3]
        */
        public bool start()
        {
            bool result = false;
            List<byte> bytes = new List<byte>();

            //W SA 06 RH RL DH DL CRCH CRCL
            //0x_01_06_00_01_00_08_00_00
            bytes.Add(networkAddress); // Network address
            bytes.Add(writeRegister); // Op code for single reg write
            bytes.Add(0x00); // Register high byte
            bytes.Add(0x01); // Register low byte for Register #1;
            bytes.Add(0x01); // Data high byte
            bytes.Add(0x08); // Data low byte - set bit[3] to 1.
            bytes.Add(0x00); // ********** required value not yet known **************
            bytes.Add(0x00); // ********** required value not yet known **************

            return result;
        }

        /*  Stop Mixer.
         *  Register # 1
         *  Set bit[2]
        */
        public bool stop()
        {
            bool result = false;
            //0x_01_06_00_01_00_04_00_00

            return result;
        }

        /*  Start Mixer.
         *     
        */
        public bool setSpeed(double speed)
        {
            bool result = false;

            return result;
        }

        /*  Checks connection to VFDrive.
         *     
        */
        public bool isConnected()
        {
            bool result = false;

            return result;
        }

        // This may not be necessary and is effectively only a stub for now.
        private void finishTask()
        {
            int maxWaits = 100;
            int sleepLength = 50; //milliseconds
            int count = 0;

            try
            {
                while (false)
                {
                    if (count >= maxWaits)
                    {
                    }
                    System.Threading.Thread.Sleep(sleepLength);
                    count++;
                }
            }
            catch
            {
                throw new System.ArgumentException("No response on drive.finishTask()");
            }
        }

        /*  Sets the designated bit to 1. All other bits will be zero.
         *  
        */
        private byte setSingleBit(int bitNum)
        {
            if(bitNum < 0 || bitNum > 15)
            {
                throw new System.ArgumentException("Out of range bit assignment.");
            }
            bitNum = bitNum % 8;

            byte b = (byte)(0xFF & (1 << bitNum));
            return b;
        }

        /*  
         *  Unlock the Drive Control register (#1) by writing a 0 (or the drive password) to Register #48.
         *  Lock by writing 0x0002 to Register # 1
         */
        private void lockDrive(bool locking)
        {
            if (locking)
            {

            }
            else
            {

            }
        }

        private List<byte> calculateCRC(List<byte> bytes)
        {
            short crc;
            

            return bytes;
        }
    }
}
