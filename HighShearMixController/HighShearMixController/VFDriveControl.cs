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
        private byte[] rsData;
        private byte[] rsDataBuffer;
        private string warning;

        private static string idResponse = ""; // ******* This needs to be determined *******
        private static byte networkAddress = 0x01;
        private static byte readHoldingReg = 0x03;
        private static byte writeSingleReg = 0x06;
        private static byte[] drivePassword = {0x00, 0x00}; // For locking and unlocking register/parameter access

        public string Warning { get => warning; set => warning = value; }

        public VFDriveControl()
        {
            portBusy = false;
            rsData = new byte[] {};
            rsDataBuffer = new byte[] {};
            drive = new SerialPort();
            drive.BaudRate = 115200;
            drive.DataBits = 8;
            drive.Parity = Parity.None;
            drive.StopBits = StopBits.One;
            drive.DiscardNull = false;
            drive.DtrEnable = false; 
            drive.Handshake = Handshake.None;
            drive.NewLine = "\r\n";
            drive.ReadTimeout = 100;    //Timeout after 1 second
            openDrive(); // ********** for testing purposes ***********
        }

        /*
         * Opend a connection to the drive through its comm port.
        */
        public bool openDrive()
        {
            bool result = false;
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
            drive.ReadTimeout = 100;    //Timeout after 1 second
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                drive.PortName = port;
                try
                {
                    
                    if (!drive.IsOpen)
                    {
                        result = isConnected();
                    }
                }
                catch (Exception ex)
                {
                    string wStr = ex.Message;
                    drive.Close();
                }
            }

            if (result)
            {
                initialize();
            }

            return (result);
        }

        /*
         * Initializes some drive settings.
         * Assumes drive is properly connected.
         */
        public void initialize()
        {
            List<byte> bytes = new List<byte>();
            
            // Set to network control.
            // 0x_01_06_00_65_00_06
            bytes.Add(networkAddress); bytes.Add(writeSingleReg);
            bytes.Add(0x00); bytes.Add(0x65); // Register Address
            bytes.Add(0x00); bytes.Add(0x06); // 6 = Network control
            calculateCRC(bytes); // Add CRC bytes

            unlockParam();
            sendCommand(bytes);
            lockDriveAndParam();
        }

        /*
         * Restores drive settings for manual use.
         * Assumes drive is properly connected.
         */
        public void restore()
        {
            stop();

            List<byte> bytes = new List<byte>();
            
            // Revert to manual control.
            // 0x_01_06_00_65_00_00
            bytes.Add(networkAddress); bytes.Add(writeSingleReg);
            bytes.Add(0x00); bytes.Add(0x65); // Register Address
            bytes.Add(0x00); bytes.Add(0x00); // 0 = manual control
            calculateCRC(bytes); // Add CRC bytes

            unlockParam();
            sendCommand(bytes);
            lockDriveAndParam();
        }

        // Sends pre-built command to VFDrive.
        private bool sendCommand(List<byte> bytes)
        {
            bool result = false;

            int timeOut = 0;
            while (portBusy)
            {
                timeOut++;
                System.Threading.Thread.Sleep(1);
                if(timeOut >2000)
                {
                    return false;
                }
            }
            portBusy = true;
            System.Threading.Thread.Sleep(10); // wait for data
            int rsLength = 0;
            if(bytes[1] == 0x03)
            {
                rsLength = 15;
            }
            else
            {
                rsLength = 16;
            }
            
            try
            {
                drive.Open();
                drive.DiscardInBuffer();
                drive.DiscardOutBuffer();
                drive.Write(bytes.ToArray(), 0, bytes.Count);
                finishTask();
                rsDataBuffer = getResponse(rsLength);
            }
            catch
            {
                portBusy = false;
                try
                {
                    drive.Close();
                }
                catch{}
                return false;
            }            
            
            drive.Close();
            portBusy = false;
            return result;
        }

        /*
         *  Gets Response from VFDrive
        */
        private byte[] getResponse(int byteNum)
        {
            byte[] response = new byte[byteNum];
            
            try
            {                
                drive.Read(response, 0, byteNum);
                return response;
            }
            catch
            {
                byte[] emptyBytes = { };
                throw new System.ArgumentException("Failed on GetResponse.");
                return emptyBytes;
            }
        }

        /*
         * Response Checker
         * Compares command with response.
         * Checks command type to determine expected byte length.
        */
        private bool checkResponse(byte[] command, byte[] response)
        {
            bool result = true;
            StringBuilder sb = new StringBuilder();
            sb.Append("");
            
            switch (command[1])
            {
                // write response
                case 0x06:
                    if (command.Length != 8 || response.Length != 16)
                    {
                        result = false;
                        break;
                    }
                    if(response[10] != 0)
                    {
                        result = false;
                    }
                    break;
                
                // Read response
                case 0x03:
                    if (command.Length != 8 || response.Length != 15)
                    {
                        result = false;
                        break;
                    }
                    if (response[10] != 0)
                    {
                        result = false;
                        break;
                    }
                    break;                    

                default: result = false;
                    break;

            }

            if (!result)
            {
                sb.Append("Command Failed.\nCommand: ");
                for(int count = 0; count < command.Length; count++)
                {
                    sb.Append(command[count] + "_");
                }
                sb.Append("\nResponse: ");
                if(response[13] == 0 & response[14] == 0)
                {
                    sb.Append("Error Code: " + response[10]);
                }
                else
                {
                    for(int count = 0; count < response.Length; count++)
                    {
                        sb.Append(response[count] + "_");
                    }
                }
                //throw new System.ArgumentException(sb.ToString()); //***** For debugging only *****
            }
            Warning = sb.ToString();

            

            return result;
        }

        /*  Start Mixer.
         *  Register # 1
         *  Set bit[3]
        */
        public bool start()
        {
            bool result = true;
            List<byte> bytes = new List<byte>();

            // W SA 06 RH RL DH DL CRCH CRCL
            // 01 06 00 01 00 08 D9 CC
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00); 
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x08); // Data low byte - set bit[3] to 1.
            bytes.Add(0xD9); bytes.Add(0xCC); // CRC bytes - pre calculated

            unlockDrive();
            sendCommand(bytes);
            rsData = (byte[])rsDataBuffer.Clone(); // Capture response before it is overwritten by next command.
            lockDriveAndParam();

            result = checkResponse(bytes.ToArray(), rsData);

            if (!result)
            {
                stop(); // redundant stop

                unlockDrive();
                sendCommand(bytes);
                rsData = (byte[])rsDataBuffer.Clone(); // Capture response before it is overwritten by next command.
                lockDriveAndParam();
                result = checkResponse(bytes.ToArray(), rsData);
            }

            return result;
        }

        /*  Stop Mixer.
         *  Register # 1
         *  Set bit[2]
        */
        public bool stop()
        {
            bool result = true;
            List<byte> bytes = new List<byte>();
            // 0x_01_06_00_01_00_04_D9_C9
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x04); // set bit[2] in reg1 to stop
            bytes.Add(0xD9); bytes.Add(0xC9); // CRC bytes - pre calculated

            unlockDrive();
            sendCommand(bytes);
            rsData = (byte[]) rsDataBuffer.Clone(); // Capture response before it is overwritten by next command.
            lockDriveAndParam();

            result = checkResponse(bytes.ToArray(), rsData);

            if(!result)
            {
                unlockDrive();
                sendCommand(bytes);
                rsData = (byte[])rsDataBuffer.Clone(); // Capture response before it is overwritten by next command.
                lockDriveAndParam();
                result = checkResponse(bytes.ToArray(), rsData);
            } 

            return result;
        }

        /*  
         *  Set Mixer Speed.
         *  Register # 44
         *  Device takes an int, but converts it to double and divides by 10. 
         *  EG:     595 = 59.5
         *  So take a double from the user, multiply by 10 and convert to int.
        */
        public bool setSpeed(double s)
        {
            bool result = false;
            ushort speed = (ushort)(s * 10);
            if(speed < 0)
            {
                speed = 0;
            }
            if(speed > 600)
            {
                speed = 600;
            }
            Byte[] byteSpeed = BitConverter.GetBytes(speed);
            List<byte> bytes = new List<byte>();
            //0x_01_06_00_2C 
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); 
            bytes.Add(0x00); bytes.Add(0x2C); // Register Address
            bytes.Add(byteSpeed[1]); bytes.Add(byteSpeed[0]); // Speed value
            calculateCRC(bytes); // Add CRC bytes

            unlockParam();
            sendCommand(bytes);
            lockDriveAndParam();

            return result;
        }

        /*
         * Returns actual speed.
         */
         public float getSpeed()
        {
            float speed = 0;
            // 01 03 00 19 00 01 55 CD
            List<byte> bytes = new List<byte>();
            bytes.Add(networkAddress); bytes.Add(readHoldingReg);
            bytes.Add(0x00); bytes.Add(0x19);
            bytes.Add(0x00); bytes.Add(0x01);
            bytes.Add(0x55); bytes.Add(0xCD);
            
            sendCommand(bytes);
            rsData = (byte[]) rsDataBuffer.Clone();

            speed = ((((ushort) rsData[11]) * 256) + (ushort) rsData[12]);

            speed /= 10;

            return speed;
        }

        /*  
         *  Checks connection to VFDrive.   
        */
        public bool isConnected()
        {
            bool result = false;
            try
            {
                drive.Open();
                drive.DiscardInBuffer();
            }
            catch
            {
                Warning = "Unable to reach " + drive.PortName;
                return false;
            }

            // Set security bit on drive register.
            byte[] testByte = new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x02, 0x59, 0xCB };
            drive.Write(testByte, 0, 8);
            System.Threading.Thread.Sleep(100);
            try
            {
                rsData = getResponse(16);
            }
            catch { }

            System.Threading.Thread.Sleep(50);
            drive.Close();

            if (rsData.Length > 0)
            {
                return checkResponse(testByte, rsData);
            }
            return result;
        }

        // This may not be necessary and is effectively only a stub for now.
        private void finishTask()
        {
            int maxWaits = 3;
            int sleepLength = 10; //milliseconds
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
            // Unlock drive with 0x_01_06_00_30_(p[0])_(p[1])_89_C5
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x30); bytes.Add(drivePassword[0]); bytes.Add(drivePassword[1]);
            bytes.Add(0x89); bytes.Add(0xC5); // CRC bytes - pre calculated
            
            sendCommand(bytes);
            rsData = (byte[])rsDataBuffer.Clone();
            if (!checkResponse(bytes.ToArray(), rsData))
            {
                resetState();
                sendCommand(bytes);
                rsData = (byte[])rsDataBuffer.Clone();
                if (!checkResponse(bytes.ToArray(), rsData))
                {
                    //throw new System.ArgumentException("Unlock Drive failure.");
                }
            }
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
            
            sendCommand(bytes);
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
            
            sendCommand(bytes);
            rsData = (byte[])rsDataBuffer.Clone();
            if (!checkResponse(bytes.ToArray(), rsData))
            {
                resetState();
            }
        }

        /*
         * Reset Drive state through a simple series of steps to ensure a
         * reproducable functioning state. Only necessary when writing to drive.
        */
        private void resetState()
        {
            List<byte> bytes = new List<byte>();
            // Lock drive with 0x_01_06_00_01_00_02
            bytes.Add(networkAddress); bytes.Add(writeSingleReg); bytes.Add(0x00);
            bytes.Add(0x01); bytes.Add(0x00); bytes.Add(0x02);
            bytes.Add(0x59); bytes.Add(0xCB); // CRC bytes - pre calculated

            sendCommand(bytes);
            rsData = (byte[])rsDataBuffer.Clone();
            if (!checkResponse(bytes.ToArray(), rsData))
            {
                System.Threading.Thread.Sleep(50);
                sendCommand(bytes);
                rsData = (byte[])rsDataBuffer.Clone();
                if (!checkResponse(bytes.ToArray(), rsData))
                {
                    //throw new System.ArgumentException("Lock Drive failure.");
                }
            }
        }

        /*
         * This CRC calculation method was converted from a C function found in Appendix B of:
         *      Modbus RTU Serial Communications User Manual
         *      https://www.honeywellprocess.com/library/support/Public/Documents/51-52-25-66.pdf
         */
        private void calculateCRC(List<byte> bytes)
        {
            byte[] message = bytes.ToArray();
            byte[] CRC = new byte[2];
            byte[] CRCtemp = new byte[2];
            byte[] table = { 0x00, 0x00, 0xC0, 0xC1, 0xC1, 0x81, 0x01, 0x40, 0xC3, 0x01, 0x03, 0xC0,
                0x02, 0x80, 0xC2, 0x41, 0xC6, 0x01, 0x06, 0xC0, 0x07, 0x80, 0xC7, 0x41, 0x05, 0x00, 0xC5, 0xC1,
                0xC4, 0x81, 0x04, 0x40, 0xCC, 0x01, 0x0C, 0xC0, 0x0D, 0x80, 0xCD, 0x41, 0x0F, 0x00, 0xCF, 0xC1,
                0xCE, 0x81, 0x0E, 0x40, 0x0A, 0x00, 0xCA, 0xC1, 0xCB, 0x81, 0x0B, 0x40, 0xC9, 0x01, 0x09, 0xC0,
                0x08, 0x80, 0xC8, 0x41, 0xD8, 0x01, 0x18, 0xC0, 0x19, 0x80, 0xD9, 0x41, 0x1B, 0x00, 0xDB, 0xC1,
                0xDA, 0x81, 0x1A, 0x40, 0x1E, 0x00, 0xDE, 0xC1, 0xDF, 0x81, 0x1F, 0x40, 0xDD, 0x01, 0x1D, 0xC0,
                0x1C, 0x80, 0xDC, 0x41, 0x14, 0x00, 0xD4, 0xC1, 0xD5, 0x81, 0x15, 0x40, 0xD7, 0x01, 0x17, 0xC0,
                0x16, 0x80, 0xD6, 0x41, 0xD2, 0x01, 0x12, 0xC0, 0x13, 0x80, 0xD3, 0x41, 0x11, 0x00, 0xD1, 0xC1,
                0xD0, 0x81, 0x10, 0x40, 0xF0, 0x01, 0x30, 0xC0, 0x31, 0x80, 0xF1, 0x41, 0x33, 0x00, 0xF3, 0xC1,
                0xF2, 0x81, 0x32, 0x40, 0x36, 0x00, 0xF6, 0xC1, 0xF7, 0x81, 0x37, 0x40, 0xF5, 0x01, 0x35, 0xC0,
                0x34, 0x80, 0xF4, 0x41, 0x3C, 0x00, 0xFC, 0xC1, 0xFD, 0x81, 0x3D, 0x40, 0xFF, 0x01, 0x3F, 0xC0,
                0x3E, 0x80, 0xFE, 0x41, 0xFA, 0x01, 0x3A, 0xC0, 0x3B, 0x80, 0xFB, 0x41, 0x39, 0x00, 0xF9, 0xC1,
                0xF8, 0x81, 0x38, 0x40, 0x28, 0x00, 0xE8, 0xC1, 0xE9, 0x81, 0x29, 0x40, 0xEB, 0x01, 0x2B, 0xC0,
                0x2A, 0x80, 0xEA, 0x41, 0xEE, 0x01, 0x2E, 0xC0, 0x2F, 0x80, 0xEF, 0x41, 0x2D, 0x00, 0xED, 0xC1,
                0xEC, 0x81, 0x2C, 0x40, 0xE4, 0x01, 0x24, 0xC0, 0x25, 0x80, 0xE5, 0x41, 0x27, 0x00, 0xE7, 0xC1,
                0xE6, 0x81, 0x26, 0x40, 0x22, 0x00, 0xE2, 0xC1, 0xE3, 0x81, 0x23, 0x40, 0xE1, 0x01, 0x21, 0xC0,
                0x20, 0x80, 0xE0, 0x41, 0xA0, 0x01, 0x60, 0xC0, 0x61, 0x80, 0xA1, 0x41, 0x63, 0x00, 0xA3, 0xC1,
                0xA2, 0x81, 0x62, 0x40, 0x66, 0x00, 0xA6, 0xC1, 0xA7, 0x81, 0x67, 0x40, 0xA5, 0x01, 0x65, 0xC0,
                0x64, 0x80, 0xA4, 0x41, 0x6C, 0x00, 0xAC, 0xC1, 0xAD, 0x81, 0x6D, 0x40, 0xAF, 0x01, 0x6F, 0xC0,
                0x6E, 0x80, 0xAE, 0x41, 0xAA, 0x01, 0x6A, 0xC0, 0x6B, 0x80, 0xAB, 0x41, 0x69, 0x00, 0xA9, 0xC1,
                0xA8, 0x81, 0x68, 0x40, 0x78, 0x00, 0xB8, 0xC1, 0xB9, 0x81, 0x79, 0x40, 0xBB, 0x01, 0x7B, 0xC0,
                0x7A, 0x80, 0xBA, 0x41, 0xBE, 0x01, 0x7E, 0xC0, 0x7F, 0x80, 0xBF, 0x41, 0x7D, 0x00, 0xBD, 0xC1,
                0xBC, 0x81, 0x7C, 0x40, 0xB4, 0x01, 0x74, 0xC0, 0x75, 0x80, 0xB5, 0x41, 0x77, 0x00, 0xB7, 0xC1,
                0xB6, 0x81, 0x76, 0x40, 0x72, 0x00, 0xB2, 0xC1, 0xB3, 0x81, 0x73, 0x40, 0xB1, 0x01, 0x71, 0xC0,
                0x70, 0x80, 0xB0, 0x41, 0x50, 0x00, 0x90, 0xC1, 0x91, 0x81, 0x51, 0x40, 0x93, 0x01, 0x53, 0xC0,
                0x52, 0x80, 0x92, 0x41, 0x96, 0x01, 0x56, 0xC0, 0x57, 0x80, 0x97, 0x41, 0x55, 0x00, 0x95, 0xC1,
                0x94, 0x81, 0x54, 0x40, 0x9C, 0x01, 0x5C, 0xC0, 0x5D, 0x80, 0x9D, 0x41, 0x5F, 0x00, 0x9F, 0xC1,
                0x9E, 0x81, 0x5E, 0x40, 0x5A, 0x00, 0x9A, 0xC1, 0x9B, 0x81, 0x5B, 0x40, 0x99, 0x01, 0x59, 0xC0,
                0x58, 0x80, 0x98, 0x41, 0x88, 0x01, 0x48, 0xC0, 0x49, 0x80, 0x89, 0x41, 0x4B, 0x00, 0x8B, 0xC1,
                0x8A, 0x81, 0x4A, 0x40, 0x4E, 0x00, 0x8E, 0xC1, 0x8F, 0x81, 0x4F, 0x40, 0x8D, 0x01, 0x4D, 0xC0,
                0x4C, 0x80, 0x8C, 0x41, 0x44, 0x00, 0x84, 0xC1, 0x85, 0x81, 0x45, 0x40, 0x87, 0x01, 0x47, 0xC0,
                0x46, 0x80, 0x86, 0x41, 0x82, 0x01, 0x42, 0xC0, 0x43, 0x80, 0x83, 0x41, 0x41, 0x00, 0x81, 0xC1,
                0x80, 0x81, 0x40, 0x40, };
            CRC[0] = 0xff; CRC[1] = 0xff;
            for(int count = 0; count < message.Length; count++)
            {
                CRCtemp[0] = CRC[0];
                CRCtemp[1] = CRC[1];
                CRC[0] = table[2 * (message[count] ^ CRCtemp[1])];
                CRC[1] = (byte)(CRCtemp[0] ^ table[(2 * (message[count] ^ CRCtemp[1])) + 1]);
            }
            bytes.Add(CRC[1]);
            bytes.Add(CRC[0]);
            return;
        }//*/

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
