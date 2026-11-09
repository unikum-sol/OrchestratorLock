// OrchestratorLock - sperrt bzw. entsperrt einen Ordner (Standard: UpdateOrchestrator-Aufgabenordner)
// Kompatibel mit dem in Windows 10 enthaltenen csc.exe (.NET Framework 4.x, C# 5).
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OrchestratorLock
{
    public class MainForm : Form
    {
        private const string DefaultPath = @"C:\Windows\System32\Tasks\Microsoft\Windows\UpdateOrchestrator";

        private TextBox txtPath;
        private Button btnBrowse;
        private RadioButton rbLock;
        private RadioButton rbUnlock;
        private Button btnRun;
        private Button btnStatus;
        private TextBox txtLog;

        public MainForm()
        {
            Text = "UpdateOrchestrator Sperre";
            Width = 760;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            Label lblPath = new Label();
            lblPath.Text = "Ordner:";
            lblPath.Left = 12; lblPath.Top = 15; lblPath.Width = 60;

            txtPath = new TextBox();
            txtPath.Left = 75; txtPath.Top = 12; txtPath.Width = 560;
            txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPath.Text = Environment.ExpandEnvironmentVariables(@"%WINDIR%\System32\Tasks\Microsoft\Windows\UpdateOrchestrator");
            if (string.IsNullOrEmpty(txtPath.Text)) txtPath.Text = DefaultPath;

            btnBrowse = new Button();
            btnBrowse.Text = "Durchsuchen...";
            btnBrowse.Left = 642; btnBrowse.Top = 10; btnBrowse.Width = 95;
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.Click += BtnBrowse_Click;

            GroupBox grp = new GroupBox();
            grp.Text = "Aktion";
            grp.Left = 12; grp.Top = 45; grp.Width = 725; grp.Height = 80;
            grp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            rbLock = new RadioButton();
            rbLock.Text = "Sperren: Vererbung deaktivieren und ALLE Benutzer/Rechte entziehen (Ordner + Dateien)";
            rbLock.Left = 12; rbLock.Top = 20; rbLock.Width = 700;
            rbLock.Checked = true;

            rbUnlock = new RadioButton();
            rbUnlock.Text = "Entsperren: Besitz übernehmen, Vererbung aktivieren und Standardrechte wiederherstellen";
            rbUnlock.Left = 12; rbUnlock.Top = 46; rbUnlock.Width = 700;

            grp.Controls.Add(rbLock);
            grp.Controls.Add(rbUnlock);

            btnRun = new Button();
            btnRun.Text = "Ausführen";
            btnRun.Left = 12; btnRun.Top = 135; btnRun.Width = 120; btnRun.Height = 30;
            btnRun.Click += BtnRun_Click;

            btnStatus = new Button();
            btnStatus.Text = "Status anzeigen";
            btnStatus.Left = 140; btnStatus.Top = 135; btnStatus.Width = 120; btnStatus.Height = 30;
            btnStatus.Click += BtnStatus_Click;

            txtLog = new TextBox();
            txtLog.Left = 12; txtLog.Top = 175; txtLog.Width = 725; txtLog.Height = 335;
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Both;
            txtLog.WordWrap = false;
            txtLog.ReadOnly = true;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.Add(lblPath);
            Controls.Add(txtPath);
            Controls.Add(btnBrowse);
            Controls.Add(grp);
            Controls.Add(btnRun);
            Controls.Add(btnStatus);
            Controls.Add(txtLog);

            Shown += delegate { ShowStatus(); };
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Ordner auswählen";
                dlg.ShowNewFolderButton = false;
                if (Directory.Exists(txtPath.Text)) dlg.SelectedPath = txtPath.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtPath.Text = dlg.SelectedPath;
                    ShowStatus();
                }
            }
        }

        private void BtnStatus_Click(object sender, EventArgs e)
        {
            ShowStatus();
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            string path = txtPath.Text.Trim().TrimEnd('\\');
            string error = ValidatePath(path);
            if (error != null)
            {
                MessageBox.Show(this, error, "Ungültiger Ordner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool doLock = rbLock.Checked;
            string action = doLock ? "SPERREN (alle Rechte entziehen)" : "ENTSPERREN (Vererbung/Standardrechte wiederherstellen)";
            DialogResult answer = MessageBox.Show(this,
                "Aktion: " + action + "\n\nOrdner:\n" + path + "\n\nFortfahren?",
                "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;

            SetBusy(true);
            txtLog.Clear();
            try
            {
                bool ok = doLock ? DoLock(path) : DoUnlock(path);
                Log("");
                Log(ok ? "===== FERTIG: ERFOLGREICH =====" : "===== FERTIG: MIT FEHLERN (siehe oben) =====");
                Log("");
                AppendStatus(path);
            }
            catch (Exception ex)
            {
                Log("FEHLER: " + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Sicherheitsprüfung: keine Laufwerkswurzel, keine zentralen Systemordner
        private static string ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Kein Ordner angegeben.";
            if (!Directory.Exists(path)) return "Ordner existiert nicht:\n" + path;

            string full = Path.GetFullPath(path).TrimEnd('\\');
            string root = Path.GetPathRoot(full).TrimEnd('\\');
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
                return "Eine Laufwerkswurzel darf nicht bearbeitet werden.";

            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
            string[] forbidden = new string[] {
                win,
                win + @"\System32",
                win + @"\System32\Tasks",
                win + @"\System32\Tasks\Microsoft",
                win + @"\System32\Tasks\Microsoft\Windows",
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\'),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).TrimEnd('\\'),
                root + @"\Users"
            };
            foreach (string f in forbidden)
            {
                if (!string.IsNullOrEmpty(f) && string.Equals(full, f, StringComparison.OrdinalIgnoreCase))
                    return "Dieser Systemordner ist aus Sicherheitsgründen gesperrt:\n" + full;
            }
            return null;
        }

        // takeown fragt bei gesperrten Unterordnern J/N bzw. Y/N - Buchstabe abhängig von der Systemsprache
        private static string YesLetter()
        {
            string lang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (lang == "de") return "J";
            if (lang == "fr") return "O";
            if (lang == "es" || lang == "it") return "S";
            if (lang == "nl") return "J";
            return "Y";
        }

        private bool DoLock(string path)
        {
            bool ok = true;
            Log("### SPERREN: " + path);

            // 1. Besitz an Ordner und Inhalt (Administratoren), damit alles erreichbar ist
            ok &= Run("takeown.exe", "/F \"" + path + "\" /A") == 0;
            ok &= Run("icacls.exe", "\"" + path + "\" /reset") == 0;
            ok &= Run("takeown.exe", "/F \"" + path + "\" /A /R /D " + YesLetter()) == 0;
            ok &= Run("icacls.exe", "\"" + path + "\" /reset /T /C") == 0;

            // 2. Dateien/Unterordner zuerst: Vererbung entfernen -> leere DACL
            string[] entries = Directory.GetFileSystemEntries(path);
            foreach (string entry in entries)
            {
                ok &= Run("icacls.exe", "\"" + entry + "\" /inheritance:r /T /C") == 0;
            }

            // 3. Zuletzt der Ordner selbst: Vererbung entfernen -> leere DACL
            ok &= Run("icacls.exe", "\"" + path + "\" /inheritance:r /C") == 0;
            return ok;
        }

        private bool DoUnlock(string path)
        {
            bool ok = true;
            Log("### ENTSPERREN: " + path);
            // Reihenfolge wie manuell erfolgreich getestet: erst Ordner, dann rekursiv
            ok &= Run("takeown.exe", "/F \"" + path + "\" /A") == 0;
            ok &= Run("icacls.exe", "\"" + path + "\" /reset") == 0;
            ok &= Run("takeown.exe", "/F \"" + path + "\" /A /R /D " + YesLetter()) == 0;
            ok &= Run("icacls.exe", "\"" + path + "\" /reset /T /C") == 0;
            return ok;
        }

        private void ShowStatus()
        {
            txtLog.Clear();
            string path = txtPath.Text.Trim().TrimEnd('\\');
            if (!Directory.Exists(path))
            {
                Log("Ordner existiert nicht: " + path);
                return;
            }
            AppendStatus(path);
        }

        private void AppendStatus(string path)
        {
            Log("### STATUS: " + path);
            Run("icacls.exe", "\"" + path + "\"");
        }

        private int Run(string exe, string args)
        {
            string sys = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");
            string full = Path.Combine(sys, exe);
            Log("> " + exe + " " + args);

            ProcessStartInfo psi = new ProcessStartInfo(full, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            Encoding oem = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            psi.StandardOutputEncoding = oem;
            psi.StandardErrorEncoding = oem;

            using (Process p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (output.Trim().Length > 0) Log(output.TrimEnd());
                if (err.Trim().Length > 0) Log(err.TrimEnd());
                Log("  (Exitcode " + p.ExitCode + ")");
                Log("");
                return p.ExitCode;
            }
        }

        private void Log(string text)
        {
            txtLog.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
            Application.DoEvents();
        }

        private void SetBusy(bool busy)
        {
            btnRun.Enabled = !busy;
            btnStatus.Enabled = !busy;
            btnBrowse.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
