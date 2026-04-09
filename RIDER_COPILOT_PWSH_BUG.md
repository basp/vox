# Rider Copilot `pwsh` Regression

## Summary

After a recent Rider Copilot update, Copilot tool execution in Rider appears unable to launch `pwsh`, even though `pwsh` is installed and works normally in the Rider integrated terminal on the same machine.

This looks like a regression specific to Rider and/or the Copilot plugin for Rider, because the same machine can use PowerShell successfully from VS Code Copilot.

## Reference Information
```
Copilot plugin version: 1.465.0
JetBrains Rider 2026.1
Build #RD-261.22158.335, built on March 29, 2026
Source revision: ba2101c71cea1
Licensed to ****
Subscription is active until ****
Runtime version: 25.0.2+1-b329.72 amd64137.0.17-261-b65
VM: OpenJDK 64-Bit Server VM by JetBrains s.r.o.
Toolkit: sun.awt.windows.WToolkit
Windows 10.0
Exception reporter ID: 10012517feb38a8-e935-48df-822a-571024572662
.NET Core v10.0.5 x64 (Server GC)
GC: G1 Young Generation, G1 Concurrent GC, G1 Old Generation
Memory: 2048M
Cores: 6
```

## Observed Behavior

When Copilot tries to execute PowerShell commands through its tool runner, it fails before running the requested command and reports:

```text
PowerShell 6+ (pwsh) is not available. Please install it from https://aka.ms/powershell.
Error: Command failed: pwsh.exe --version
'pwsh.exe' is not recognized as an internal or external command,
operable program or batch file.
```

This happens even for simple commands such as `Get-Command`, because the failure occurs before the command itself is executed.

## Expected Behavior

If `pwsh` is installed and available in Rider's environment, Copilot should be able to launch it and execute PowerShell tool commands successfully.

## Evidence

### 1. Rider integrated terminal resolves `pwsh` correctly

The Rider terminal shows `pwsh` on `PATH`, and both PowerShell and `where.exe` can resolve it:

```powershell
$env:PATH
...
C:\Program Files\PowerShell\7
...
C:\Program Files\PowerShell\7\
...

Get-Command pwsh -All

CommandType     Name       Version    Source
-----------     ----       -------    ------
Application     pwsh.exe   7.6.0.0    C:\Program Files\PowerShell\7\pwsh.exe

where.exe pwsh
C:\Program Files\PowerShell\7\pwsh.exe
```

### 2. Copilot tool runner in Rider does not resolve `pwsh`

From Copilot's tool execution path in Rider, the tool fails immediately with:

```text
'pwsh.exe' is not recognized as an internal or external command,
operable program or batch file.
```

### 3. VS Code Copilot works on the same machine

VS Code Copilot can use PowerShell without issue on the same system.

## Likely Cause

The Rider integrated terminal and the Rider Copilot tool runner appear to be using different execution environments.

Most likely, after the recent update, the Rider Copilot plugin or its helper process no longer inherits the same `PATH` environment as the Rider terminal, or otherwise resolves `pwsh` differently.

## Minimal Repro

1. Open Rider on a machine where `pwsh` is installed and available from the integrated terminal.
2. Confirm in Rider terminal:

   ```powershell
   Get-Command pwsh -All
   where.exe pwsh
   ```

3. Ask Copilot in Rider to run a PowerShell command such as:

   ```powershell
   Get-Command
   ```

4. Observe that Copilot tool execution fails before command execution with a `pwsh.exe` not recognized error.

## Impact

PowerShell-backed Copilot tool usage in Rider is effectively broken, even on systems where PowerShell 7 is properly installed and available in the IDE terminal.

## Suggested Investigation

- Compare the environment inherited by the Rider integrated terminal versus the Copilot tool runner/helper process.
- Check whether recent updates changed shell resolution or process launch behavior for Copilot tools in Rider.
- Verify whether `pwsh.exe` is being resolved from `PATH`, from a fixed location, or from a different host process environment.
