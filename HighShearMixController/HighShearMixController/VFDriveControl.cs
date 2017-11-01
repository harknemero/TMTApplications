using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections;

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
            { // Lock drive with 0x_01_06_00_01_00_02

            }
            else
            { // Unlock drive with 0x_01_06_00_30_00_00

            }
        }

        /*  Calculate CRC through the following steps.
         *
         *  bytes concatenated into bit string
         *  concatenate 16 0's onto bit string
         *  bit string reversed (significant bit on the right)
         *  Inside loop that runs
         *  shift right until 1 is in x^0 position
         *  XOR with 0xC00280 - 1100 0000 0000 0010 1000 0000 (x^16 + x^15 + x^2 + 1)
         *
         *  returns full bit array with CRC bits appended.
        */
        private void calculateCRC(List<byte> bytes)
        {
            List<byte> tempBytes = new List<byte>(bytes);
            tempBytes.Add(0x00);
            tempBytes.Add(0x00);
            BitArray bits = new BitArray(tempBytes.ToArray());

            System.Console.WriteLine(bits.Count + bits.ToString());
            
            byte[] divBytes = {0x01, 0x40, 0x03};
            BitArray divisor = new BitArray(divBytes);


            //****** Must Fix - XOR operation is not performed on the end of the bit string, but in the middle. ********
            for(int count = 0; count < bits.Count - 16; count++)
            {
                if (bits.Get(0))
                {
                    bits.Xor(divisor);
                }
                for (int i = 1; i < bits.Count; i++)
                {
                    bits[i - 1] = bits[i];
                }
                bits[bits.Count - 1] = false;
            }
            
            byte[] crc = ToByteArray(bits);

            bytes.Add(crc[0]);
            bytes.Add(crc[1]);            
            
        }

        private void reverse(BitArray array)
        {
            int length = array.Length;
            int mid = (length / 2);

            for (int i = 0; i < mid; i++)
            {
                bool bit = array[i];
                array[i] = array[length - i - 1];
                array[length - i - 1] = bit;
            }
        }

        private byte[] ToByteArray(this BitArray bits)
        {
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;

            byte[] bytes = new byte[numBytes];
            int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            return bytes;
        }
    }
}
