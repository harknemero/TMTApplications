using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyence_Laser
{
    public class KeyenceCTRL
    {

        public KeyenceCTRL()
        {

        }

        public double takeSample()
        {
            return randomTestData();
        }

        public double randomTestData()
        {
            Random rand = new Random();
            double value = (rand.NextDouble()*6)+9;

            return value;
        }
    }
}
