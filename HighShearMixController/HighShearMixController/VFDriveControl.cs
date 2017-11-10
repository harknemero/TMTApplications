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
        //private DriveExtension driveExtend;
        private bool portBusy;
        private bool settingLock;
        private byte[] rsData;

        private static string idResponse = ""; // ******* This needs to be determined *******
        private static byte networkAddress = 0x01;
        private static byte readHoldingReg = 0x03;
        private static byte readInputReg = 0x04;
        private static byte writeSingleReg = 0x06;
        private static byte[] drivePassword = {0x00, 0x00}; // For locking and unlocking register/parameter access

        public VFDriveControl()
        {
            portBusy = false;
            rsData = new byte[] {};
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
            openDrive(); // ********** for testing purposes ***********
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
                        // Set security bit on drive register.
                        drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x02, 0x59, 0xCB }, 0, 8);
                        System.Threading.Thread.Sleep(100);
                        rsData = getResponse(8);
                        System.Threading.Thread.Sleep(100);
                        /*
                        rsData = rsData.Substring(rsData.Length - 5, 5);
                        if (rsData == idResponse)
                        {
                            // any initialization?
                            return (true);
                        }
                        */
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

            //calculateCRC(bytes);
            drive.Open();
            drive.Write(bytes.ToArray(), 0, bytes.Count);

            if (!settingLock)
            {
                rsData = getResponse((drive.ReadExisting()).Length);
                if (rsData.Length > 4)
                {
                    System.Console.WriteLine(rsData[0] + rsData[1] + rsData[2] + rsData[3] + rsData[4] + "");
                }
            }
            drive.DiscardInBuffer();

            finishTask();
            drive.Close();
            return result;
        }

        /*
         *  Gets Response from VFDrive
        */
        private byte[] getResponse(int byteNum)
        {

            try
            {
                byte[] response = new byte[byteNum];
                drive.Read(response, 0, byteNum);      
                return response;
            }
            catch
            {
                byte[] emptyBytes = { };
                return emptyBytes;
            }
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
            //0x_01_06_00_01_00_08           01 06 00 01 00 08 D9 CC
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00); 
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x08); // Data low byte - set bit[3] to 1.
            bytes.Add(0xD9); bytes.Add(0xCC); // CRC bytes - pre calculated

            unlockDrive();
            sendCommand(bytes);
            lockDriveAndParam();

            /*// ************* testing

            //  01 06 00 30 00 00 89 C5     01 06 00 01 00 08 D9 CC     01 06 00 01 00 04 D9 C9     01 06 00 01 00 02 59 CB
            drive.Open();
            System.Threading.Thread.Sleep(100);
            drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x02, 0x59, 0xCB }, 0, 8);
            System.Threading.Thread.Sleep(100);
            drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x30, 0x00, 0x00, 0x89, 0xC5 }, 0, 8);
            System.Threading.Thread.Sleep(100);
            drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x08, 0xD9, 0xCC }, 0, 8);
            System.Threading.Thread.Sleep(1000);
            drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x04, 0xD9, 0xC9 }, 0, 8);
            System.Threading.Thread.Sleep(100);
            drive.Write(new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x02, 0x59, 0xCB }, 0, 8);
            System.Threading.Thread.Sleep(100);
            drive.Close();
            // ************* /testing*/

            return result;
        }

        /*  Stop Mixer.
         *  Register # 1
         *  Set bit[2]
        */
        public bool stop()
        {
            bool result = false;
            List<byte> bytes = new List<byte>();
            //0x_01_06_00_01_00_04
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x04); // set bit[2] in reg1 to stop
            bytes.Add(0xD9); bytes.Add(0xC9); // CRC bytes - pre calculated

            unlockDrive();
            sendCommand(bytes);
            lockDriveAndParam();

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
            int maxWaits = 2;
            int sleepLength = 50; //milliseconds
            int count = 0;

            try
            {
                while (count < maxWaits)
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
        private void unlockDrive()
        {
            List<byte> bytes = new List<byte>();
            // Unlock drive with 0x_01_06_00_30_(p[0])_(p[1])       01 06 00 30 00 00 89 C5
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x30); bytes.Add(drivePassword[0]); bytes.Add(drivePassword[1]);
            bytes.Add(0x89); bytes.Add(0xC5); // CRC bytes - pre calculated

            settingLock = true;
            sendCommand(bytes);
            settingLock = false;
        }

        /*  
         *  Unlock Drive Parameters by writing the drive password to Register #48.
         *  Lock by writing 0x0002 to Register # 1
         */
        private void unlockParam()
        {
            List<byte> bytes = new List<byte>();
            // Unlock drive with 0x_01_06_00_31_(p[0])_(p[1])
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x31); bytes.Add(drivePassword[0]); bytes.Add(drivePassword[1]);
            bytes.Add(0xD8); bytes.Add(0x05); // CRC bytes - pre calculated

            settingLock = true;
            sendCommand(bytes);
            settingLock = false;
        }

        /*  
         *  Lock drive and parameters by writing 0x0002 to Register # 1
         */
        private void lockDriveAndParam()
        {
            List<byte> bytes = new List<byte>();
            // Lock drive with 0x_01_06_00_01_00_02
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x02);
            bytes.Add(0x59); bytes.Add(0xCB); // CRC bytes - pre calculated

            settingLock = true;
            sendCommand(bytes);
            settingLock = false;
        }

        /*  Calculate CRC through the following steps.
         *
         *  bytes concatenated into bit string
         *  concatenate 16 0's onto bit string
         *  bit string reversed (significant bit on the right)
         *  Inside loop that runs
         *      shift left until 1 is in x^0 position
         *      XOR with 0xC00280 - 1100 0000 0000 0010 1000 0000 (x^16 + x^15 + x^2 + 1)
         *
         *  returns full bit array with CRC bits appended.
        */
        private void calculateCRC(List<byte> bytes)
        {
            BitArray bits = new BitArray(byteToBit(bytes.ToArray()));
            
            byte[] divBytes = {0xC0, 0x02, 0x80, 0x00, 0x00, 0x00};            
            BitArray divisor = new BitArray(byteToBit(divBytes.ToArray()));

            for (int count = 0; count < bits.Count - 17; count++)
            {
                if (bits.Get(0))
                {
                    bool bit1;
                    bool bit2;
                    bool xor;
                    for(int xorCount = 0; xorCount < 17; xorCount++)
                    {
                        bit1 = bits.Get(xorCount);
                        bit2 = divisor.Get(xorCount);
                        xor = bit1 ^ bit2;
                        bits.Set(xorCount, xor);
                    }
                    byte[] t1 = ToByteArray(bits); // For testing **************
                    byte[] t2 = ToByteArray(divisor); // For testing **********************
                }
                // Shift BitArray one space left.
                for (int i = 0; i < bits.Count - 1; i++)
                {
                    bits[i] = bits[i + 1];
                }
                bits[bits.Count - 1] = false;
            }

            byte[] crc = ToByteArray(bits);

            bytes.Add(crc[0]);
            bytes.Add(crc[1]);            
            
        }

        /*
         *  Creates BitArray with bits in descending order from bytes
         *  This is to correct for the fact that BitArray(byte[]) reverses the bits in each byte.
        */
        private BitArray byteToBit(byte[] bytes)
        {
            BitArray bits = new BitArray(bytes); // Now backward... we don't want that.
            byte[] reverseBytes = ToByteArray(bits);
            bits = new BitArray(reverseBytes); // Now forward again. That's better.

            return bits;
        }

        private byte[] ToByteArray(BitArray bits)
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
