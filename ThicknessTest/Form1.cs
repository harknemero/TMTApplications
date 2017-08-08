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
using System.IO;

namespace ThicknessTest
{
    public partial class Form1 : Form
    {
        private ZaberCTRL zaber;
        private KeyenceCTRL keyence;
        private Settings settings;
        private ThicknessData data;
        private Profiles profiles;
        int currentRow;
        double lastSample;

        public Form1(ZaberCTRL z, KeyenceCTRL k, Settings s, ThicknessData d, Profiles p)
        {
            zaber = z;
            keyence = k;
            settings = s;
            data = d;
            profiles = p;
            currentRow = 0;
            lastSample = 0;

            InitializeComponent();

            dataTextUpdate(0);
            if(profiles.DefaultProfile != "")
            {
                settings = profiles.getProfile(profiles.DefaultProfile);
            }
            loadSettings();
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
                
                double offset = 0; // This variable is used to keep track of offset positions in the event
                // that Active Error Correction is activated.
                double retestDistance = 5; // This is how far the zaber moves from position to take a new sample.
                for (int count = 0; count < settings.NumOfIntervals; count++)
                {                    
                    lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
                    // Active Error Correction Round 1
                    if (lastSample < settings.TargetThickness - settings.ErrorRange || 
                        lastSample > settings.TargetThickness + settings.ErrorRange)
                    {
                        zaber.moveRel(retestDistance, settings.DirFromOrigin * (-1));
                        offset = retestDistance;
                        zaber.finishMove();
                        System.Threading.Thread.Sleep(100);
                        lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);

                        // Active Error Correction Round 2
                        if (lastSample < settings.TargetThickness - settings.ErrorRange ||
                        lastSample > settings.TargetThickness + settings.ErrorRange)
                        {
                            zaber.moveRel(retestDistance * 2, settings.DirFromOrigin);
                            offset = retestDistance * (-1);
                            zaber.finishMove();
                            System.Threading.Thread.Sleep(100);
                            lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
                        }
                    }

                    data.recordData(currentRow, count, lastSample);
                    dataTextUpdate(count);
                    if(count + 1 < settings.NumOfIntervals)
                    { 
                    zaber.moveRel(settings.IntervalLengthMM + offset, settings.DirFromOrigin);
                    offset = 0; // Offset should be corrected now. Reset to 0.
                    zaber.finishMove();
                    System.Threading.Thread.Sleep(200);
                    }
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

        private void loadSettings()
        {
            // Millimeters/Inches Radio buttons alter text box data upon changing, so set them first.
            if (settings.IsLengthInMillimeters)
            {
                radioButton1.Checked = true;
            }
            else
            {
                radioButton2.Checked = true;
            }
            if (settings.DirFromOrigin > 0)
            {
                radioButton3.Checked = true;
            }
            else
            {
                radioButton4.Checked = true;
            }
            textBox2.Text = "" + settings.NumOfRows;
            textBox3.Text = "" + settings.NumOfIntervals;
            if (settings.IsLengthInMillimeters)
            {
                textBox4.Text = "" + settings.IntervalLengthMM;
            }
            else
            {
                textBox4.Text = "" + settings.IntervalLengthMM / zaber.getMMperInch();
            }
            textBox5.Text = "" + settings.TargetThickness;
            textBox6.Text = "" + settings.AcceptableRange;
            textBox7.Text = "" + settings.ErrorRange;
            textBox8.Text = profiles.DefaultProfile;
            textBox9.Text = "" + settings.SampleSize;
            loadProfileMenu();
            
            moveOrigin(); // Moves zaber to currently set origin
        }

        // Updates Data Grid
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

        // Issue warning if intervals * intervalLength exceeds track length.
        private void warningUpdate()
        {
            // Issue warning if intervals * intervalLength exceeds track length.
            if (textBox3.Text != "" && textBox4.Text != "")
            {
                int sequenceSteps = Convert.ToInt32(Convert.ToInt32(textBox3.Text) * settings.IntervalLengthMM) * zaber.getStepsPerMM();
                int finalPos = settings.ZaberOrigin + sequenceSteps * settings.DirFromOrigin;
                if (finalPos < 0 )
                {

                    label12.Text = "Warning: Final position cannot be reached due to finite track length!\n" +
                        "Final Position will be approximately" + (0-finalPos)/zaber.getStepsPerMM() + "mm past Home (zero) position." +
                        "Consider changing one or more of: Origin, Direction, Interval Length, Intervals Per Sequence";
                }
                else if (finalPos > zaber.getMaxSteps())
                {
                    label12.Text = "Warning: Final position cannot be reached due to finite track length!\n" +
                        "Final Position will be approximately " + (finalPos - zaber.getMaxSteps()) / zaber.getStepsPerMM() + "mm past Home (zero) position." +
                        "Consider changing one or more of: Origin, Direction, Interval Length, Intervals Per Sequence";
                }
                else
                {
                    label12.Text = "";
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        
        // Runs test sequence according to current settings.
        private void TestRowButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("TestRowButton Pushed.");
            runRowThicknessTestRoutine();
        }  
        
        // Prev button. Subtracts 1 from textBox1 value and currentRow variable.
        private void button1_Click(object sender, EventArgs e)
        {
            if(currentRow > 0)
            {
                currentRow--;
                textBox1.Text = ("" + (currentRow + 1));
            }
        }

        // Next button. Adds 1 to textBox1 value and currentRow variable.
        private void button2_Click(object sender, EventArgs e)
        {
            if (currentRow + 1 < settings.NumOfRows)
            {
                currentRow++;
                textBox1.Text = ("" + (currentRow + 1));
            }
        }
        
        // Save button. Saves ThicknessData to csv file
        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = ".csv";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter myStream = new StreamWriter(saveFileDialog1.FileName);
                myStream.Write(data.toString());
                myStream.Close();
            }
        }

        // Clear Data button. Creates new empty ThicknessData using parameters from settings.
        private void button4_Click(object sender, EventArgs e)
        {
            data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
            textBox1.Text = "1";
            dataTextUpdate(0);
        }

        // Set Origin to current zaber position
        private void button5_Click(object sender, EventArgs e)
        {
            settings.ZaberOrigin = zaber.getPos();
            warningUpdate();
        }

        // Length in Millimeters radio button
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                settings.IsLengthInMillimeters = true;
                double value = Convert.ToDouble(textBox4.Text) * zaber.getMMperInch();
                textBox4.Text = "" + (value);
                Update();
            }
        }

        // Length in Inches radio button
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                settings.IsLengthInMillimeters = false;
                double value;
                if (textBox4.Text == "")
                {
                    value = settings.IntervalLengthMM / zaber.getMMperInch();
                }
                else
                {
                    value = Convert.ToDouble(textBox4.Text) / zaber.getMMperInch();
                }
                textBox4.Text = "" + (value);
                Update();
            }
        }

        // Away from home radio button
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                settings.DirFromOrigin = 1;
                warningUpdate();
            }
        }

        // Toward home radio button
        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                settings.DirFromOrigin = -1;
                warningUpdate();
            }
        }

        // Set sequence number to be tested on TestRowButton_Click
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
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
        }

        // Number of Sequences
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
            {
                if (Convert.ToInt32(textBox2.Text) >= 1 && Convert.ToInt32(textBox2.Text) < settings.MaxNumOfRows)
                {
                    settings.NumOfRows = Convert.ToInt32(textBox2.Text);
                    data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
                    dataTextUpdate(0);
                    textBox1.Text = "1";
                }
                else
                {
                    textBox2.Text = ("" + settings.NumOfRows);
                }
            }
            Update();
        }

        // Number of Intervals
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if(textBox3.Text != "")
            {
                if (Convert.ToInt32(textBox3.Text) >= 1)
                {
                    settings.NumOfIntervals = Convert.ToInt32(textBox3.Text);
                    data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
                    dataTextUpdate(0);
                    textBox1.Text = "1";
                    warningUpdate(); // Possible tracklength warning
                }
                else
                {
                    textBox3.Text = ("" + settings.NumOfIntervals);
                }
            }
            Update();
        }

        // Length of Intervals in Millimeters
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (textBox4.Text != "" && textBox4.Text != ".")
            {
                if (Convert.ToDouble(textBox4.Text) >= 0)
                {
                    Double lengthMM;
                    if (radioButton1.Checked)
                    {
                        lengthMM = Convert.ToDouble(textBox4.Text);
                    }
                    else
                    {
                        lengthMM = zaber.getMMperInch()*Convert.ToDouble(textBox4.Text);
                    }
                    settings.IntervalLengthMM = lengthMM;
                    warningUpdate(); // Possible tracklength warning.
                }
                else
                {
                    textBox4.Text = ("" + settings.IntervalLengthMM);
                }
            }
            Update();
        }
        
        // Set Target Thickness
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if(textBox5.Text != "")
            {
                settings.TargetThickness = Convert.ToDouble(textBox5.Text);
            }
        }

        // Set Acceptable Range
        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (textBox6.Text != "")
            {
                settings.AcceptableRange = Convert.ToDouble(textBox6.Text);
            }
        }

        // Set Error Range
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (textBox7.Text != "")
            {
                settings.ErrorRange = Convert.ToDouble(textBox7.Text);
            }
        }

        // Set Keyence Sample Size
        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            if (textBox9.Text != "")
            {
                settings.SampleSize = Convert.ToInt32(textBox9.Text);
            }
        }

        // Default Settings button
        private void button7_Click(object sender, EventArgs e)
        {
            settings = new Settings();
            profiles.DefaultProfile = "";
            loadSettings();
        }

        // Save Settings button
        private void button6_Click(object sender, EventArgs e)
        {
            if(textBox8.Text == "")
            {
                label13.Text = "Required Field";
            }
            else
            {
                profiles.addProfile(settings, textBox8.Text);
                profiles.DefaultProfile = textBox8.Text;
                profiles.saveToFile();
                loadProfileMenu();
            }
        }

        // Profile Name
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            label13.Text = "";
        }

        // Profile Selection Dropdown Menu
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            settings = profiles.getProfile(comboBox1.Text);
            profiles.DefaultProfile = comboBox1.Text;
            loadSettings();            
        }
        
        // Populates drowdown list on comboBox1
        private void loadProfileMenu()
        {
            comboBox1.Items.Clear();
            String[] profileNames = profiles.getKeys();
            foreach (String name in profileNames)
            {
                comboBox1.Items.Add(name);
            }
        }

        // Delete Profile Button
        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                String profile = textBox8.Text;
                textBox8.Text = "";
                label13.Text = profile + " Deleted";
                profiles.removeProfile(profile);
                loadProfileMenu();
            }
            catch
            {
                label13.Text = "Pofile Not Found";
            }
        }

        // Moves zaber to currently set Origin
        public void moveOrigin()
        {
            zaber.finishMove();
            zaber.moveABS(settings.ZaberOrigin);
            zaber.finishMove();
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
