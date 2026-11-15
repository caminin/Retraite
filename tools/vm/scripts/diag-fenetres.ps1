# Diagnostic : arbre des fenêtres du bureau (Progman, WorkerW) et fenêtres du compteur.
# À lancer dans la session visible (tools/vm/vm.sh diag) ; résultat dans C:\Users\Public\diag.txt
Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class W {
  public delegate bool Proc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(Proc p, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, Proc p, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern IntPtr GetWindowLongPtr(IntPtr h, int i);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static string Info(IntPtr h) {
    var c = new StringBuilder(256); GetClassName(h, c, 256);
    var t = new StringBuilder(256); GetWindowText(h, t, 256);
    RECT r; GetWindowRect(h, out r); uint pid; GetWindowThreadProcessId(h, out pid);
    return String.Format("0x{0:X} classe={1} titre='{2}' visible={3} parent=0x{4:X} rect=({5},{6},{7},{8}) pid={9} exstyle=0x{10:X}",
      h.ToInt64(), c, t, IsWindowVisible(h), GetParent(h).ToInt64(), r.L, r.T, r.R, r.B, pid, GetWindowLongPtr(h, -20).ToInt64());
  }
}
"@
$sortie = New-Object System.Collections.Generic.List[string]
$pids = @(Get-Process CompteurRetraite -ErrorAction SilentlyContinue | ForEach-Object Id)
$sortie.Add("PID compteur : $pids")
$sortie.Add("== Fenêtres de premier niveau (Progman, WorkerW, compteur)")
[W]::EnumWindows({ param($h, $l)
  $i = [W]::Info($h)
  if ($i -match 'classe=(Progman|WorkerW|Shell_TrayWnd) ' -or ($pids | Where-Object { $i -match "pid=$_ " })) {
    $sortie.Add($i)
    [W]::EnumChildWindows($h, { param($c, $l2) $sortie.Add("    " + [W]::Info($c)); $true }, [IntPtr]::Zero) | Out-Null
  }
  $true }, [IntPtr]::Zero) | Out-Null
$sortie | Set-Content C:\Users\Public\diag.txt -Encoding UTF8
