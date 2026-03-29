using System.Drawing;
using System.Windows.Forms;

namespace J2534
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Timer flashTimer;
        private System.Windows.Forms.Timer readTimer;
        // logTimer removed — logging now uses a background thread

        private MenuStrip menuMain;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem resetServiceReminderToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;

        private Panel pnlConnection;
        private Label lblDevice;
        private ComboBox comboBox_J2534_devices;
        private Button cmdDetectDevices;
        private Button but_connectdice;
        private Label lblConnectionStatus;

        private TabControl tabControl1;

        private TabPage tabPage1;
        private Label lblFlashFile;
        private TextBox txtBin;
        private Button cmdFlash;
        private Button cmdRead;
        private ProgressBar progressFlash;
        private Label lblTime;
        private Button cmdReset;
        private Label label4;
        private Label label2;

        private TabPage tabPage2;
        private Label lblParamsLabel;
        private TextBox txtParamsFile;
        private Button but_chooseparams;
        private Label lblLogLabel;
        private TextBox txtLogFile;
        private Button cmdChooseLog;
        private ComboBox comboBox_xmlparams;
        private CheckBox chkPSI;
        private CheckBox chkshowvitals;
        private Button cmdStartLogging;
        private Button cmdStopLogging;
        private Label lblLogTime;
        private GroupBox groupBox1;
        private Label label1;
        private Label vitals_boost;
        private Label label3;
        private Label vitals_lambda;
        private Label label5;
        private Label vitals_retard;
        private Label label6;
        private Label vitals_fuelpressure;
        private Label label_custom;
        private Label vitals_custom;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel tsslStatus;
        private ToolStripStatusLabel tsslVersion;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));

            // --- Accent colors for manual overrides ---
            var accentColor = Color.FromArgb(218, 165, 32);
            var accentDark = Color.FromArgb(170, 128, 25);
            var dangerColor = Color.FromArgb(200, 60, 50);
            var dangerDark = Color.FromArgb(160, 45, 35);
            var successColor = Color.FromArgb(72, 160, 72);
            var surfaceColor = Color.FromArgb(38, 38, 50);
            var borderColor = Color.FromArgb(60, 60, 75);

            // =================================================================
            // TIMERS
            // =================================================================
            this.flashTimer = new System.Windows.Forms.Timer(this.components);
            this.flashTimer.Interval = 1000;
            this.flashTimer.Tick += new System.EventHandler(this.flashTimer_Tick);

            this.readTimer = new System.Windows.Forms.Timer(this.components);
            this.readTimer.Interval = 1000;
            this.readTimer.Tick += new System.EventHandler(this.readTimer_Tick);

            // =================================================================
            // MENU STRIP
            // =================================================================
            this.menuMain = new MenuStrip();
            this.fileToolStripMenuItem = new ToolStripMenuItem();
            this.exitToolStripMenuItem = new ToolStripMenuItem();
            this.toolsToolStripMenuItem = new ToolStripMenuItem();
            this.resetServiceReminderToolStripMenuItem = new ToolStripMenuItem();
            this.helpToolStripMenuItem = new ToolStripMenuItem();
            this.aboutToolStripMenuItem = new ToolStripMenuItem();

            this.menuMain.Items.AddRange(new ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.toolsToolStripMenuItem,
                this.helpToolStripMenuItem
            });
            this.menuMain.Dock = DockStyle.Top;
            this.menuMain.Padding = new Padding(8, 2, 0, 2);

            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.DropDownItems.Add(this.exitToolStripMenuItem);

            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += (s, e) => this.Close();

            this.toolsToolStripMenuItem.Text = "&Tools";
            this.toolsToolStripMenuItem.DropDownItems.Add(this.resetServiceReminderToolStripMenuItem);

            this.resetServiceReminderToolStripMenuItem.Text = "Reset Service Reminder...";
            this.resetServiceReminderToolStripMenuItem.Enabled = false;
            this.resetServiceReminderToolStripMenuItem.Click += new System.EventHandler(this.resetServiceReminderToolStripMenuItem_Click);

            this.helpToolStripMenuItem.Text = "&Help";
            this.helpToolStripMenuItem.DropDownItems.Add(this.aboutToolStripMenuItem);

            this.aboutToolStripMenuItem.Text = "&About OpenMoose";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);

            // =================================================================
            // CONNECTION PANEL
            // =================================================================
            this.pnlConnection = new Panel();
            this.lblDevice = new Label();
            this.comboBox_J2534_devices = new ComboBox();
            this.cmdDetectDevices = new Button();
            this.but_connectdice = new Button();
            this.lblConnectionStatus = new Label();

            this.pnlConnection.Dock = DockStyle.Top;
            this.pnlConnection.Height = 52;
            this.pnlConnection.Padding = new Padding(12, 8, 12, 8);

            this.lblDevice.Text = "J2534 Device:";
            this.lblDevice.AutoSize = true;
            this.lblDevice.Location = new Point(14, 17);

            this.comboBox_J2534_devices.Location = new Point(110, 13);
            this.comboBox_J2534_devices.Size = new Size(340, 24);
            this.comboBox_J2534_devices.DropDownStyle = ComboBoxStyle.DropDownList;

            this.cmdDetectDevices.Text = "Refresh";
            this.cmdDetectDevices.Location = new Point(460, 12);
            this.cmdDetectDevices.Size = new Size(80, 26);
            this.cmdDetectDevices.FlatStyle = FlatStyle.Flat;
            this.cmdDetectDevices.FlatAppearance.BorderColor = borderColor;
            this.cmdDetectDevices.Click += new System.EventHandler(this.cmdDetectDevices_Click);

            this.but_connectdice.Text = "Connect";
            this.but_connectdice.Location = new Point(550, 12);
            this.but_connectdice.Size = new Size(90, 26);
            this.but_connectdice.FlatStyle = FlatStyle.Flat;
            this.but_connectdice.BackColor = accentDark;
            this.but_connectdice.ForeColor = Color.Black;
            this.but_connectdice.FlatAppearance.BorderColor = accentColor;
            this.but_connectdice.Click += new System.EventHandler(this.but_connectdice_Click);

            this.lblConnectionStatus.Text = "  Disconnected";
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Location = new Point(656, 17);
            this.lblConnectionStatus.ForeColor = Color.FromArgb(160, 80, 80);

            this.pnlConnection.Controls.AddRange(new Control[] {
                this.lblDevice,
                this.comboBox_J2534_devices,
                this.cmdDetectDevices,
                this.but_connectdice,
                this.lblConnectionStatus
            });

            // =================================================================
            // TAB CONTROL
            // =================================================================
            this.tabControl1 = new TabControl();
            this.tabPage1 = new TabPage();
            this.tabPage2 = new TabPage();

            this.tabControl1.Dock = DockStyle.Fill;
            this.tabControl1.Padding = new Point(12, 6);
            this.tabControl1.TabPages.Add(this.tabPage1);
            this.tabControl1.TabPages.Add(this.tabPage2);

            // =================================================================
            // TAB 1: FLASH
            // =================================================================
            this.tabPage1.Text = "  Flash  ";
            this.tabPage1.Padding = new Padding(16, 12, 16, 12);

            this.lblFlashFile = new Label();
            this.txtBin = new TextBox();
            this.cmdFlash = new Button();
            this.cmdRead = new Button();
            this.progressFlash = new ProgressBar();
            this.lblTime = new Label();
            this.cmdReset = new Button();
            this.label4 = new Label();
            this.label2 = new Label();

            // File path
            this.lblFlashFile = new Label();
            this.lblFlashFile.Text = "BIN File:";
            this.lblFlashFile.AutoSize = true;
            this.lblFlashFile.Location = new Point(18, 18);

            this.txtBin.Location = new Point(80, 14);
            this.txtBin.Size = new Size(540, 24);
            this.txtBin.ReadOnly = true;

            // Action buttons
            this.cmdFlash.Text = "Flash ECU";
            this.cmdFlash.Location = new Point(640, 12);
            this.cmdFlash.Size = new Size(130, 30);
            this.cmdFlash.FlatStyle = FlatStyle.Flat;
            this.cmdFlash.BackColor = accentDark;
            this.cmdFlash.ForeColor = Color.Black;
            this.cmdFlash.FlatAppearance.BorderColor = accentColor;
            this.cmdFlash.Enabled = false;
            this.cmdFlash.Click += new System.EventHandler(this.cmdFlash_Click);

            this.cmdRead.Text = "Read ECU";
            this.cmdRead.Location = new Point(780, 12);
            this.cmdRead.Size = new Size(130, 30);
            this.cmdRead.FlatStyle = FlatStyle.Flat;
            this.cmdRead.FlatAppearance.BorderColor = borderColor;
            this.cmdRead.Enabled = false;
            this.cmdRead.Click += new System.EventHandler(this.cmdRead_Click);

            // Progress bar
            this.progressFlash.Location = new Point(18, 52);
            this.progressFlash.Size = new Size(892, 12);
            this.progressFlash.Style = ProgressBarStyle.Continuous;
            this.progressFlash.Maximum = 168907;

            // Time label
            this.lblTime.Text = "";
            this.lblTime.AutoSize = true;
            this.lblTime.Location = new Point(18, 72);

            // Emergency Reset
            this.cmdReset.Text = "Emergency Reset";
            this.cmdReset.Location = new Point(760, 90);
            this.cmdReset.Size = new Size(150, 32);
            this.cmdReset.FlatStyle = FlatStyle.Flat;
            this.cmdReset.BackColor = dangerDark;
            this.cmdReset.ForeColor = Color.FromArgb(230, 200, 200);
            this.cmdReset.FlatAppearance.BorderColor = dangerColor;
            this.cmdReset.Enabled = false;
            this.cmdReset.Click += new System.EventHandler(this.cmdReset_Click);

            // Tips text
            this.label4.AutoSize = true;
            this.label4.Location = new Point(18, 135);
            this.label4.MaximumSize = new Size(700, 0);
            this.label4.ForeColor = Color.FromArgb(140, 140, 155);
            this.label4.Text = resources.GetString("label4.Text");

            // Credits
            this.label2.Text = "RIP Dream3R";
            this.label2.AutoSize = true;
            this.label2.ForeColor = Color.FromArgb(90, 90, 105);
            this.label2.Location = new Point(830, 310);
            this.label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.label2.Cursor = Cursors.Hand;
            this.label2.MouseDown += new MouseEventHandler(this.label2_MouseDown);

            this.tabPage1.Controls.AddRange(new Control[] {
                this.lblFlashFile, this.txtBin, this.cmdFlash, this.cmdRead,
                this.progressFlash, this.lblTime, this.cmdReset,
                this.label4, this.label2
            });

            // =================================================================
            // TAB 2: DATALOG
            // =================================================================
            this.tabPage2.Text = "  Datalog  ";
            this.tabPage2.Padding = new Padding(16, 12, 16, 12);

            this.lblParamsLabel = new Label();
            this.txtParamsFile = new TextBox();
            this.but_chooseparams = new Button();
            this.lblLogLabel = new Label();
            this.txtLogFile = new TextBox();
            this.cmdChooseLog = new Button();
            this.comboBox_xmlparams = new ComboBox();
            this.chkPSI = new CheckBox();
            this.chkshowvitals = new CheckBox();
            this.cmdStartLogging = new Button();
            this.cmdStopLogging = new Button();
            this.lblLogTime = new Label();

            // Params row
            this.lblParamsLabel = new Label();
            this.lblParamsLabel.Text = "Parameters:";
            this.lblParamsLabel.AutoSize = true;
            this.lblParamsLabel.Location = new Point(18, 18);

            this.txtParamsFile.Location = new Point(105, 14);
            this.txtParamsFile.Size = new Size(440, 24);
            this.txtParamsFile.ReadOnly = true;

            this.but_chooseparams.Text = "Load Params";
            this.but_chooseparams.Location = new Point(555, 12);
            this.but_chooseparams.Size = new Size(110, 26);
            this.but_chooseparams.FlatStyle = FlatStyle.Flat;
            this.but_chooseparams.FlatAppearance.BorderColor = borderColor;
            this.but_chooseparams.Click += new System.EventHandler(this.but_chooseparams_Click);

            // Log file row
            this.lblLogLabel = new Label();
            this.lblLogLabel.Text = "Log File:";
            this.lblLogLabel.AutoSize = true;
            this.lblLogLabel.Location = new Point(18, 50);

            this.txtLogFile.Location = new Point(105, 46);
            this.txtLogFile.Size = new Size(440, 24);
            this.txtLogFile.ReadOnly = true;

            this.cmdChooseLog.Text = "Save Location";
            this.cmdChooseLog.Location = new Point(555, 44);
            this.cmdChooseLog.Size = new Size(110, 26);
            this.cmdChooseLog.FlatStyle = FlatStyle.Flat;
            this.cmdChooseLog.FlatAppearance.BorderColor = borderColor;
            this.cmdChooseLog.Click += new System.EventHandler(this.cmdChooseLog_Click);

            // Options row
            this.comboBox_xmlparams.Location = new Point(18, 82);
            this.comboBox_xmlparams.Size = new Size(280, 24);
            this.comboBox_xmlparams.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox_xmlparams.SelectedIndexChanged += new System.EventHandler(this.comboBox_xmlparams_SelectedIndexChanged);

            this.chkshowvitals.Text = "Show Vitals";
            this.chkshowvitals.AutoSize = true;
            this.chkshowvitals.Location = new Point(314, 84);

            this.chkPSI.Text = "Boost PSI";
            this.chkPSI.AutoSize = true;
            this.chkPSI.Location = new Point(420, 84);

            // Logging controls
            this.cmdStartLogging.Text = "Start Logging";
            this.cmdStartLogging.Location = new Point(555, 80);
            this.cmdStartLogging.Size = new Size(110, 26);
            this.cmdStartLogging.FlatStyle = FlatStyle.Flat;
            this.cmdStartLogging.BackColor = successColor;
            this.cmdStartLogging.ForeColor = Color.Black;
            this.cmdStartLogging.FlatAppearance.BorderColor = successColor;
            this.cmdStartLogging.Enabled = false;
            this.cmdStartLogging.Click += new System.EventHandler(this.cmdStartLogging_Click);

            this.cmdStopLogging.Text = "Stop Logging";
            this.cmdStopLogging.Location = new Point(675, 80);
            this.cmdStopLogging.Size = new Size(110, 26);
            this.cmdStopLogging.FlatStyle = FlatStyle.Flat;
            this.cmdStopLogging.BackColor = dangerDark;
            this.cmdStopLogging.ForeColor = Color.FromArgb(230, 200, 200);
            this.cmdStopLogging.FlatAppearance.BorderColor = dangerColor;
            this.cmdStopLogging.Enabled = false;
            this.cmdStopLogging.Click += new System.EventHandler(this.cmdStopLogging_Click);

            this.lblLogTime.Text = "";
            this.lblLogTime.AutoSize = true;
            this.lblLogTime.Location = new Point(800, 86);

            // Vitals group
            this.groupBox1 = new GroupBox();
            this.label1 = new Label();
            this.vitals_boost = new Label();
            this.label3 = new Label();
            this.vitals_lambda = new Label();
            this.label5 = new Label();
            this.vitals_retard = new Label();
            this.label6 = new Label();
            this.vitals_fuelpressure = new Label();
            this.label_custom = new Label();
            this.vitals_custom = new Label();

            this.groupBox1.Text = "Live Vitals";
            this.groupBox1.Location = new Point(18, 118);
            this.groupBox1.Size = new Size(892, 100);

            int colWidth = 170;
            int labelY = 24;
            int valueY = 50;
            var valueFont = new Font("Segoe UI", 14f, FontStyle.Bold);
            var valueColor = accentColor;

            // Boost
            this.label1.Text = "Boost";
            this.label1.AutoSize = true;
            this.label1.Location = new Point(20, labelY);

            this.vitals_boost.Text = "-";
            this.vitals_boost.AutoSize = true;
            this.vitals_boost.Font = valueFont;
            this.vitals_boost.ForeColor = valueColor;
            this.vitals_boost.Location = new Point(20, valueY);

            // Lambda
            this.label3.Text = "Lambda";
            this.label3.AutoSize = true;
            this.label3.Location = new Point(20 + colWidth, labelY);

            this.vitals_lambda.Text = "-";
            this.vitals_lambda.AutoSize = true;
            this.vitals_lambda.Font = valueFont;
            this.vitals_lambda.ForeColor = valueColor;
            this.vitals_lambda.Location = new Point(20 + colWidth, valueY);

            // Ign Retard
            this.label5.Text = "Ign Retard";
            this.label5.AutoSize = true;
            this.label5.Location = new Point(20 + colWidth * 2, labelY);

            this.vitals_retard.Text = "-";
            this.vitals_retard.AutoSize = true;
            this.vitals_retard.Font = valueFont;
            this.vitals_retard.ForeColor = valueColor;
            this.vitals_retard.Location = new Point(20 + colWidth * 2, valueY);

            // Fuel Pressure
            this.label6.Text = "Fuel Pressure";
            this.label6.AutoSize = true;
            this.label6.Location = new Point(20 + colWidth * 3, labelY);

            this.vitals_fuelpressure.Text = "-";
            this.vitals_fuelpressure.AutoSize = true;
            this.vitals_fuelpressure.Font = valueFont;
            this.vitals_fuelpressure.ForeColor = valueColor;
            this.vitals_fuelpressure.Location = new Point(20 + colWidth * 3, valueY);

            // Custom
            this.label_custom.Text = "Select Parameter";
            this.label_custom.AutoSize = true;
            this.label_custom.Location = new Point(20 + colWidth * 4, labelY);

            this.vitals_custom.Text = "-";
            this.vitals_custom.AutoSize = true;
            this.vitals_custom.Font = valueFont;
            this.vitals_custom.ForeColor = valueColor;
            this.vitals_custom.Location = new Point(20 + colWidth * 4, valueY);

            this.groupBox1.Controls.AddRange(new Control[] {
                this.label1, this.vitals_boost,
                this.label3, this.vitals_lambda,
                this.label5, this.vitals_retard,
                this.label6, this.vitals_fuelpressure,
                this.label_custom, this.vitals_custom
            });

            this.tabPage2.Controls.AddRange(new Control[] {
                this.lblParamsLabel, this.txtParamsFile, this.but_chooseparams,
                this.lblLogLabel, this.txtLogFile, this.cmdChooseLog,
                this.comboBox_xmlparams, this.chkshowvitals, this.chkPSI,
                this.cmdStartLogging, this.cmdStopLogging, this.lblLogTime,
                this.groupBox1
            });

            // =================================================================
            // STATUS STRIP
            // =================================================================
            this.statusStrip = new StatusStrip();
            this.tsslStatus = new ToolStripStatusLabel();
            this.tsslVersion = new ToolStripStatusLabel();

            this.tsslStatus.Text = "Ready";
            this.tsslStatus.Spring = true;
            this.tsslStatus.TextAlign = ContentAlignment.MiddleLeft;

            this.tsslVersion.Text = "v" + Application.ProductVersion;
            this.tsslVersion.Alignment = ToolStripItemAlignment.Right;

            this.statusStrip.Items.AddRange(new ToolStripItem[] {
                this.tsslStatus, this.tsslVersion
            });

            // =================================================================
            // FORM
            // =================================================================
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new Size(960, 520);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "OpenMoose REDUX";
            this.Icon = (Icon)resources.GetObject("$this.Icon");
            this.MainMenuStrip = this.menuMain;

            // Add controls in correct Z-order for docking
            this.Controls.Add(this.tabControl1);      // Fill (center)
            this.Controls.Add(this.pnlConnection);    // Top (below menu)
            this.Controls.Add(this.menuMain);          // Top (first)
            this.Controls.Add(this.statusStrip);       // Bottom

            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
