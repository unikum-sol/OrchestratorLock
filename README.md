# OrchestratorLock

A small Windows (WinForms) program with a graphical interface that **locks or unlocks the access permissions (NTFS ACLs) of a folder**. The default target is the task folder of the Windows Update Orchestrator:

```
C:\Windows\System32\Tasks\Microsoft\Windows\UpdateOrchestrator
```

The program automates the `takeown` and `icacls` commands that you would otherwise run by hand in an elevated command prompt, and shows their output in a log window.

> **Important:** The program only changes file permissions. There is **no guarantee** that Windows will refrain from any particular behavior (for example updates or restarts) as a result. It also makes deep changes to system permissions. Please read [Risks and notes](#risks-and-notes) before using it.

---

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Files in the repository](#files-in-the-repository)
- [Building](#building)
- [Usage](#usage)
- [What "Lock" and "Unlock" do exactly](#what-lock-and-unlock-do-exactly)
- [Built-in safeguards](#built-in-safeguards)
- [Risks and notes](#risks-and-notes)
- [Troubleshooting](#troubleshooting)
- [License](#license)

---

## Features

- **Lock:** Removes all rights of all users and groups on the folder and its contents (inheritance is disabled, the access list stays empty).
- **Unlock:** Takes ownership and restores inheritance and default permissions.
- **Show status:** Displays the folder's current access list (`icacls`).
- **Folder selection:** The default folder is the UpdateOrchestrator task folder. Use "Durchsuchen…" (Browse) to choose another folder.
- **Confirmation dialog** before every action. "No" is preselected.
- **Protection of system folders:** Drive roots and key Windows folders are rejected.
- **Log:** Every command that is run is shown with its output and exit code.
- **Language adaptation:** The answer to `takeown`'s prompt (Y/N or J/N) is adapted to the system language.

---

## Requirements

| Component | Note |
|---|---|
| 64-bit Windows | The program is compiled as x64 (`/platform:x64`) |
| .NET Framework 4.x | Included in Windows 10, provides the compiler `csc.exe` (C# 5) |
| Administrator rights | Required. The manifest requests `requireAdministrator`, so Windows shows a UAC prompt at startup |

No development environment needs to be installed.

---

## Files in the repository

| File | Purpose |
|---|---|
| `OrchestratorLock.cs` | Source code |
| `app.manifest` | Manifest that requests administrator rights |
| `build.cmd` | Build script that compiles the EXE |

> If you renamed the files with `.txt` to upload them: in the repository they must be named `OrchestratorLock.cs`, `app.manifest` and `build.cmd` again. The build script expects exactly these names.

---

## Building

1. Put all three files into the same folder.
2. Double-click `build.cmd`.

The script looks for `csc.exe` in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\` and runs this command:

```cmd
csc.exe /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:OrchestratorLock.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll OrchestratorLock.cs
```

On success it prints `BUILD OK: <path>\OrchestratorLock.exe`, otherwise `BUILD FEHLGESCHLAGEN` (build failed). If the compiler cannot be found, the script reports this and exits.

> The messages of `build.cmd` are in German.

---

## Usage

1. Start `OrchestratorLock.exe` and confirm the UAC prompt. At startup, the status of the configured folder is shown immediately.
2. Check the **Ordner** (folder) field or choose another folder with **Durchsuchen…** (Browse).
3. Under **Aktion** (action), choose:
   - **Sperren** (Lock): disable inheritance and remove ALL users/rights (folder and files)
   - **Entsperren** (Unlock): take ownership, enable inheritance and restore default permissions
4. Click **Ausführen** (Run) and read the confirmation dialog. It names the action and the folder.
5. The log shows every step. At the end it says `FERTIG: ERFOLGREICH` (finished: successful) or `FERTIG: MIT FEHLERN (siehe oben)` (finished: with errors, see above), followed by the current status.
6. **Status anzeigen** (Show status) prints the current access list at any time.

The user interface is in German.

---

## What "Lock" and "Unlock" do exactly

All commands are started from `%WINDIR%\System32`. Output and error messages appear in the log.

### Lock

| Step | Command | Purpose |
|---|---|---|
| 1 | `takeown /F "<folder>" /A` | Give ownership of the folder to the Administrators group |
| 2 | `icacls "<folder>" /reset` | Reset the folder's permissions |
| 3 | `takeown /F "<folder>" /A /R /D <J/Y/…>` | Take ownership of all contents recursively |
| 4 | `icacls "<folder>" /reset /T /C` | Reset permissions recursively |
| 5 | `icacls "<entry>" /inheritance:r /T /C` | For each file and subfolder directly in the folder: remove inheritance, recursively |
| 6 | `icacls "<folder>" /inheritance:r /C` | Finally the folder itself: remove inheritance |

Steps 1 to 4 make sure all contents are accessible. Steps 5 and 6 remove the inherited rights, which leaves an empty access list. The order is deliberate: contents first, the folder last.

### Unlock

| Step | Command |
|---|---|
| 1 | `takeown /F "<folder>" /A` |
| 2 | `icacls "<folder>" /reset` |
| 3 | `takeown /F "<folder>" /A /R /D <J/Y/…>` |
| 4 | `icacls "<folder>" /reset /T /C` |

According to a comment in the source code, this order is the one that was tested successfully by hand.

### Answer to the takeown prompt

For locked subfolders, `takeown` asks whether ownership should be taken anyway. The `/D` answer depends on the installed system language:

| Language | Answer |
|---|---|
| German, Dutch | `J` |
| French | `O` |
| Spanish, Italian | `S` |
| all others | `Y` |

---

## Built-in safeguards

Before every action the program checks the folder and aborts with a message if:

- no folder is given or the folder does not exist
- the folder is a **drive root** (for example `C:\`)
- the folder is one of these system folders:
  - `C:\Windows`
  - `C:\Windows\System32`
  - `C:\Windows\System32\Tasks`
  - `C:\Windows\System32\Tasks\Microsoft`
  - `C:\Windows\System32\Tasks\Microsoft\Windows`
  - `Program Files` and `Program Files (x86)`
  - `<drive>:\Users`

This check compares the **exact** path. **Subfolders** of the listed folders are **not** automatically protected. The default folder `...\Windows\UpdateOrchestrator` is deliberately allowed.

In addition, a confirmation dialog asks before execution, with "No" preselected.

---

## Risks and notes

- **Changes to system permissions:** "Lock" removes the rights of **all** users and groups, including the system accounts. Windows components may then be unable to read or write the folder. The effects on Windows cannot be predicted. Try the program on a test folder of your own first.
- **No guarantee of Windows behavior:** The program only changes file permissions. Whether and how Windows reacts depends on the Windows version and on updates.
- **Owner is not restored:** Both locking and unlocking assign ownership to the **Administrators** group. Unlocking does not restore the original owner. Only inheritance and default permissions are reset.
- **Permissions are reset, not backed up:** The program makes **no backup** of the previous permissions before locking. Custom permissions you set earlier are lost. If needed, back them up first, for example with `icacls "<folder>" /save acl.txt /T`.
- **Use only on systems you own** or are responsible for, and where you have administrator rights.
- **No warranty:** Use at your own risk.

---

## Troubleshooting

| Problem | Possible cause / solution |
|---|---|
| UAC prompt does not appear / program does not start | Right-click and choose "Run as administrator" |
| `BUILD FEHLGESCHLAGEN` (build failed) | Read the messages with `error CS…` above that line. Check that `OrchestratorLock.cs` and `app.manifest` are in the same folder |
| "csc.exe nicht gefunden" (csc.exe not found) | .NET Framework 4.x (64-bit) is missing or installed elsewhere |
| Message "Ordner ist aus Sicherheitsgründen gesperrt" (folder blocked for safety reasons) | The chosen folder is on the blocklist, see above |
| Exit code ≠ 0 in individual steps | That step failed. The output in the log shows why. The log then ends with `MIT FEHLERN` (with errors) |

---


