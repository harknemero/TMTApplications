using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thickness_Data
{
    class ThicknessData
    {
        private RowData[] Data;

        public ThicknessData(int numOfRows)
        {
            Data = new RowData[numOfRows];
        }

        public void recordData(int row, int intervalNum, double value)
        {
            Data[row].recordData(intervalNum, value);
        }
    }

    class RowData
    {
        private double[] row;

        public RowData(int numOfIntervals)
        {
            row = new double[numOfIntervals];
        }

        public void recordData(int intervalNum, double value)
        {
            row[intervalNum] = value;
        }
    }
}
