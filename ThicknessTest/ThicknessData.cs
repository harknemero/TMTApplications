using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thickness_Data
{
    public class ThicknessData
    {
        private RowData[] Data;

        public ThicknessData(int numOfRows, int numOfIntervals)
        {
            Data = new RowData[numOfRows];
            for(int count = 0; count < numOfRows; count++)
            {
                Data[count] = new RowData(numOfIntervals);
            }
        }

        public void recordData(int row, int interval, double value)
        {
            Data[row].recordData(interval, value);
        }

        public double getValueAt(int row, int interval)
        {
            return Data[row].getValueAt(interval);
        }
    }

    class RowData
    {
        private double[] row;

        public RowData(int numOfIntervals)
        {
            row = new double[numOfIntervals];
            for(int count = 0; count < numOfIntervals; count++)
            {
                row[count] = -1;
            }
        }

        public void recordData(int intervalNum, double value)
        {
            row[intervalNum] = value;
        }

        public double getValueAt(int index)
        {
            try
            {
                return row[index];
            }
            catch
            {
                Console.WriteLine("Error accessing rowData element!");
                return 0;
            }
        }
    }
}
