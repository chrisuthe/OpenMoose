# Fix Broken ME7 ECU Datalogging — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all bugs preventing ME7 ECU datalogging from working and make the logging pipeline robust with proper error handling, timeouts, and background threading.

**Architecture:** The protocol bytes are correct (verified against VolvoTools). All bugs are in orchestration: infinite loops, UI-thread blocking, stale references, missing error handling. We fix ECULogger to have retry limits and proper response parsing, fix ToolComm memory leak, then rewrite the frmMain logging lifecycle to use a background thread with 50ms polling (matching VolvoTools' proven approach). Dead DRM code is removed.

**Tech Stack:** C# / .NET 10 / WinForms / J2534 PassThru API

---

### Task 1: Fix ECULogger — sendReqs() infinite loop and clearReqs() robustness

**Files:**
- Modify: `src/J2534/J2534.Logging/ECULogger.cs:129-146`

- [ ] **Step 1: Fix sendReqs() — add retry limit of 10 per variable**

Replace the `while (!flag)` infinite loop with a bounded retry. Throw a descriptive exception if registration fails after retries. Also fix `clearReqs()` to retry.

```csharp
public void sendReqs()
{
    clearReqs();
    foreach (ECUVariable ecuVar in ecuParams.ecuVars)
    {
        CANPacket cANMsg = new CANPacket(ecuVar.getRequestData());
        bool ack = false;
        for (int attempt = 0; attempt < 10 && !ack; attempt++)
        {
            ack = dice.sendMsgCheckDiagResponse(cANMsg, CANChannel.HS, 234);
        }
        if (!ack)
            throw new InvalidOperationException(
                $"ECU did not acknowledge registration of variable '{ecuVar.name}' after 10 attempts.");
    }
}

public bool clearReqs()
{
    for (int attempt = 0; attempt < 5; attempt++)
    {
        if (dice.sendMsgCheckDiagResponse(ECULoggingCommands.msgCANClearRecordReqs, CANChannel.HS, 234))
            return true;
    }
    return false;
}
```

- [ ] **Step 2: Build and verify compilation**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
fix: add retry limits to ECULogger.sendReqs() and clearReqs()
```

---

### Task 2: Fix ECULogger — requestRecords() response parsing

**Files:**
- Modify: `src/J2534/J2534.Logging/ECULogger.cs:183-207`

- [ ] **Step 1: Fix Split()[1] IndexOutOfRangeException**

The `Split("7AE6F000")[1]` throws if the marker is absent. Add bounds checking.

```csharp
public bool requestRecords(ref string result)
{
    CANPacket cANMsg = new CANPacket(ECULoggingCommands.msgCANRequestRecordSetOnce);
    uint maxNumMsgs = getNumMsgs();
    uint expectedMsgs = maxNumMsgs;
    uint timeout = 1000u;
    List<CANPacket> cl = dice.sendMsgReadResponse(cANMsg, CANChannel.HS, ref maxNumMsgs, timeout);

    if (cl.Count != expectedMsgs)
    {
        result = "";
        return false;
    }

    if (!VerifyLogList(ref cl, expectedMsgs, 8))
        return false;

    string raw = "";
    foreach (CANPacket item in cl)
        raw += item.getLoggingDataString();

    string[] parts = raw.Split(new[] { "7AE6F000" }, StringSplitOptions.None);
    if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
        return false;

    result = parts[1];
    return true;
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build`

- [ ] **Step 3: Commit**

```
fix: add bounds checking to ECULogger.requestRecords() response parsing
```

---

### Task 3: Fix ToolComm memory leak

**Files:**
- Modify: `src/J2534/J2534/ToolComm.cs:115-131`

- [ ] **Step 1: Fix sendMsgCheckDiagResponse — free IntPtr on success path**

The `return true` at line 127 exits before `Marshal.FreeHGlobal`. Use try/finally.

```csharp
public bool sendMsgCheckDiagResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte, ref uint numMsgs)
{
    uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
    uint timeout = 900u;
    sendMsgHelper(CANMsg, channelType, clearRXBuffer: true, 900u);
    IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * numMsgs));
    try
    {
        dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref numMsgs, timeout);
        J2534Message j2534Message = new J2534Message(intPtr, (int)numMsgs, (int)(numMsgs + 1));
        for (int i = 0; i < numMsgs; i++)
        {
            if (new CANPacket(j2534Message[i]).getDiagResponseByte() == responseByte)
                return true;
        }
        return false;
    }
    finally
    {
        Marshal.FreeHGlobal(intPtr);
    }
}
```

Also fix the single-numMsgs overload at lines 85-106 (same bug):

```csharp
public bool sendMsgCheckResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte)
{
    // ... same try/finally pattern
}
```

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Commit**

```
fix: prevent unmanaged memory leak in ToolComm.sendMsgCheckDiagResponse
```

---

### Task 4: Remove dead DRM code and duplicate XmlSerializer

**Files:**
- Modify: `src/J2534/J2534.Logging/ECULogger.cs` — remove `checkParamsValid()` and `getNetworkTime()`
- Delete: `src/J2534/J2534.Logging/ParametersExpiredException.cs`
- Delete: `src/J2534/J2534.Logging/XmlSerializer.cs`
- Modify: `frmMain.cs` — change `XmlSerializer.ReadObject` call to `ECUParameters.ReadObject`

- [ ] **Step 1: Remove dead methods from ECULogger.cs**

Remove lines 108-127 (`checkParamsValid` and `getNetworkTime`). Remove `using System.Net;`, `using System.Net.Sockets;` if no longer needed.

- [ ] **Step 2: Delete ParametersExpiredException.cs and XmlSerializer.cs**

- [ ] **Step 3: Update frmMain.cs deserializeXML()**

Change:
```csharp
this.eParams = XmlSerializer.ReadObject(this.txtParamsFile.Text);
```
To:
```csharp
this.eParams = ECUParameters.ReadObject(this.txtParamsFile.Text);
```

- [ ] **Step 4: Build and verify**

- [ ] **Step 5: Commit**

```
chore: remove dead DRM code and duplicate XmlSerializer
```

---

### Task 5: Rewrite frmMain.cs logging lifecycle — background thread

**Files:**
- Modify: `frmMain.cs` — rewrite `cmdStartLogging_Click`, `cmdStopLogging_Click`, add background logging method, remove `logTimer_Tick_1`

This is the big change. The current design runs blocking CAN I/O on a 1ms WinForms timer (UI thread). We replace it with:
1. Parse parameters FIRST, then construct ECULogger
2. Run data collection on a background `Thread` with 50ms polling
3. Use `Control.Invoke` to update UI from the background thread
4. Track consecutive errors; abort after 10

- [ ] **Step 1: Add logging state fields**

Add to the field declarations in frmMain.cs:

```csharp
private Thread loggingThread;
private volatile bool loggingActive;
private readonly object flashLock = new object();
```

- [ ] **Step 2: Rewrite cmdStartLogging_Click**

Key change: parse params BEFORE constructing ECULogger. Start a background thread instead of the logTimer.

```csharp
private void cmdStartLogging_Click(object sender, EventArgs e)
{
    try
    {
        if (string.IsNullOrEmpty(this.txtLogFile.Text))
        {
            MessageBox.Show("Please choose a log file.",
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }

        // Parse parameters FIRST (bug fix: was after ECULogger construction)
        this.parseParameters();
        if (this.eVars == null || this.eVars.Count == 0)
        {
            MessageBox.Show("No valid parameters loaded.",
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }

        // Construct logger with current (just-parsed) params
        this.logger = new ECULogger(this.dice, this.eParams);
        if (!this.p80)
            new DIMComm(this.dice, true).sendMessage("Logging...");

        // Register variables with ECU
        try
        {
            this.logger.sendReqs();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show("Failed to register logging variables:\n" + ex.Message,
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }

        // Open log file and write CSV header
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

        // Start background logging thread
        this.loggingActive = true;
        this.logTime = 0L;
        this.startTimer();
        this.changeButtonState(false);
        this.cmdStartLogging.Enabled = false;
        this.cmdStopLogging.Enabled = true;
        ++this.numLogSessions;
        this.tsslStatus.Text = "Logging...";

        this.loggingThread = new Thread(LoggingWorker);
        this.loggingThread.IsBackground = true;
        this.loggingThread.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show("Error starting logging: " + ex.Message,
            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
}
```

- [ ] **Step 3: Write the LoggingWorker background method**

This runs on a background thread, polls the ECU at ~50ms, writes to CSV, and uses Invoke for UI updates. Has error counting with 10-error abort threshold.

```csharp
private void LoggingWorker()
{
    int consecutiveErrors = 0;
    const int maxErrors = 10;

    while (this.loggingActive && consecutiveErrors < maxErrors)
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = this.culture;
            string result = "";
            if (!this.logger.requestRecords(ref result))
            {
                consecutiveErrors++;
                Thread.Sleep(50);
                continue;
            }

            consecutiveErrors = 0; // reset on success
            this.processReqs(result);

            // Write to CSV
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

            // Update UI via Invoke (thread-safe)
            this.Invoke((Action)UpdateLoggingUI);
        }
        catch (Exception ex)
        {
            consecutiveErrors++;
            Console.WriteLine("Logging error: " + ex.Message);
        }

        Thread.Sleep(50); // ~20Hz polling, matching VolvoTools
    }

    // If we exited due to errors, update UI
    if (consecutiveErrors >= maxErrors)
    {
        try
        {
            this.Invoke((Action)(() =>
            {
                this.tsslStatus.Text = "Logging stopped: too many consecutive errors";
                StopLoggingCleanup();
            }));
        }
        catch (ObjectDisposedException) { }
    }
}

private void UpdateLoggingUI()
{
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

    var lambdaList = this.eVars.Where(x => x.name.Contains("lamsoni_w")).ToList();
    if (lambdaList.Count > 0)
        this.vitals_lambda.Text = lambdaList[0].result;

    var retardList = this.eVars.Where(x => x.name.Contains("wkrm")).ToList();
    if (retardList.Count > 0)
        this.vitals_retard.Text = retardList[0].result;

    // BUG FIX: was checking list2.Count (retard) instead of list4.Count (fuel pressure)
    var fuelList = this.eVars.Where(x => x.name.Contains("pistnd_w")).ToList();
    if (fuelList.Count > 0)
        this.vitals_fuelpressure.Text = fuelList[0].result;

    var customList = this.eVars.Where(x => x.name.Contains(this.comboBox_xmlparams.SelectedValue?.ToString() ?? "")).ToList();
    if (customList.Count > 0)
        this.vitals_custom.Text = customList[0].result;
}
```

- [ ] **Step 4: Rewrite cmdStopLogging_Click**

Add null checks, use the shared cleanup method.

```csharp
private void cmdStopLogging_Click(object sender, EventArgs e)
{
    this.loggingActive = false;
    this.loggingThread?.Join(2000); // wait up to 2s for thread to finish

    try
    {
        this.dice.sendMsg(new CANPacket(ECULoggingCommands.msgCANRequestRecordSetStop), CANChannel.HS);
        if (this.logger != null && this.logger.hs_Logging)
            this.logger.recs_req = false;
    }
    catch (Exception) { }

    StopLoggingCleanup();
    this.tsslStatus.Text = "Logging stopped";
}

private void StopLoggingCleanup()
{
    try { this.logFile?.Close(); } catch { }
    try { this.logger?.clearReqs(); } catch { }

    string[] strArray = this.txtLogFile.Text.Split(new[] { ".csv" }, StringSplitOptions.None);
    if (strArray.Length != 0)
        this.txtLogFile.Text = strArray[0] + "_" + this.numLogSessions + ".csv";
    else
        this.txtLogFile.Text = "";

    try
    {
        this.stopTimer();
        this.lblLogTime.Text = this.getLogTimeSeconds(false) + "s";
        this.logTime = 0L;
    }
    catch { }

    this.changeButtonState(true);
    this.cmdStopLogging.Enabled = false;
    this.cmdStartLogging.Enabled = true;
}
```

- [ ] **Step 5: Remove the old logTimer_Tick_1 method and logTimer references**

Remove the `logTimer` from frmMain.Designer.cs (it's no longer needed — the background thread replaces it).

- [ ] **Step 6: Fix lock(new object()) in flash timer ticks**

Replace `lock (new object())` with `lock (this.flashLock)` in `flashTimer_Tick` and `readTimer_Tick`.

- [ ] **Step 7: Build and verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```
fix: rewrite logging to use background thread with 50ms polling

- Move CAN I/O off UI thread to prevent freezing
- Parse parameters before ECULogger construction (stale ref bug)
- Add 10-error abort threshold for robustness
- Fix fuel pressure vitals checking wrong list
- Fix lock(new object()) no-ops with proper lock object
- Remove 1ms logTimer in favor of background Thread
```

---

### Task 6: Fix deserializeXML error handling

**Files:**
- Modify: `frmMain.cs` — fix null check ordering in `deserializeXML()`

- [ ] **Step 1: Fix the method**

```csharp
private void deserializeXML()
{
    try
    {
        this.eParams = ECUParameters.ReadObject(this.txtParamsFile.Text);
        this.eVars = this.eParams.ecuVars;
    }
    catch (Exception)
    {
        this.eParams = new ECUParameters();
        this.eVars = new ECUVariables();
        this.txtParamsFile.Text = "";
        this.setECUParams("");
        MessageBox.Show("Error parsing parameters file!",
            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
}
```

- [ ] **Step 2: Build, verify, commit**

```
fix: correct deserializeXML error handling and null check ordering
```

---

### Task 7: Final build verification and cleanup

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Launch app**

Run: `dotnet run`
Verify: App opens, both tabs work, no crashes

- [ ] **Step 3: Final commit if any cleanup needed**
