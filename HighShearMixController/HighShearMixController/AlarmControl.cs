using System;
using System.Text;
using System.IO.Ports;

namespace HighShearMixController
{
    class AlarmControl
    {
        private SerialPort alarm;
        private bool portBusy;
        private bool connected = false;
        private string rsDataBuffer;

        private const char CONNECT = 'W';
        private const char STAND_BY = 'X';
        private const char ARM = 'Y';
        private const char ACTIVATE = 'Z';

        public AlarmControl()
        {
            portBusy = false;
            rsDataBuffer = string.Empty;
        }

        /*
         * Open a connection to the drive through its comm port.
        */
        public bool openAlarm()
        {
            closeAlarm();

            bool result = false;
            portBusy = false;
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                alarm = new SerialPort();
                alarm.BaudRate = 115200;
                alarm.DtrEnable = true;
                alarm.WriteTimeout = 50;
                alarm.ReadTimeout = 50;
                alarm.PortName = port;
                try
                {
                    if (!alarm.IsOpen)
                    {
                        alarm.Open();
                        result = connect();
                        //alarm.Write(STAND_BY.ToString());
                        //rsDataBuffer = alarm.ReadExisting();
                        if (result) break;
                    }
                }
                catch (Exception ex)
                {
                    string wStr = ex.Message;
                }
                closeAlarm();
            }

            return (result);
        }

        public bool isConnected()
        {
            return connected;
        }

        // Sends command to Alarm.
        private bool sendCommand(string command)
        {
            if (alarm == null)
            {
                connected = false;
                return false;
            }

            int timeOut = 0;
            while (portBusy)
            {
                timeOut++;
                System.Threading.Thread.Sleep(1);
                if (timeOut > 1000)
                {
                    return false;
                }
            }
            portBusy = true;
            System.Threading.Thread.Sleep(10); // wait for data

            try
            {
                alarm.DiscardInBuffer();
                alarm.DiscardOutBuffer();
                alarm.Write(command);
                System.Threading.Thread.Sleep(50); // too cautious?
                rsDataBuffer = getResponse();
            }
            catch
            {
                connected = false;
                portBusy = false;
                return false;
            }

            portBusy = false;
            return true;
        }

        /*
         *  Gets Response from Alarm
        */
        private string getResponse()
        {
            string response = string.Empty;

            try
            {
                response = alarm.ReadExisting();
                connected = response != string.Empty;
                return response;
            }
            catch
            {
                throw new System.ArgumentException("Failed on GetResponse.");
            }
        }

        /*
         * Response Checker checks response for correct CONNECTION_CODE
        */
        private bool checkResponse(string command, string response)
        {
            bool result = true;
            StringBuilder sb = new StringBuilder();
            sb.Append("");

            if(response == null || response.Length < 1 || response != command.ToLower()) // the alarm converts the uppercase command to a lowercase response 
            {
                result = false;
            }

            if (!result)
            {
                sb.Append("Alarm command failed.\nCommand: ");
                sb.Append(command[0]);
                sb.Append("Expected: " + command);
                //throw new System.ArgumentException(sb.ToString()); //***** For debugging only *****
            }
            string warning = sb.ToString();
            if (warning != "") Console.WriteLine(warning);

            return result;
        }

        private void closeAlarm()
        {
            if (alarm == null) return;
            alarm.Close();
            System.Threading.Thread.Sleep(100);
            alarm.Dispose();
            System.Threading.Thread.Sleep(150);
            alarm = null;
        }

        public bool connect()
        {
            sendCommand(CONNECT.ToString());
            return checkResponse(CONNECT.ToString(), rsDataBuffer);
        }

        public bool standBy()
        {
            sendCommand(STAND_BY.ToString());
            return checkResponse(STAND_BY.ToString(), rsDataBuffer);
        }

        public void arm()
        {
            sendCommand(ARM.ToString());
            checkResponse(ARM.ToString(), rsDataBuffer);
        }

        public void activate()
        {
            sendCommand(ACTIVATE.ToString());
            checkResponse(ACTIVATE.ToString(), rsDataBuffer);
        }
    }
}
