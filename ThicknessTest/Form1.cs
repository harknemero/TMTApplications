using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Thickness_Data;
using Thickness_Test_Settings;
using Zaber_Track_System;
using Keyence_Laser;

namespace ThicknessTest
{
    public partial class Form1 : Form
    {
        private ZaberCTRL zaber;
        private KeyenceCTRL keyence;
        private Settings settings;
        private ThicknessData data;
        int currentRow;
        double lastSample;

        public Form1(ZaberCTRL z, KeyenceCTRL k, Settings s, ThicknessData d)
        {
            zaber = z;
            keyence = k;
            settings = s;
            data = d;
            currentRow = 0;
            lastSample = 0;

            InitializeComponent();
            
        }

        private void runRowThicknessTestRoutine()
        {
            try
            {
                if (zaber.getPos() != settings.ZaberOrigin)
                {
                    zaber.finishMove(); // Just in case operation begins from an unexpected zaber state.
                    zaber.moveABS(settings.ZaberOrigin);
                    zaber.finishMove();
                }
                for (int count = 0; count < settings.NumOfIntervals; count++)
                {
                    int counter = 0; // for troubleshooting. delete later
                    zaber.moveRel(settings.IntervalLengthMM, settings.DirFromOrigin);
                    zaber.finishMove();
                    lastSample = keyence.takeSample();
                    data.recordData(currentRow, count, lastSample);
                    dataTextUpdate(count);
                    System.Console.WriteLine("Slept " + counter + " times.");// for troubleshooting. delete later
                }
                zaber.moveABS(settings.ZaberOrigin);
                zaber.finishMove();
                if(currentRow + 1 < settings.NumOfRows)
                {
                    currentRow++;
                    textBox1.Text = ("" + (currentRow + 1));
                }
            }
            catch
            {

            }
        }

        private void dataTextUpdate(int currentInterval)
        {
            richTextBox1.Clear();
            for(int row = settings.NumOfIntervals-1; row >= 0; row--)
            {
                for(int column = 0; column < settings.NumOfRows; column++)
                {
                    double value = data.getValueAt(column, row);
                    string valueString = string.Format("{0:00.00}", data.getValueAt(column, row));
                    if(value < settings.TargetThickness - settings.ErrorRange || value > settings.TargetThickness + settings.ErrorRange)
                    {
                        richTextBox1.AppendText("  " + valueString + "  ", Color.Gray);
                    }
                    else if(value < settings.TargetThickness - settings.AcceptableRange)
                    {
                        richTextBox1.AppendText("  " + valueString + "  ", Color.Red);
                    }
                    else if (row == currentInterval && column == currentRow)
                    {// Indicates the most recent sample taken, unless that sample is flagged a different color.
                        richTextBox1.AppendText("  " + valueString + "  ", Color.Green);
                    }
                    else
                    {
                        richTextBox1.AppendText("  " + valueString + "  ");
                    }
                }
                richTextBox1.AppendText("\n\n");
            }
            richTextBox1.Update();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void TestRowButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("TestRowButton Pushed.");
            runRowThicknessTestRoutine();
        }  
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {    
            if (Convert.ToInt32(textBox1.Text) < 1 || Convert.ToInt32(textBox1.Text) > settings.NumOfRows)
            {
                currentRow = 0;
                textBox1.Text = (1 + "");
            }
            else
            {
                currentRow = Convert.ToInt32(textBox1.Text) - 1;
            }
            textBox1.Update();
        }

        // Prev button. Subtracts 1 from text box value and currentRow variable.
        private void button1_Click(object sender, EventArgs e)
        {
            if(currentRow > 0)
            {
                currentRow--;
                textBox1.Text = ("" + (currentRow + 1));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (currentRow + 1 < settings.NumOfRows)
            {
                currentRow++;
                textBox1.Text = ("" + (currentRow + 1));
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }        
    }
}
