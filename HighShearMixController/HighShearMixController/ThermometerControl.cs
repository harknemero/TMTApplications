using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MccDaq;

namespace HighShearMixController
{
    class ThermometerControl
    {
        private MccBoard therm;
        private TempScale tempScale;
        private float lastTemp;
        private const int channel = 0;

        public ThermometerControl()
        {
            tempScale = TempScale.Celsius;
            lastTemp = -50;
            MccDaq.DaqDeviceManager.IgnoreInstaCal();            
            openTherm();
            isConnected();
        }

        private bool openTherm()
        {
            bool result = false;

            MccDaq.DaqDeviceDescriptor[] daqDevices = MccDaq.DaqDeviceManager.GetDaqDeviceInventory(DaqDeviceInterface.Usb);

            if(daqDevices.Length > 0)
            {
                foreach(MccDaq.DaqDeviceDescriptor device in daqDevices)
                {
                    if(device.ProductName == "USB-TC")
                    {
                        therm = MccDaq.DaqDeviceManager.CreateDaqDevice(0, device);
                        result = true;
                    }
                }
            }

            return result;
        }

        public float getTemp()
        {
            float temp = -50;
            try
            {
                therm.TIn(channel, tempScale, out temp, ThermocoupleOptions.Filter);
                if (temp != -50)
                {
                    lastTemp = temp;
                }
            }
            catch
            {

            }
            //System.Threading.Thread.Sleep(5); // Wait time may already be built into TIn method.

            return lastTemp;
        }

        public bool isConnected()
        {
            bool result = false;
            float temp = -50;
            try
            {
                therm.TIn(channel, tempScale, out temp, ThermocoupleOptions.Filter);
            }
            catch
            {

            }
            if(temp != -50)
            {
                result = true;
            }

            if (!result)
            {
                result = openTherm();
            }

            return result; 
        }
    }
}
