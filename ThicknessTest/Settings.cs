using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thickness_Test_Settings
{
    public class Settings
    {
        private double intervalLengthMM;
        private int numOfIntervals;
        private int numOfRows;
        private int zaberOrigin;
        private int dirFromOrigin;
        private double targetThickness;
        private double acceptableRange;
        private double errorRange;

        // This constructor loads some default settings.
        // Should only run in the event that no settings files are found.
        public Settings()
        {
            intervalLengthMM = 76.2;
            numOfIntervals = 9;
            numOfRows = 12;
            zaberOrigin = 330974;
            dirFromOrigin = -1;
            targetThickness = 12.25;
            acceptableRange = 0.5;
            errorRange = 2;
        }

        public void refreshSettings(string fileName)
        {

        }

        public double IntervalLengthMM { get => intervalLengthMM; set => intervalLengthMM = value; }
        public int NumOfIntervals { get => numOfIntervals; set => numOfIntervals = value; }
        public int NumOfRows { get => numOfRows; set => numOfRows = value; }
        public int ZaberOrigin { get => zaberOrigin; set => zaberOrigin = value; }
        public int DirFromOrigin { get => dirFromOrigin; set => dirFromOrigin = value; }
        public double TargetThickness { get => targetThickness; set => targetThickness = value; }
        public double AcceptableRange { get => acceptableRange; set => acceptableRange = value; }
        public double ErrorRange { get => errorRange; set => errorRange = value; }
    }

    
}
