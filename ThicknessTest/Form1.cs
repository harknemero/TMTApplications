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
        private int currentRow;
        private double lastSample;
        private bool abortTestRoutine;
        private static int waitTimeToStabilize = 200;

        public Form1(ZaberCTRL z, KeyenceCTRL k, Settings s, ThicknessData d, Profiles p)
        {
            zaber = z;
            keyence = k;
            settings = s;
            data = d;
            profiles = p;
            currentRow = 0;
            lastSample = 0;
            abortTestRoutine = false;

            InitializeComponent();

            dataTextUpdate(0);
            if(profiles.getDefaultProfileName() != "")
            {
                settings = profiles.getProfile(profiles.getDefaultProfileName());
            }
            loadSettings();
            if (!keyence.isOpen())
            {
                keyenceDisconnected();
            }
            if (!zaber.isOpen())
            {
                zaberDisconnected();
            }
        }

        // Runs Keyence and Zaber through thickness test routine, and stores their data feedback.
        private void runRowThicknessTestRoutine()
        {
//            RunTestButton.Visible = false;
//            RunTestButton.Enabled = false;
//            button11.Enabled = true;
//            button11.Visible = true;

            try
            {
                if (zaber.getPos() != settings.ZaberOrigin)
                {
                    zaber.finishMove(); // Just in case operation begins from an unexpected zaber state.
                    zaber.moveABS(settings.ZaberOrigin);
                    zaber.finishMove();
                }
            }
            catch (System.ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                zaberDisconnected();
            }
                
            double offset = 0; // This variable is used to keep track of offset positions in the event
            // that Active Error Correction is activated.
            double retestDistance = 5; // This is how far the zaber moves from position to take a new sample.
            for (int count = 0; count < settings.NumOfIntervals; count++)
            {
                if (abortTestRoutine)
                {
                    count = settings.NumOfIntervals;
                }
                else
                {
                    try
                    {
                        lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ArgumentException || ex is InvalidOperationException)
                        {
                            Console.WriteLine(ex.Message);
                            keyenceDisconnected();
                        }
                        else
                        {
                            throw new System.ArgumentException("Unknown Exception On Keyence Call.");
                        }
                    }
                    // Active Error Correction Round 1
                    if (lastSample < settings.TargetThickness - settings.ErrorRange ||
                        lastSample > settings.TargetThickness + settings.ErrorRange)
                    {
                        try
                        {
                            zaber.moveRel(retestDistance, settings.DirFromOrigin * (-1));
                            offset = retestDistance;
                            zaber.finishMove();
                        }
                        catch (System.ArgumentException ex)
                        {
                            Console.WriteLine(ex.Message);
                            zaberDisconnected();
                        }
                        System.Threading.Thread.Sleep(waitTimeToStabilize);
                        try
                        {
                            lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
                        }
                        catch (Exception ex)
                        {
                            if (ex is ArgumentException || ex is InvalidOperationException)
                            {
                                Console.WriteLine(ex.Message);
                                keyenceDisconnected();
                            }
                            else
                            {
                                throw new System.ArgumentException("Unknown Exception On Keyence Call.");
                            }
                        }
                        // Active Error Correction Round 2
                        if (lastSample < settings.TargetThickness - settings.ErrorRange ||
                        lastSample > settings.TargetThickness + settings.ErrorRange)
                        {
                            try
                            {
                                zaber.moveRel(retestDistance * 2, settings.DirFromOrigin);
                                offset = retestDistance * (-1);
                                zaber.finishMove();
                            }
                            catch (System.ArgumentException ex)
                            {
                                Console.WriteLine(ex.Message);
                                zaberDisconnected();
                            }
                            System.Threading.Thread.Sleep(waitTimeToStabilize);
                            try
                            {
                                lastSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
                            }
                            catch (Exception ex)
                            {
                                if (ex is ArgumentException || ex is InvalidOperationException)
                                {
                                    Console.WriteLine(ex.Message);
                                    keyenceDisconnected();
                                }
                                else
                                {
                                    throw new System.ArgumentException("Unknown Exception On Keyence Call.");
                                }
                            }
                        }
                    }

                    data.recordData(currentRow, count, lastSample);
                    dataTextUpdate(count);
                    if (count + 1 < settings.NumOfIntervals)
                    {
                        try
                        {
                            zaber.moveRel(settings.IntervalLengthMM + offset, settings.DirFromOrigin);
                            offset = 0; // Offset should be corrected now. Reset to 0.
                            zaber.finishMove();
                        }
                        catch (System.ArgumentException ex)
                        {
                            Console.WriteLine(ex.Message);
                            zaberDisconnected();
                        }
                        System.Threading.Thread.Sleep(200);
                    }
                }
            }
            try { 
            zaber.moveABS(settings.ZaberOrigin);
            zaber.finishMove();
            }
            catch (System.ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                zaberDisconnected();
            }
            if (currentRow + 1 < settings.NumOfRows && !abortTestRoutine)
            {
                currentRow++;
                textBox1.Text = ("" + (currentRow + 1));
            }
            abortTestRoutine = false;
 //           button11.Enabled = false;
 //           button11.Visible = false;
 //           RunTestButton.Enabled = true;
 //           RunTestButton.Visible = true;
        }

        // Updates and populates all controls in the settings tab.
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
            textBox8.Text = profiles.getDefaultProfileName();
            textBox9.Text = "" + settings.SampleSize;
            textBox10.Text = profiles.getControlledProfilesFilePath();
            loadProfileMenu();

            try { 
            moveOrigin(); // Moves zaber to currently set origin
            }
            catch (System.ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                zaberDisconnected();
            }
            if (profiles.isControlled())
            {
                checkBox1.Checked = true;
            }
            else
            {
                checkBox1.Checked = false;
            }
            updateSettingsControlsEnabledStatus();
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
        
        // This is a Button. Runs test sequence according to current settings.
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

        // Save Settings button
        private void button6_Click(object sender, EventArgs e)
        {
            if (!profiles.isControlled())
            {
                if (textBox8.Text == "")
                {
                    label13.Text = "Required Field";
                }
                else
                {
                    profiles.addProfile(settings, textBox8.Text);
                    profiles.setDefaultProfileName(textBox8.Text);
                    loadProfileMenu();
                }
            }
        }

        // Default Settings button
        private void button7_Click(object sender, EventArgs e)
        {
            settings = new Settings();
            profiles.setDefaultProfileName("");
            loadSettings();
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
                label13.Text = "Profile Not Found";
            }
        }

        // Connect Keyence Button
        private void button9_Click(object sender, EventArgs e)
        {
            if (!keyence.isOpen())
            {
                if (keyence.openKeyence())
                {
                    label1.Text = "Keyence: Connected";
                    label1.ForeColor = Color.Green;
                    if (zaber.isOpen())
                    {
                        RunTestButton.Enabled = true;
                    }
                }
                else
                {
                    label1.Text = "Keyence: Failed";
                }
            }
        }

        // Connect Zaber Button
        private void button10_Click(object sender, EventArgs e)
        {
            if (!zaber.isOpen())
            {
                if (zaber.openZaber())
                {
                    label14.Text = "Zaber: Connected";
                    label14.ForeColor = Color.Green;
                    if (keyence.isOpen())
                    {
                        RunTestButton.Enabled = true;
                    }
                }
                else
                {
                    label14.Text = "Zaber: Failed";
                }
            }
        }

        // Abort Button
        private void button11_Click(object sender, EventArgs e)
        {            
            abortTestRoutine = true;
            button11.Enabled = false;
            button11.Visible = false;
        }

        // Browse Button
        private void button12_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.AddExtension = true;
            openFileDialog1.DefaultExt = ".txt";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox10.Text = openFileDialog1.FileName;
                textBox10.Focus();
                SendKeys.Send("{ENTER}"); // This can create issues during debugging.
            }
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
            try
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
            catch
            {
                textBox1.Text = (1 + "");
            }
            moveCursorToEndOfText(textBox1);
        }

        // Number of Sequences
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
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
            }
            catch
            {
                textBox2.Text = ("" + settings.NumOfRows);
            }
            Update();
            moveCursorToEndOfText(textBox2);
        }

        // Number of Intervals
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try { 
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
            }
            catch
            {
                textBox3.Text = ("" + settings.NumOfIntervals);
            }
            Update();
            moveCursorToEndOfText(textBox3);
        }

        // Length of Intervals in Millimeters
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            try
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
                            lengthMM = zaber.getMMperInch() * Convert.ToDouble(textBox4.Text);
                        }
                        settings.IntervalLengthMM = lengthMM;
                        warningUpdate(); // Possible tracklength warning.
                    }
                    else
                    {
                        textBox4.Text = ("" + settings.IntervalLengthMM);
                    }
                }
            }
            catch
            {
                textBox4.Text = ("" + settings.IntervalLengthMM);
            }
            Update();
            moveCursorToEndOfText(textBox4);
        }
        
        // Set Target Thickness
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox5.Text != "")
                {
                    settings.TargetThickness = Convert.ToDouble(textBox5.Text);
                }
            }
            catch
            {
                if (textBox5.Text == ".")
                {
                    textBox5.Text = "0.";
                }
                else
                {
                    textBox5.Text = ("" + settings.TargetThickness);
                }
            }
            moveCursorToEndOfText(textBox5);
        }

        // Set Acceptable Range
        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox6.Text != "")
                {
                    settings.AcceptableRange = Convert.ToDouble(textBox6.Text);
                }
            }
            catch
            {
                if (textBox6.Text == ".")
                {
                    textBox6.Text = "0.";
                }
                else
                {
                    textBox6.Text = ("" + settings.AcceptableRange);
                }
            }
            moveCursorToEndOfText(textBox6);
        }

        // Set Error Range
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox7.Text != "")
                {
                    settings.ErrorRange = Convert.ToDouble(textBox7.Text);
                }
            }
            catch
            {
                if (textBox7.Text == ".")
                {
                    textBox7.Text = "0.";
                }
                else
                {
                    textBox7.Text = ("" + settings.ErrorRange);
                }
            }
            moveCursorToEndOfText(textBox7);
        }
        
        // Profile Name
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            label13.Text = "";
            moveCursorToEndOfText(textBox8);
        }

        // Set Keyence Sample Size
        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox9.Text != "")
                {
                    settings.SampleSize = Convert.ToInt32(textBox9.Text);
                }
            }
            catch
            {                
                textBox9.Text = ("" + settings.SampleSize);                
            }

            moveCursorToEndOfText(textBox9);
        }

        // Controlled Profiles Settings File Path
        private void textBox10_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                profiles.setControlledProfilesFilePath(textBox10.Text);
            }
        }

        // Profile Selection Dropdown Menu
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            settings = profiles.getProfile(comboBox1.Text);
            profiles.setDefaultProfileName(comboBox1.Text);
            loadSettings();            
        }

        // Use Controlled Profile Settings Checkbox
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                if (!profiles.isControlled())
                {
                    try
                    {
                        profiles.setControlled(true);
                    }
                    catch
                    {
                        button12.PerformClick();
                    }
                }
            }
            else
            {
                if (profiles.isControlled())
                {
                    try
                    {
                        profiles.setControlled(false);
                    }
                    catch
                    {
                        button12.PerformClick();
                    }
                }            
            }
            loadSettings();
        }

        // Updates Enabled/Disabled Status of Settings Tab Controls
        private void updateSettingsControlsEnabledStatus()
        {
            if (profiles.isControlled())
            {
                this.Text = "ClearShield Thickness Test - Controlled";
                // Enable all controlled profile options.
                {
                    textBox10.Enabled = true;
                    button12.Enabled = true;
                }
                // Disable and make read only all settings options
                {
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                    textBox4.Enabled = false;
                    textBox5.Enabled = false;
                    textBox6.Enabled = false;
                    textBox7.Enabled = false;
                    textBox8.Enabled = false;
                    textBox9.Enabled = false;
                    radioButton1.Enabled = false;
                    radioButton2.Enabled = false;
                    radioButton3.Enabled = false;
                    radioButton4.Enabled = false;
                }
            }
            else 
            {
                this.Text = "ClearShield Thickness Test - Uncontrolled";
                // Disable all controlled profile options.
                {
                    textBox10.Enabled = false;
                    button12.Enabled = false;
                }
                // Enable and make read only all settings options
                {
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                    textBox4.Enabled = true;
                    textBox5.Enabled = true;
                    textBox6.Enabled = true;
                    textBox7.Enabled = true;
                    textBox8.Enabled = true;
                    textBox9.Enabled = true;
                    radioButton1.Enabled = true;
                    radioButton2.Enabled = true;
                    radioButton3.Enabled = true;
                    radioButton4.Enabled = true;
                }
            }  
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

        // Moves zaber to currently set Origin
        private void moveOrigin()
        {
            zaber.finishMove();
            zaber.moveABS(settings.ZaberOrigin);
            zaber.finishMove();
        }

        // Disables RunTestButton and sets Keyence status label
        private void keyenceDisconnected()
        {
            if (!keyence.isOpen())
            {
                abortTestRoutine = true;
                RunTestButton.Enabled = false;
                label1.Text = "Keyence: Not Connected";
                label1.ForeColor = Color.Red;
            }
        }

        // Disables RunTestButton and sets Zaber status label
        private void zaberDisconnected()
        {
            if (!zaber.isOpen())
            {
                abortTestRoutine = true;
                RunTestButton.Enabled = false;
                label14.Text = "Zaber: Not Connected";
                label14.ForeColor = Color.Red;
            }
        }

        // Moves Cursor to the end of the text in any text box.
        private void moveCursorToEndOfText(TextBox tbox)
        {
            if (tbox.Text != "")
            {
                tbox.SelectionStart = tbox.Text.Length;
                tbox.SelectionLength = 0;
            }
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
