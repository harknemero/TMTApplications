using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyence_Laser
{
    public class KeyenceCTRL
    {
        SerialPort laser;
        string[] kData;
        string[] badCommandResponse; // Right now this is used to verify correct device. If necessary, change value in constructor.

        public KeyenceCTRL()
        {
            laser = new SerialPort();
            laser.BaudRate = 115200;
            laser.DataBits = 8;
            laser.Parity = Parity.None;
            laser.StopBits = StopBits.One;
            laser.Handshake = Handshake.None;
            laser.DtrEnable = true;
            laser.RtsEnable = true;
            laser.NewLine = "\r";

            kData = null;
            badCommandResponse = new string[]{ "ER","BA","00\r"}; // Right now this is used to verify correct device.


        }

        public bool openKeyence()
        {
            kData = null;
            laser = new SerialPort();
            laser.BaudRate = 115200;
            laser.DataBits = 8;
            laser.Parity = Parity.None;
            laser.StopBits = StopBits.One;
            laser.Handshake = Handshake.None;
            laser.DtrEnable = true;
            laser.RtsEnable = true;
            laser.NewLine = "\r";

            string[] portNames = SerialPort.GetPortNames();

            Console.WriteLine("There are " + portNames.Length + " port names to check through.");
            foreach(string name in portNames)
            {
                laser.PortName = name;
                try
                {
                    if (!laser.IsOpen)
                    {
                        laser.Open();
                        laser.WriteLine("Bad Command\r\n");
                        kData = testResponse();
                        if (kData[0] == badCommandResponse[0] && kData[1] == badCommandResponse[1] && kData[2] == badCommandResponse[2])
                        {
                            return true;
                        }
                        // In a strange state, keyence will return an error response. Try again.
                        else if (kData[0] == "ER") 
                        {
                            laser.WriteLine("Bad Command\r\n");
                            kData = testResponse();
                            if (kData[0] == badCommandResponse[0] && kData[1] == badCommandResponse[1] && kData[2] == badCommandResponse[2])
                            {
                                return true;
                            }
                        }
                        laser.Close();
                    }
                    else
                    {
                        Console.WriteLine("Port is already open.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    laser.Close();
                }
            }
            
            return false;
        }

        public void Close()
        {
            laser.Close();
        }

        public double averageOfXSamples(int samplesNeeded, double target, double errorRange)
        {
            double avgThickness = 0;
                        
            laser.Write("AQ\r\n"); // Initialize Storage
            kData = getResponse();
            if(kData[0] != "AQ")
            {
                throw new System.ArgumentException("No Response Received From Keyence!");
            }
            try
            {
                int count = 0;
                laser.Write("AS\r\n"); // Begin Collecting Data
                kData = getResponse();
                int tics = 0;
                // Runs during data collection until data collected meets or exceeds the samples needed.
                while (count < samplesNeeded && tics < 100)
                {
                    laser.Write("AN\r\n"); // Get Sample Count
                    kData = getResponse();
                    count = Convert.ToInt32(kData[2]);
                    tics++;
                }
                laser.Write("AP\r\n"); //Stop Collecting Data
                kData = getResponse();
                laser.Write("AN\r\n"); // Get Sample Count
                kData = getResponse();
                count = Convert.ToInt32(kData[2]);
                if (count == 0)
                {
                    throw new System.ArgumentException("No data collected.");
                }
                Console.WriteLine("Keyence has collected " + count + " samples.");
                
                laser.Write("AO,1\r\n"); // Get All Stored Samples
                kData = getResponse();
                Console.WriteLine(kData.ToString());
                List<double> goodValues = new List<double>();
                List<double> badValues = new List<double>();
                double value = 0;
                for (count = 1; count < kData.Length; count++)
                {
                    if (kData[count].Contains("-") || kData[count] == "")
                    {
                        value = 0;
                    }
                    else
                    {
                        value = Convert.ToDouble(kData[count]);
                    }
                    if (value > target - errorRange && value < target + errorRange)
                    {
                        goodValues.Add(value);
                    }
                    else
                    {
                        badValues.Add(value);
                    }
                }
                Console.Write("\nGood values: " + goodValues.Count + ",   Bad values: " + badValues.Count);
                double sum = 0;
                int entries = 0;
                if (goodValues.Count >= badValues.Count / 2)
                {
                    entries = goodValues.Count;
                    foreach (double entry in goodValues)
                    {
                        sum += entry;
                    }
                }
                else
                {
                    entries = badValues.Count;
                    foreach (double entry in badValues)
                    {
                        sum += entry;
                    }
                }
                avgThickness = sum / entries;
            }
            catch
            {
                throw new System.ArgumentException("No response on keyence.averageOfXSamples()");
            }
            Console.Write(",   Thickness: " + avgThickness + "\n");
            return avgThickness;
        }

        public double takeSample()
        {
            double thickness = 0;
            laser.DiscardInBuffer();
            laser.Write("M0\r\n");
            kData = getResponse();
            if(kData != null)
            {
                if(kData[0] != "ER")
                {
                    if (kData[1] == "-FFFFFFF")
                    {
                        thickness = 0;
                    }
                    else
                    {
                        thickness = Convert.ToDouble(kData[1]);
                    }
                }
            }            

            return thickness;
        }

        // Temporary function used to test system when Keyence hardware is unavailable.
        public double randomTestData()
        {
            Random rand = new Random();
            double value = (rand.NextDouble()*6)+9;

            return value;
        }

        // Returns array of strings representing the Keyence response.
        // Do not call this method when buffer is expected to be empty - 
        // Impossible to discern between empty buffer and lack of connection.
        private string[] getResponse()
        {
            string value = "";
            try
            {
                value = laser.ReadLine();
            }
            catch
            {
                throw new System.ArgumentException("No response on keyence.getResponse()");
            }
            string[] response = value.Split(',');
            return response;
        }

        private string[] testResponse()
        {
            string value = "";
            System.Threading.Thread.Sleep(100);
            value = laser.ReadExisting();

            string[] response = value.Split(',');
            return response;
        }

        public bool isOpen()
        {
            bool open = laser.IsOpen;
            return open;
        }
    }
}
