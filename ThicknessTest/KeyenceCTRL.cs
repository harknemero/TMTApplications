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
        string kData;
        string badCommandResponse; // Right now this is used to verify correct device. If necessary, change value in constructor.

        public KeyenceCTRL()
        {
            laser = new SerialPort();
            laser.BaudRate = 9600;
            laser.DataBits = 8;
            laser.Parity = Parity.None;
            laser.StopBits = StopBits.One;
            laser.Handshake = Handshake.None;
            laser.DtrEnable = true;
            laser.RtsEnable = true;
            laser.NewLine = "\r\n";

            kData = "";
            badCommandResponse = "ER,BA,00\r"; // Right now this is used to verify correct device.


        }

        public bool openKeyence()
        {
            string[] portNames = SerialPort.GetPortNames();

            Console.WriteLine("There are " + portNames.Length + " port names to check through.");
            foreach(string name in portNames)
            {
                laser.PortName = name;
                // laser.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                try
                {
                    if (!laser.IsOpen)
                    {
                        laser.Open();
                        laser.DiscardInBuffer();                        
                        laser.WriteLine("Bad Command\r\n");
                        kData = getResponse();
                        if (kData == badCommandResponse)
                        {
                            return true;
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

        public double takeSample()
        {
            double thickness = 0;
            string[] responseItems;
            laser.DiscardInBuffer();
            laser.Write("M0\r\n");
            kData = getResponse();
            responseItems = kData.Split(',');
            if(responseItems != null)
            {
                if(responseItems[0] != "ER")
                {
                    thickness = Convert.ToDouble(responseItems[1]);
                }
            }            

            return thickness;
        }

        // Temporary function used to test system before KeyenceCTRL was implimented.
        public double randomTestData()
        {
            Random rand = new Random();
            double value = (rand.NextDouble()*6)+9;

            return value;
        }

        private string getResponse()
        {
            string response = laser.ReadExisting();
            int tics = 0;
            while(response == "" && tics < 20)
            {
                System.Threading.Thread.Sleep(100);
                response = laser.ReadExisting();
            }

            return response;
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received: ");
            Console.Write(indata);
        }
    }
}
