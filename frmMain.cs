using J2534.Checksums;
using J2534.Display;
using J2534.Flash;
using J2534.Flash.ECU;
using J2534.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace J2534
{
    public partial class frmMain : Form
    {
        private byte[] readout = new byte[0];
        private byte[] ramReadout = new byte[0];
        public readonly string HKCU_RUN = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        public readonly string APP_KEY = "Software\\" + Application.ProductName;
        private byte[] binFile;
        private int flashTime;
        private ToolComm dice;
        private ECUProgrammer eProg;
        private bool flash512;
        private bool p80;
        private long logTime;
        private ECULogger logger;
        private int numLogSessions;
        private StateObjClass StateObj;
        private Utility.ModifyRegistry.ModifyRegistry regedit;
        [Obfuscation(Exclude = true, Feature = "default", StripAfterObfuscation = false)]
        private ECUParameters eParams;
        [Obfuscation(Exclude = true, Feature = "default", StripAfterObfuscation = false)]
        private ECUVariables eVars;
        private CultureInfo culture;
        private StreamWriter logFile;

        public frmMain()
        {
            this.InitializeComponent();
            this.culture = CultureInfo.CreateSpecificCulture("en-GB");
            Thread.CurrentThread.CurrentCulture = this.culture;
            Thread.CurrentThread.CurrentUICulture = this.culture;
            this.eParams = new ECUParameters();
            this.eVars = new ECUVariables();
            this.dice = new ToolComm();
        }

        // =================================================================
        // FORM EVENTS
        // =================================================================

        private void Form1_Load(object sender, EventArgs e)
        {
            this.updateDevices();
            this.regedit = new Utility.ModifyRegistry.ModifyRegistry();
            this.txtParamsFile.Text = this.getECUParams();
            try
            {
                if (this.txtParamsFile.Text.Equals(""))
                    return;
                if (this.txtParamsFile.Text.Split('.')[this.txtParamsFile.Text.Split('.').Length - 1].Equals("xml"))
                {
                    this.parseParameters();
                    this.SetComboXML();
                }
                else
                {
                    this.txtParamsFile.Text = "";
                    this.setECUParams("");
                }
            }
            catch (Exception ex)
            {
                this.txtParamsFile.Text = "";
                this.setECUParams("");
                Console.WriteLine(ex.ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.dice.DeviceOpen)
                return;
            this.dice.disconnectFinal();
        }

        // =================================================================
        // CONNECTION
        // =================================================================

        private void cmdDetectDevices_Click(object sender, EventArgs e)
        {
            this.updateDevices();
        }

        private void updateDevices()
        {
            List<J2534Device> installedDevices = ToolComm.getInstalledDevices();
            if (installedDevices.Count == 0)
            {
                MessageBox.Show("Could not find any installed J2534 devices.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else
            {
                this.comboBox_J2534_devices.DataSource = installedDevices;
            }
        }

        private bool BitBashConnect()
        {
            this.dice.setHSBaud(BaudRate.CAN_500000);
            this.dice.setJ2534Device(ToolComm.getInstalledDevices()[this.comboBox_J2534_devices.SelectedIndex]);
            if (!this.dice.connect())
                return false;

            uint msgid1 = 0;
            uint msgid2 = 0;
            this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid1, 1000U, CANChannel.HS);
            Thread.Sleep(1200);
            bool flag = this.dice.sendMsgCheckDiagResponse(VolvoECUCommands.msgCANReadECMSerial, CANChannel.HS, (byte)249);

            if (!flag)
            {
                this.dice.stopPeriodicMsg(msgid1, CANChannel.HS);
                this.dice.disconnect();
                this.dice.setHSBaud(BaudRate.CAN_250000);
                if (!this.dice.connect())
                    return false;
                Thread.Sleep(2200);
                this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid1, 1000U, CANChannel.HS);
                this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid2, 1000U, CANChannel.MS);
                Thread.Sleep(1200);
                flag = this.dice.sendMsgCheckDiagResponse(VolvoECUCommands.msgCANReadECMSerial, CANChannel.HS, (byte)249);
            }

            this.dice.stopPeriodicMsg(msgid1, CANChannel.HS);
            if (this.dice.getHSBaud() == BaudRate.CAN_250000)
                this.dice.stopPeriodicMsg(msgid2, CANChannel.MS);
            return flag;
        }

        private void but_connectdice_Click(object sender, EventArgs e)
        {
            bool flag = this.BitBashConnect();
            switch (this.dice.getHSBaud())
            {
                case BaudRate.CAN_250000:
                    this.progressFlash.Maximum = 114;
                    break;
                case BaudRate.CAN_500000:
                    this.progressFlash.Maximum = 81;
                    break;
            }

            if (flag)
            {
                this.but_connectdice.Enabled = false;
                this.cmdReset.Enabled = true;
                this.comboBox_J2534_devices.Enabled = false;
                this.cmdDetectDevices.Enabled = false;
                this.changeButtonState(true);
                this.lblConnectionStatus.Text = "  Connected";
                this.lblConnectionStatus.ForeColor = Color.FromArgb(72, 180, 72);
                this.tsslStatus.Text = "Connected to ECU via " +
                    (this.dice.getHSBaud() == BaudRate.CAN_500000 ? "500k" : "250k") + " CAN";
            }
            else
            {
                MessageBox.Show("DiCE is not connected",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                try { this.dice.disconnect(); }
                catch (Exception) { }
                this.dice = new ToolComm();
            }
        }

        // =================================================================
        // FLASH OPERATIONS
        // =================================================================

        private void openBINFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Volvo ME7 BIN File (*.bin)|*.bin";
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            long num1 = new FileInfo(openFileDialog.FileName).Length / 1024L;
            switch (num1)
            {
                case 512:
                case 1024:
                    this.txtBin.Text = openFileDialog.FileName;
                    this.flash512 = num1 == 512L;
                    try
                    {
                        VolvoChecksumUpdater volvoChecksumUpdater = new VolvoChecksumUpdater(openFileDialog.FileName);
                        if (!volvoChecksumUpdater.updateChecksums(true))
                        {
                            if (!volvoChecksumUpdater.updateChecksums(false))
                            {
                                MessageBox.Show("Unknown error updating checksums!",
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                                Environment.Exit(3);
                            }
                            else
                            {
                                MessageBox.Show("Checksums updated!",
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                            }
                        }
                        this.binFile = File.ReadAllBytes(openFileDialog.FileName);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        break;
                    }
                default:
                    MessageBox.Show("Incorrect File Size!",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    break;
            }
        }

        private void saveBINFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "ECU Binary (*.bin)|*.bin";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;
            this.txtBin.Text = saveFileDialog.FileName;
        }

        private void cmdFlash_Click(object sender, EventArgs e)
        {
            if (!this.dice.DeviceOpen)
            {
                MessageBox.Show("Please connect the J2534 Cable first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            this.openBINFile();
            if (this.txtBin.Text == "")
            {
                MessageBox.Show("Please choose a BIN file.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            if (MessageBox.Show("Key MUST be in POS II with the engine NOT running. Are you ready?",
                Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                this.txtBin.Text = "";
                return;
            }

            this.flashTime = 0;
            this.changeButtonState(false);
            int length = this.binFile.Length;
            if (this.binFile[length - 1] != (byte)131 || this.binFile[length - 2] != (byte)131)
            {
                MessageBox.Show("This is not a valid BIN file for this platform.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                this.changeButtonState(true);
                this.txtBin.Text = "";
                return;
            }

            if (this.flash512)
                this.progressFlash.Maximum = 56;
            this.eProg = new ECUProgrammer(this.dice, VolvoECUCommands.sbl, this.binFile, this.flash512, this.p80);
            this.eProg.startFlash();
            this.tsslStatus.Text = "Flashing ECU...";
            this.flashTimer.Start();
        }

        private void flashTimer_Tick(object sender, EventArgs e)
        {
            this.lblTime.Text = "Flash Time: " + this.flashTime.ToString() + "s";
            lock (new object())
            {
                this.progressFlash.Value = this.flashTime > this.progressFlash.Maximum
                    ? this.progressFlash.Maximum : this.flashTime;
                if (this.eProg.doneFlashing)
                {
                    this.flashTimer.Stop();
                    this.changeButtonState(true);
                    this.lblTime.Text = "Done Flashing!";
                    this.progressFlash.Value = 0;
                    this.tsslStatus.Text = "Flash complete";
                }
            }
            ++this.flashTime;
        }

        // =================================================================
        // READ OPERATIONS
        // =================================================================

        private void cmdRead_Click(object sender, EventArgs e)
        {
            this.saveBINFile();
            if (this.txtBin.Text == "")
            {
                MessageBox.Show("Please choose a BIN file.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            if (!this.dice.DeviceOpen)
            {
                MessageBox.Show("Please connect the DiCE first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            if (MessageBox.Show("Is the key in pos II with the engine NOT running?",
                Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                this.txtBin.Text = "";
                return;
            }

            this.flashTime = 0;
            this.changeButtonState(false);
            this.eProg = new ECUProgrammer(this.dice, VolvoECUCommands.sbl, (byte[])null, this.flash512, this.p80);
            new Thread(() =>
            {
                this.eProg.sendModuleReset();
                this.eProg.sendSilence();
                this.eProg.startPBL();
                this.eProg.startSBL(VolvoECUCommands.sbl, true);
                this.readout = this.eProg.readECU(false);
                this.ramReadout = this.eProg.readECU(true);
                this.eProg.sendReset();
                Thread.Sleep(2000);
                if (!this.p80)
                    new DIMComm(this.dice, false).setTime();
            }).Start();
            this.progressFlash.Maximum = 861;
            this.tsslStatus.Text = "Reading ECU...";
            this.readTimer.Start();
        }

        private void readTimer_Tick(object sender, EventArgs e)
        {
            this.lblTime.Text = "Read Time: " + this.flashTime.ToString() + "s";
            lock (new object())
            {
                this.progressFlash.Value = this.flashTime > this.progressFlash.Maximum
                    ? this.progressFlash.Maximum : this.flashTime;
                if (this.eProg.doneFlashing)
                {
                    this.readTimer.Stop();
                    this.progressFlash.Value = this.progressFlash.Maximum;
                    string str = this.txtBin.Text;
                    int length1 = str.LastIndexOf(".");
                    if (length1 >= 0)
                        str = str.Substring(0, length1);

                    if (this.ramReadout != null)
                        File.WriteAllBytes(str + "_RAM.bin", this.ramReadout);

                    if (this.readout == null)
                    {
                        MessageBox.Show("Error reading file!",
                            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        this.changeButtonState(true);
                        this.tsslStatus.Text = "Read failed";
                        return;
                    }

                    if (this.readout.Length == 4 &&
                        ECUProgrammer.checkArrayEq(this.readout, new byte[] { 68, 69, 78, 89 }))
                    {
                        MessageBox.Show("This file cannot be read! An attempt to read and save the RAM was made.",
                            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        this.changeButtonState(true);
                        this.tsslStatus.Text = "ECU denied read (security lock)";
                        return;
                    }

                    int length2 = this.readout.Length;
                    if (this.readout[length2 - 1] != (byte)131 || this.readout[length2 - 2] != (byte)131)
                    {
                        MessageBox.Show("This file is corrupt. Please contact the distributor.",
                            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        this.changeButtonState(true);
                        this.txtBin.Text = "";
                        this.tsslStatus.Text = "Read failed - corrupt data";
                        return;
                    }

                    File.WriteAllBytes(this.txtBin.Text, this.readout);
                    this.changeButtonState(true);
                    this.setReadLatch(true);
                    this.lblTime.Text = "Done Reading!";
                    this.progressFlash.Value = 0;
                    this.tsslStatus.Text = "Read complete - saved to " + Path.GetFileName(this.txtBin.Text);
                }
            }
            ++this.flashTime;
        }

        private void cmdReset_Click(object sender, EventArgs e)
        {
            if (this.eProg != null)
                return;
            this.eProg = new ECUProgrammer(this.dice, null, null, false, this.p80);
            this.eProg.sendReset();
            if (!this.p80)
                new DIMComm(this.dice, false).setTime();
            this.eProg = null;
            this.tsslStatus.Text = "ECU reset sent";
        }

        // =================================================================
        // LOGGING
        // =================================================================

        private void but_chooseparams_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Volvo XML Parameter Files (*.xml)|*.xml";
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            this.txtParamsFile.Text = openFileDialog.FileName;
            this.setECUParams(openFileDialog.FileName);
            this.parseParameters();
            if (this.eVars == null)
                return;
            this.SetComboXML();
        }

        private void parseParameters()
        {
            if (this.txtParamsFile.Text.Equals(""))
            {
                MessageBox.Show("Please choose a parameters file.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            try { this.deserializeXML(); }
            catch (Exception)
            {
                MessageBox.Show("Error in parameters file.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void deserializeXML()
        {
            if (this.eVars == null)
            {
                this.txtParamsFile.Text = "";
                this.setECUParams("");
                MessageBox.Show("Error parsing parameters file!",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            try
            {
                this.eParams = XmlSerializer.ReadObject(this.txtParamsFile.Text);
                this.eVars = this.eParams.ecuVars;
            }
            catch (Exception) { }
        }

        private void SetComboXML()
        {
            try
            {
                this.comboBox_xmlparams.DisplayMember = "var_txt";
                this.comboBox_xmlparams.ValueMember = "var";
                this.comboBox_xmlparams.DataSource = this.eParams.ecuVars.Select(x => new
                {
                    var_txt = x.name,
                    var = x.name
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void cmdChooseLog_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                this.txtLogFile.Text = saveFileDialog.FileName;
            else
                this.txtLogFile.Text = "";

            this.cmdStartLogging.Enabled = !string.IsNullOrEmpty(this.txtLogFile.Text);
        }

        private void cmdStartLogging_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.txtLogFile.Text.Equals(""))
                {
                    MessageBox.Show("Please choose a log file.",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }

                this.logger = new ECULogger(this.dice, this.eParams);
                if (!this.p80)
                    new DIMComm(this.dice, true).sendMessage("Logging...");
                this.parseParameters();
                this.logger.sendReqs();
                this.logFile = new StreamWriter(this.txtLogFile.Text, false);

                if (this.eParams.displayTime)
                    this.logFile.Write("Time (sec),");
                foreach (ECUVariable eVar in (List<ECUVariable>)this.eVars)
                {
                    if (eVar.desc.Equals("") || eVar.units.Equals(""))
                        this.logFile.Write(eVar.name + ",");
                    else
                        this.logFile.Write(eVar.desc + "(" + eVar.units + ") " + eVar.name + ",");
                }
                this.logFile.WriteLine();
                this.logTimer.Start();
                this.startTimer();
                this.changeButtonState(false);
                this.cmdStartLogging.Enabled = false;
                this.cmdStopLogging.Enabled = true;
                ++this.numLogSessions;
                this.tsslStatus.Text = "Logging...";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void cmdStopLogging_Click(object sender, EventArgs e)
        {
            this.dice.sendMsg(new CANPacket(ECULoggingCommands.msgCANRequestRecordSetStop), CANChannel.HS);
            if (this.logger.hs_Logging)
                this.logger.recs_req = false;
            this.logFile.Close();

            string[] strArray = this.txtLogFile.Text.Split(new string[] { ".csv" }, StringSplitOptions.None);
            if (strArray.Length != 0)
                this.txtLogFile.Text = strArray[0] + "_" + this.numLogSessions + ".csv";
            else
                this.txtLogFile.Text = "";

            try
            {
                this.logTimer.Stop();
                this.stopTimer();
                this.lblLogTime.Text = this.getLogTimeSeconds(false) + "s";
                this.logTime = 0L;
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            this.logger.clearReqs();
            this.changeButtonState(true);
            this.cmdStopLogging.Enabled = false;
            this.cmdStartLogging.Enabled = true;
            this.tsslStatus.Text = "Logging stopped";
        }

        private void logTimer_Tick_1(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = this.culture;
            Thread.CurrentThread.CurrentUICulture = this.culture;
            string result = "";
            if (!this.logger.requestRecords(ref result))
                return;

            this.processReqs(result);
            if (this.eParams.displayTime)
                this.logFile.Write(this.getLogTimeSeconds(true) + ",");

            foreach (ECUVariable eVar in (List<ECUVariable>)this.eVars)
            {
                int num = (int)eVar.value;
                if (eVar.signed)
                    num = !eVar.word ? (int)(sbyte)eVar.value : (int)(short)eVar.value;
                double dbValue = (double)num * eVar.factor + eVar.offset;
                eVar.result = this.logger.getDoublePrecision(dbValue, eVar.precision);
                this.logFile.Write(eVar.result + ",");
            }
            this.logFile.WriteLine();
            this.lblLogTime.Text = this.getLogTimeSeconds(false) + "s";

            if (!this.chkshowvitals.Checked)
                return;

            var list1 = this.eVars.Where(x => x.name.Contains("pvdkds_w")).ToList();
            if (list1.Count > 0)
            {
                if (this.chkPSI.Checked)
                {
                    double num = (double.Parse(list1[0].result) - 1000.0) / 68.9475729;
                    this.vitals_boost.Text = this.logger.getDoublePrecision(num > 0.0 ? num : 0.0, list1[0].precision);
                }
                else
                    this.vitals_boost.Text = list1[0].result;
            }

            this.vitals_lambda.Text = this.eVars.Where(x => x.name.Contains("lamsoni_w")).ToList()[0].result;
            var list2 = this.eVars.Where(x => x.name.Contains("wkrm")).ToList();
            if (list2.Count > 0)
                this.vitals_retard.Text = list2[0].result;

            var list4 = this.eVars.Where(x => x.name.Contains("pistnd_w")).ToList();
            if (list4.Count > 0)
                this.vitals_fuelpressure.Text = list4[0].result;

            var list3 = this.eVars.Where(x => x.name.Contains(this.comboBox_xmlparams.SelectedValue.ToString())).ToList();
            if (list3.Count > 0)
                this.vitals_custom.Text = list3[0].result;
        }

        private void comboBox_xmlparams_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.eVars == null)
                return;
            string varVal = this.comboBox_xmlparams.SelectedValue.ToString();
            var list = this.eVars.Where(x => x.name.Contains(varVal)).ToList();
            if (list.Count > 0)
                this.label_custom.Text = list[0].name + " (" + list[0].units + ")";
        }

        public void processReqs(string msgs)
        {
            foreach (ECUVariable eVar in (List<ECUVariable>)this.eVars)
            {
                try
                {
                    if (eVar.word)
                    {
                        string input = msgs.Substring(0, 4);
                        msgs = msgs.Substring(4);
                        eVar.value = eVar.getHexValueFromString(input);
                    }
                    else
                    {
                        string input = msgs.Substring(0, 2);
                        msgs = msgs.Substring(2);
                        eVar.value = eVar.getHexValueFromString(input);
                    }
                }
                catch (Exception)
                {
                    eVar.value = 0;
                }
            }
        }

        // =================================================================
        // SERVICE RESET
        // =================================================================

        private void resetServiceReminderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.dice.DeviceOpen)
            {
                MessageBox.Show("Please connect the DiCE first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            if (MessageBox.Show("Would you like to reset the SRI?",
                    Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (MessageBox.Show("Is the key in pos II with the engine NOT running?",
                    Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            this.eProg = new ECUProgrammer(this.dice, null, null, this.flash512, this.p80);
            if (!new DIMComm(this.dice, false).resetSRI())
            {
                MessageBox.Show("Failed to reset the service indicator!",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else
            {
                MessageBox.Show("Service indicator reset!",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        // =================================================================
        // ABOUT / CREDITS
        // =================================================================

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string nl = Environment.NewLine;
            MessageBox.Show(
                Application.ProductName + " v" + Application.ProductVersion + nl + nl +
                "CAN Toolbox and Flashing Suite for Volvo ME7 ECUs." + nl + nl +
                "This project is open-source and free to use and modify," + nl +
                "so long as you give credit where credit is due." + nl + nl +
                "FREE THE MOOSE!",
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void label2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Shift)
            {
                string nl = Environment.NewLine;
                MessageBox.Show(
                    Application.ProductName + " was developed by the Volvo enthusiast community, " +
                    "for the Volvo enthusiast community." + nl + nl +
                    "This application includes code originally developed by Dream3R, which was " +
                    "greedily stolen and profited from, then stolen again and given back to the " +
                    "Volvo enthusiast community." + nl + nl +
                    "Special thanks to the ME7 enthusiasts at NefariousMotorSports including " +
                    "Dream3R (RIP) for discovering how to flash over DiCE, s60rawr for securing " +
                    "this version of the code, rlinewiz for debugging and programming, and the " +
                    "patrons in the Volvo ME7 Thread for testing and feedback." + nl + nl +
                    "This project is open-source and free to use and modify, so long as you " +
                    "give credit where credit is due." + nl + nl + "FREE THE MOOSE!",
                    Application.ProductName, MessageBoxButtons.OK);
            }
        }

        // =================================================================
        // UTILITY
        // =================================================================

        private void changeButtonState(bool setEnabled)
        {
            this.cmdFlash.Enabled = setEnabled;
            this.cmdRead.Enabled = setEnabled;
            this.cmdReset.Enabled = setEnabled;
            this.cmdStartLogging.Enabled = setEnabled;
            this.cmdStopLogging.Enabled = setEnabled;
            this.resetServiceReminderToolStripMenuItem.Enabled = setEnabled;
        }

        private string getECUParams()
        {
            this.regedit.BaseRegistryKey = Registry.CurrentUser;
            this.regedit.SubKey = this.APP_KEY;
            return this.regedit.Read("ECUParams");
        }

        private bool setECUParams(string ecuparams)
        {
            this.regedit.BaseRegistryKey = Registry.CurrentUser;
            this.regedit.SubKey = this.APP_KEY;
            return this.regedit.Write("ECUParams", ecuparams);
        }

        private bool getReadLatch()
        {
            this.regedit.BaseRegistryKey = Registry.CurrentUser;
            this.regedit.SubKey = this.APP_KEY;
            try { return bool.Parse(this.regedit.Read("ReadLatch")); }
            catch { return false; }
        }

        private bool setReadLatch(bool readOut)
        {
            this.regedit.BaseRegistryKey = Registry.CurrentUser;
            this.regedit.SubKey = this.APP_KEY;
            return this.regedit.Write("ReadLatch", readOut.ToString());
        }

        private void startTimer()
        {
            this.StateObj = new StateObjClass();
            this.StateObj.TimerCanceled = false;
            this.StateObj.lf = this.logFile;
            this.StateObj.TimerReference = new System.Threading.Timer(
                new TimerCallback(this.TimerTask), this.StateObj, 0, 100);
        }

        private void stopTimer()
        {
            try { this.StateObj.TimerCanceled = true; }
            catch (Exception) { }
        }

        private void TimerTask(object StateObj)
        {
            var stateObjClass = (StateObjClass)StateObj;
            Interlocked.Increment(ref this.logTime);
            if (stateObjClass.TimerCanceled)
                stateObjClass.TimerReference.Dispose();
        }

        private string getLogTimeSeconds(bool withMillis)
        {
            if (withMillis)
                return ((double)this.logTime / 10.0).ToString("0.000");
            return (this.logTime / 10L).ToString();
        }

        private List<CANPacket> createSBLList()
        {
            var canPacketList = new List<CANPacket>();
            int num1 = 0;
            while (num1 < VolvoECUCommands.sbl.Length)
            {
                CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
                byte[] mData = new byte[6];
                int num2 = VolvoECUCommands.sbl.Length - num1;
                for (int index = 0; index < 6; ++index)
                    mData[index] = num2 < index + 1 ? (byte)0 : VolvoECUCommands.sbl[num1 + index];
                canPacket.setMsgData(mData);
                canPacketList.Add(canPacket);
                num1 += 6;
            }
            return canPacketList;
        }

        private List<CANPacket> createData8kList()
        {
            var canPacketList = new List<CANPacket>();
            int num1 = 32768;
            while (num1 < 57344)
            {
                CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
                byte[] mData = new byte[6];
                int num2 = this.binFile.Length - num1;
                for (int index = 0; index < 6; ++index)
                    mData[index] = num2 < index + 1 ? (byte)0 : this.binFile[num1 + index];
                canPacket.setMsgData(mData);
                canPacketList.Add(canPacket);
                num1 += 6;
            }
            return canPacketList;
        }

        private List<CANPacket> createData10kList()
        {
            var canPacketList = new List<CANPacket>();
            int num1 = 65536;
            while (num1 < 1048576)
            {
                CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
                byte[] mData = new byte[6];
                int num2 = this.binFile.Length - num1;
                for (int index = 0; index < 6; ++index)
                    mData[index] = num2 < index + 1 ? (byte)0 : this.binFile[num1 + index];
                canPacket.setMsgData(mData);
                canPacketList.Add(canPacket);
                num1 += 6;
            }
            return canPacketList;
        }

        public int getHexValueFromString(string input)
        {
            if (input.Contains('x'))
                input = input.Split('x')[1];
            if (input.Length == 2)
                return (int)this.getAddressFromString(input)[0];
            if (input.Length == 4)
            {
                byte[] addr = this.getAddressFromString(input);
                return addr[1] + (ushort)(addr[0] * 256U);
            }
            if (input.Length != 8)
                throw new Exception();
            byte[] addr1 = this.getAddressFromString(input);
            return addr1[3] + addr1[2] * 256 + addr1[1] * 65536 + addr1[0] * 16777216;
        }

        private byte[] getAddressFromString(string input)
        {
            if (input.Contains('x'))
                input = input.Split('x')[1];
            byte[] numArray = new byte[input.Length / 2];
            char[] charArray = input.ToCharArray();
            int index = 0;
            while (index < charArray.Length)
            {
                numArray[index / 2] = (byte)(16U * getByteFromChar(charArray[index]));
                numArray[index / 2] += getByteFromChar(charArray[index + 1]);
                index += 2;
            }
            return numArray;
        }

        private static byte getByteFromChar(char c) => c switch
        {
            >= '0' and <= '9' => (byte)(c - '0'),
            'A' or 'a' => 10,
            'B' or 'b' => 11,
            'C' or 'c' => 12,
            'D' or 'd' => 13,
            'E' or 'e' => 14,
            'F' or 'f' => 15,
            _ => 0,
        };

        private class StateObjClass
        {
            public StreamWriter lf;
            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;
        }
    }
}
