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

            dataTextInitialize();
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

            tabControl1.Dock = DockStyle.Fill;
        }

        // Runs Keyence and Zaber through thickness test routine, and stores their data feedback. **Called Asynchronously**
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
            }
            catch (System.ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                zaberDisconnected();
            }

            double tempSample = 0; // used to store Error Correction samples so lastSample retains original sample if all three samples are bad.
            double offset = 0; // This variable is used to keep track of offset positions in the event
            // that Active Error Correction is activated.
            double retestDistance = 5; // This is how far (millimeters) the zaber moves from position to take a new sample.
            for (int count = 0; count < settings.NumOfIntervals && !abortTestRoutine; count++)
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
                        tempSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
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
                    if (tempSample < settings.TargetThickness - settings.ErrorRange ||
                    tempSample > settings.TargetThickness + settings.ErrorRange)
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
                            tempSample = keyence.averageOfXSamples(settings.SampleSize, settings.TargetThickness, settings.ErrorRange);
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
                    if (tempSample < settings.TargetThickness - settings.ErrorRange ||
                    tempSample > settings.TargetThickness + settings.ErrorRange)
                    {
                        lastSample = tempSample;
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
            try
            { 
                if(settings.DirFromOrigin > 0)
                {
                    zaber.moveABS(settings.ZaberOrigin);
                    zaber.finishMove();
                    zaber.goHome();
                    zaber.finishMove();
                    zaber.moveABS(settings.ZaberOrigin);
                }
                else
                {
                    zaber.goHome();
                    zaber.finishMove();
                    zaber.moveABS(settings.ZaberOrigin);
                    zaber.finishMove();
                }
                
            }
            catch (System.ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                zaberDisconnected();
            }           
        }

        // Updates and populates all controls in the settings tab with data from this.settings.
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
            updateDistanceMarkLabel();
        }

        // Initializes Data Grid
        private void dataTextInitialize()
        {            
            richTextBox1.Clear();
            for (int row = settings.NumOfIntervals-1; row >= 0; row--)
            {
                string rowString;
                if (settings.IsLengthInMillimeters)
                {
                    rowString = String.Format("{0,7}", "" + Convert.ToInt32(row * settings.IntervalLengthMM));
                }
                else
                {
                    rowString = String.Format("{0,7}", ("" + (row * settings.IntervalLengthMM)/zaber.getMMperInch()));
                }
                richTextBox1.AppendText("\n " + rowString + "  |  ", Color.Black);

                // settings.NumOfRows in this context refers to the number of sequences, or the number of columns
                // in the thickness data. Dumb, I know...
                for (int column = 0; column < settings.NumOfRows; column++)
                {
                    string valueString = string.Format("{0:00.00}", data.getValueAt(column, row));

                    richTextBox1.AppendText(" " + valueString + " ", Color.Gray);                   
                }
                richTextBox1.AppendText("\n          |  ", Color.Black);
            }
            richTextBox1.AppendText("__");
            for (int column = 0; column < settings.NumOfRows; column++)
            {
                richTextBox1.AppendText("_______", Color.Black);
            }
            richTextBox1.AppendText("\n     0      ");
            for (int column = 0; column < settings.NumOfRows; column++)
            {
                string columnString;
                if (settings.IsLengthInMillimeters)
                {
                    columnString = String.Format("{0,6}", ("" + Convert.ToInt32(column * settings.IntervalLengthMM)));
                }
                else
                {
                    columnString = String.Format("{0,6}", ("" + (column * settings.IntervalLengthMM) / zaber.getMMperInch()));
                }
                richTextBox1.AppendText(" " + columnString, Color.Black);
            }
            richTextBox1.Update();
        }

        // Updates Data Grid by changing a single value.
        private void dataTextUpdate(int currentInterval)
        {
            try
            {
                richTextBox1.DeselectAll();
            }
            catch
            {
            }
            int textRow = settings.NumOfIntervals - currentInterval - 1;
            int valueStartPos = textRow * (settings.NumOfRows * 7 + 14) + (currentRow * 7) + textRow * 14 + 15;
            try
            {
                richTextBox1.Select(valueStartPos, 5);
            }
            catch
            {
            }
            double value = data.getValueAt(currentRow, currentInterval);
            string valueString = string.Format("{0:00.00}", value);
            try
            {
                richTextBox1.SelectedText = valueString;
            }
            catch
            {
            }
            // Set color depending on data value
            try
            {
                if (value < settings.TargetThickness - settings.ErrorRange || value > settings.TargetThickness + settings.ErrorRange)
                {
                    richTextBox1.SelectionColor = Color.Gray;
                }
                else if (value < settings.TargetThickness - settings.AcceptableRange)
                {
                    richTextBox1.SelectionColor = Color.Red;
                }
                else
                {
                    richTextBox1.SelectionColor = Color.Black;
                }
            }
            catch
            {
            }
            try
            {
                richTextBox1.Update();
            }
            catch
            {
            }
        }

        // Updates the text color of the last row to be tested. (can't do it cross-thread)
        private void dataTextColorUpdate(int lastRow)
        {                 
            if(lastRow < 0) // This only occurs in the case where a test routine is aborted on row[0]
            {
                lastRow = 0;
            }
            richTextBox1.DeselectAll();
            for (int count = 0; count < settings.NumOfIntervals; count++) {
                int textRow = settings.NumOfIntervals - count - 1;
                int valueStartPos = textRow * (settings.NumOfRows * 7 + 14) + (lastRow * 7) + textRow * 14 + 15;
                richTextBox1.Select(valueStartPos, 5);
                double value = data.getValueAt(lastRow, count);
                // Set color depending on data value
                if (value < settings.TargetThickness - settings.ErrorRange)
                {
                    richTextBox1.SelectionColor = Color.Gray;
                }
                else if (value < settings.TargetThickness - settings.AcceptableRange)
                {
                    richTextBox1.SelectionColor = Color.Red;
                }
                else if (value > settings.TargetThickness + settings.AcceptableRange)
                {
                    richTextBox1.SelectionColor = Color.Blue;
                }
                else
                {
                    richTextBox1.SelectionColor = Color.Black;
                }
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
                        "Final Position will be approximately " + (finalPos - zaber.getMaxSteps()) / zaber.getStepsPerMM() + "mm past End Of Track." +
                        "Consider changing one or more of: Origin, Direction, Interval Length, Intervals Per Sequence";
                }
                else
                {
                    label12.Text = "";
                }
            }
        }
        
        // This is a Button. Runs test sequence according to current settings. **Runs BackgroundWorker**
        private void TestRowButton_Click(object sender, EventArgs e)
        {
            RunTestButton.Visible = false;
            RunTestButton.Enabled = false;
            button11.Enabled = true;
            button11.Visible = true;

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_doWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_finished);

            backgroundWorker.RunWorkerAsync();
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
            saveFileDialog1.AddExtension = false;
            saveFileDialog1.InitialDirectory = profiles.DefaultSaveLocation;
            //saveFileDialog1.DefaultExt = ".csv";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (profiles.isControlled())
                {
                    saveFileDialog1.FileName += ".csv";
                }
                else
                {
                    saveFileDialog1.FileName += "_UnControlled.csv";
                }
                profiles.DefaultSaveLocation = Path.GetDirectoryName(saveFileDialog1.FileName);
                StreamWriter myStream = new StreamWriter(saveFileDialog1.FileName);
                myStream.Write(data.toString());
                myStream.Close();
            }
            profiles.saveInternalSettings();
        }

        // Clear Data button. Creates new empty ThicknessData using parameters from settings.
        private void button4_Click(object sender, EventArgs e)
        {
            data = new ThicknessData(settings.NumOfRows, settings.NumOfIntervals);
            textBox1.Text = "1";
            dataTextInitialize();
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
                    label1.ForeColor = Color.Red;
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
                    zaber.goHome();
                    zaber.finishMove();
                    zaber.moveABS(settings.ZaberOrigin);
                    zaber.finishMove();
                }
                else
                {
                    label14.Text = "Zaber: Failed";
                    label14.ForeColor = Color.Red;
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
                moveCursorToEndOfText(textBox10);
                SendKeys.Send("{ENTER}"); // This can create issues during debugging.
                profiles.setControlledProfilesFilePath(textBox10.Text);
                profiles.loadProfiles();
                settings = profiles.getProfile(profiles.getDefaultProfileName());
                loadSettings();
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
            updateDistanceMarkLabel();
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
            updateDistanceMarkLabel();
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
                    if (settings.IsLengthInMillimeters)
                    {
                        label16.Text = ("Go to the  " + (settings.IntervalLengthMM * currentRow) + "  mm mark.");
                    }
                    else
                    {
                        label16.Text = ("Go to the  " + ((settings.IntervalLengthMM / zaber.getMMperInch()) * currentRow) + "  inch mark.");
                    }
                }
            }
            catch
            {
                textBox1.Text = (1 + "");
            }
            moveCursorToEndOfText(textBox1);
            updateDistanceMarkLabel();
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
                        dataTextInitialize();
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
                        dataTextInitialize();
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
            updateDistanceMarkLabel();
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
            if (textBox8.Text.Contains(","))
            {
                textBox8.Text = textBox8.Text.Remove(textBox8.Text.Length-1, 1);
                moveCursorToEndOfText(textBox8);
                label13.Text = "No Commas";
            }
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

        // Font Size
        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox11.Text != "")
                {
                    System.Drawing.Font font = richTextBox1.SelectionFont;
                    if (Convert.ToSingle(textBox11.Text) < 1 || Convert.ToSingle(textBox11.Text) > 48)
                    {
                        richTextBox1.Font = new Font("Courier New", 8);
                        textBox11.Text = (8 + "");
                    }
                    else
                    {
                        richTextBox1.Font = new Font("Courier New", Convert.ToSingle(textBox11.Text));
                    }
                    textBox11.Update();
                }
            }
            catch
            {
                textBox11.Text = (8 + "");
            }
            moveCursorToEndOfText(textBox11);
            richTextBox1.Update();
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
                        button12_Click(new object(), new EventArgs());
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
                        button12_Click(new object(), new EventArgs());
                    }
                }            
            }
            updateSettingsControlsEnabledStatus();
            try
            {
                profiles.loadProfiles();
                settings = profiles.getProfile(profiles.getDefaultProfileName());
                loadSettings();
            }
            catch(FileNotFoundException)
            {
                label13.Text = "Settings File Not Found";
            }
            
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

        // Updates "Go to 'distance' mark" label
        private void updateDistanceMarkLabel()
        {
            if (settings.IsLengthInMillimeters)
            {
                label16.Text = ("Go to the  " + (settings.IntervalLengthMM * currentRow) + "  mm mark.");
            }
            else
            {
                label16.Text = ("Go to the  " + ((settings.IntervalLengthMM / zaber.getMMperInch()) * currentRow) + "  inch mark.");
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

        // Runs the test routine on a separate thread
        private void backgroundWorker_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            runRowThicknessTestRoutine();
        }

        // Resets buttons when the background worker finishes
        private void backgroundWorker_finished(object sender, RunWorkerCompletedEventArgs e)
        {            
            button11.Enabled = false;
            button11.Visible = false;
            RunTestButton.Enabled = true;
            RunTestButton.Visible = true;
            if (!abortTestRoutine)
            {
                currentRow++;
                dataTextColorUpdate(currentRow-1);
                if (currentRow + 1 > settings.NumOfRows) { currentRow--; };
                textBox1.Text = ("" + (currentRow + 1));
            }            

            richTextBox1.DeselectAll();
            abortTestRoutine = false;
        }

        // Resizes tabs and richTextBox1
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            
            //richTextBox1.Width = Form1.ActiveForm.Width - 40;
            //richTextBox1.Height = Form1.ActiveForm.Height - 115;
        }

        // Resizes richTextBox1 based on changes in tabControl1 size changes (that is changed with form1 resizing)
        private void tabControl1_SizeChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Stalling");
            if (Form1.ActiveForm != null)
            {
                richTextBox1.Width = tabControl1.Width - 20; // - 20
                richTextBox1.Height = tabControl1.Height - 90; // - 70
            }
        }
    }

    // Extends functionality for RichTextBoxes
    public static class RichTextBoxExtensions
    {
        // Enables text appending on RichTextBoxes with ability to set color.
        // *********** No longer called in dataTextUpdate. Still called in dataTextInitialize.
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
