# OrchestratorLock

Ein kleines Windows-Programm (WinForms) mit grafischer Oberfläche, das die **Zugriffsrechte (NTFS-ACLs) eines Ordners sperrt oder wieder entsperrt**. Voreingestellt ist der Aufgabenordner des Windows-Update-Orchestrators:

```
C:\Windows\System32\Tasks\Microsoft\Windows\UpdateOrchestrator
```

Das Programm automatisiert die Befehle `takeown` und `icacls`, die man sonst von Hand in einer Administrator-Eingabeaufforderung ausführen müsste, und zeigt deren Ausgabe in einem Protokollfenster an.

> **Wichtig:** Das Programm ändert ausschließlich Dateiberechtigungen. Es gibt **keine Garantie**, dass Windows dadurch ein bestimmtes Verhalten (z. B. Updates oder Neustarts) unterlässt. Außerdem greift es tief in Systemberechtigungen ein. Bitte den Abschnitt [Risiken und Hinweise](#risiken-und-hinweise) lesen, bevor du es benutzt.

---

## Inhalt

- [Funktionen](#funktionen)
- [Voraussetzungen](#voraussetzungen)
- [Dateien im Repository](#dateien-im-repository)
- [Kompilieren](#kompilieren)
- [Bedienung](#bedienung)
- [Was bei „Sperren“ und „Entsperren“ genau passiert](#was-bei-sperren-und-entsperren-genau-passiert)
- [Sicherheitsmechanismen im Programm](#sicherheitsmechanismen-im-programm)
- [Risiken und Hinweise](#risiken-und-hinweise)
- [Fehlerbehebung](#fehlerbehebung)
- [Lizenz](#lizenz)

---

## Funktionen

- **Sperren:** Entzieht allen Benutzern und Gruppen alle Rechte am Ordner und seinem Inhalt (Vererbung wird deaktiviert, die Zugriffsliste bleibt leer).
- **Entsperren:** Übernimmt den Besitz und stellt Vererbung und Standardrechte wieder her.
- **Status anzeigen:** Zeigt die aktuelle Zugriffsliste des Ordners (`icacls`).
- **Ordnerauswahl:** Standardordner ist der UpdateOrchestrator-Aufgabenordner, über „Durchsuchen…“ kann ein anderer Ordner gewählt werden.
- **Bestätigungsdialog** vor jeder Aktion. Vorausgewählt ist „Nein“.
- **Schutz vor Systemordnern:** Laufwerkswurzeln und zentrale Windows-Ordner werden abgelehnt.
- **Protokoll:** Jeder ausgeführte Befehl samt Ausgabe und Exitcode wird im Fenster angezeigt.
- **Sprachanpassung:** Die Antwort auf die `takeown`-Rückfrage (J/N bzw. Y/N) wird an die Systemsprache angepasst.

---

## Voraussetzungen

| Komponente | Hinweis |
|---|---|
| 64-Bit-Windows | Das Programm wird als x64 kompiliert (`/platform:x64`) |
| .NET Framework 4.x | In Windows 10 enthalten, liefert den Compiler `csc.exe` (C# 5) |
| Administratorrechte | Pflicht. Das Manifest fordert `requireAdministrator`, Windows zeigt beim Start die UAC-Abfrage |

Es muss keine Entwicklungsumgebung installiert werden.

---

## Dateien im Repository

| Datei | Zweck |
|---|---|
| `OrchestratorLock.cs` | Quellcode |
| `app.manifest` | Manifest, das Administratorrechte anfordert |
| `build.cmd` | Build-Skript, das die EXE kompiliert |

> Falls du die Dateien zum Hochladen mit `.txt` umbenannt hast: Im Repository müssen sie wieder `OrchestratorLock.cs`, `app.manifest` und `build.cmd` heißen. Das Build-Skript erwartet genau diese Namen.

---

## Kompilieren

1. Alle drei Dateien in denselben Ordner legen.
2. `build.cmd` per Doppelklick starten.

Das Skript sucht `csc.exe` unter `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\` und führt diesen Befehl aus:

```cmd
csc.exe /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:OrchestratorLock.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll OrchestratorLock.cs
```

Bei Erfolg erscheint `BUILD OK: <Pfad>\OrchestratorLock.exe`, sonst `BUILD FEHLGESCHLAGEN`. Findet das Skript den Compiler nicht, wird das gemeldet und das Skript beendet.

---

## Bedienung

1. `OrchestratorLock.exe` starten und die UAC-Abfrage bestätigen. Beim Start wird sofort der Status des eingestellten Ordners angezeigt.
2. Im Feld **Ordner** den Zielordner prüfen oder über **Durchsuchen…** einen anderen wählen.
3. Unter **Aktion** wählen:
   - **Sperren:** Vererbung deaktivieren und alle Benutzer/Rechte entziehen (Ordner und Dateien)
   - **Entsperren:** Besitz übernehmen, Vererbung aktivieren und Standardrechte wiederherstellen
4. **Ausführen** klicken und den Bestätigungsdialog lesen. Er nennt Aktion und Ordner.
5. Das Protokoll zeigt jeden Schritt. Am Ende steht `FERTIG: ERFOLGREICH` oder `FERTIG: MIT FEHLERN (siehe oben)`, danach folgt der aktuelle Status.
6. **Status anzeigen** gibt jederzeit die aktuelle Zugriffsliste aus.

Die Oberfläche ist auf Deutsch.

---

## Was bei „Sperren“ und „Entsperren“ genau passiert

Alle Befehle werden aus dem Ordner `%WINDIR%\System32` gestartet. Ausgabe und Fehlermeldungen erscheinen im Protokoll.

### Sperren

| Schritt | Befehl | Zweck |
|---|---|---|
| 1 | `takeown /F "<Ordner>" /A` | Besitz am Ordner an die Gruppe Administratoren |
| 2 | `icacls "<Ordner>" /reset` | Rechte des Ordners zurücksetzen |
| 3 | `takeown /F "<Ordner>" /A /R /D <J/Y/…>` | Besitz rekursiv für alle Inhalte |
| 4 | `icacls "<Ordner>" /reset /T /C` | Rechte rekursiv zurücksetzen |
| 5 | `icacls "<Eintrag>" /inheritance:r /T /C` | Für jede Datei und jeden Unterordner direkt im Ordner: Vererbung entfernen, rekursiv |
| 6 | `icacls "<Ordner>" /inheritance:r /C` | Zuletzt den Ordner selbst: Vererbung entfernen |

Schritt 1 bis 4 stellen sicher, dass der Zugriff auf alle Inhalte möglich ist. Die Schritte 5 und 6 entfernen die geerbten Rechte, sodass eine leere Zugriffsliste entsteht. Die Reihenfolge ist bewusst: erst Inhalte, zuletzt der Ordner.

### Entsperren

| Schritt | Befehl |
|---|---|
| 1 | `takeown /F "<Ordner>" /A` |
| 2 | `icacls "<Ordner>" /reset` |
| 3 | `takeown /F "<Ordner>" /A /R /D <J/Y/…>` |
| 4 | `icacls "<Ordner>" /reset /T /C` |

Laut Kommentar im Quellcode entspricht die Reihenfolge der, die manuell erfolgreich getestet wurde.

### Antwort auf die takeown-Rückfrage

`takeown` fragt bei gesperrten Unterordnern nach, ob der Besitz trotzdem übernommen werden soll. Die Antwort `/D` hängt von der installierten Systemsprache ab:

| Sprache | Antwort |
|---|---|
| Deutsch, Niederländisch | `J` |
| Französisch | `O` |
| Spanisch, Italienisch | `S` |
| alle anderen | `Y` |

---

## Sicherheitsmechanismen im Programm

Vor jeder Aktion prüft das Programm den Ordner und bricht mit einer Meldung ab, wenn:

- kein Ordner angegeben ist oder der Ordner nicht existiert
- der Ordner eine **Laufwerkswurzel** ist (z. B. `C:\`)
- der Ordner einer dieser Systemordner ist:
  - `C:\Windows`
  - `C:\Windows\System32`
  - `C:\Windows\System32\Tasks`
  - `C:\Windows\System32\Tasks\Microsoft`
  - `C:\Windows\System32\Tasks\Microsoft\Windows`
  - `Program Files` und `Program Files (x86)`
  - `<Laufwerk>:\Users`

Diese Prüfung vergleicht den **exakten** Pfad. **Unterordner** der genannten Ordner sind **nicht** automatisch geschützt, auch der Standardordner `...\Windows\UpdateOrchestrator` ist bewusst erlaubt.

Zusätzlich fragt ein Bestätigungsdialog vor der Ausführung nach, mit „Nein“ als Vorauswahl.

---

## Risiken und Hinweise

- **Eingriff in Systemberechtigungen:** „Sperren“ entzieht **allen** Benutzern und Gruppen die Rechte, einschließlich der Systemkonten. Windows-Komponenten können den Ordner danach womöglich nicht mehr lesen oder schreiben. Auswirkungen auf Windows lassen sich nicht vorhersagen. Teste das Programm zuerst mit einem eigenen Testordner.
- **Keine Garantie für ein Windows-Verhalten:** Das Programm ändert nur Dateirechte. Ob und wie Windows darauf reagiert, hängt von der Windows-Version und von Updates ab.
- **Besitzer wird nicht zurückgesetzt:** Beim Sperren und Entsperren wird der Besitz der Gruppe **Administratoren** zugewiesen. Der ursprüngliche Besitzer wird beim Entsperren nicht wiederhergestellt. Es werden nur Vererbung und Standardrechte zurückgesetzt.
- **Rechte werden zurückgesetzt, nicht gesichert:** Das Programm speichert vor dem Sperren **keine Sicherung** der bisherigen Rechte. Wer vorher benutzerdefinierte Rechte vergeben hat, verliert sie. Bei Bedarf vorher sichern, z. B. mit `icacls "<Ordner>" /save acl.txt /T`.
- **Nur auf eigenen Systemen verwenden**, für die du Administratorrechte besitzt und für die du verantwortlich bist.
- **Keine Gewähr:** Die Nutzung erfolgt auf eigene Verantwortung.

---

## Fehlerbehebung

| Problem | Mögliche Ursache / Lösung |
|---|---|
| UAC-Abfrage erscheint nicht / Programm startet nicht | Manuell per Rechtsklick „Als Administrator ausführen“ |
| `BUILD FEHLGESCHLAGEN` | Meldungen mit `error CS…` über der Zeile lesen. Prüfen, ob `OrchestratorLock.cs` und `app.manifest` im selben Ordner liegen |
| „csc.exe nicht gefunden“ | .NET Framework 4.x (64 Bit) ist nicht vorhanden oder liegt woanders |
| Meldung „Ordner ist aus Sicherheitsgründen gesperrt“ | Der gewählte Ordner steht auf der Verbotsliste, siehe oben |
| Exitcode ≠ 0 in einzelnen Schritten | Der Schritt ist fehlgeschlagen. Die Ausgabe im Protokoll zeigt den Grund. Am Ende steht dann `MIT FEHLERN` |

---

