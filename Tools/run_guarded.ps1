# Unity batch işini bellek bekçisiyle çalıştırır: Unity çalışma belleği LimitGB'ı aşarsa süreç sonlandırılır (sistem kilitlenmesin).
# Kullanım: powershell -File Tools/run_guarded.ps1 -Method CoopGame.EditorTools.CoopSetup.SetupAll -Log Logs/setup.log [-LimitGB 12] [-Extra "-draft"]
param(
    [Parameter(Mandatory = $true)][string]$Method,
    [Parameter(Mandatory = $true)][string]$Log,
    [double]$LimitGB = 12,
    [string]$Extra = ""
)
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
$proj = Split-Path -Parent $PSScriptRoot
$args = "-batchmode -quit -projectPath `"$proj`" -executeMethod $Method -logFile `"$proj\$Log`" $Extra"
$p = Start-Process -FilePath $unity -ArgumentList $args -PassThru
try { $p.PriorityClass = 'BelowNormal' } catch {}
$peak = 0.0
while (-not $p.HasExited) {
    Start-Sleep -Seconds 3
    $p.Refresh()
    # Unity'nin alt süreçleri (bake işçileri) dahil toplam
    $kids = Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.Id)" -ErrorAction SilentlyContinue
    $ws = $p.WorkingSet64
    foreach ($k in $kids) { $ws += [double]$k.WorkingSetSize }
    $gb = [math]::Round($ws / 1GB, 2)
    if ($gb -gt $peak) { $peak = $gb }
    if ($gb -gt $LimitGB) {
        "BEKCI: bellek $gb GB > $LimitGB GB, Unity durduruluyor"
        foreach ($k in $kids) { Stop-Process -Id $k.ProcessId -Force -ErrorAction SilentlyContinue }
        Stop-Process -Id $p.Id -Force
        "SONUC: DURDURULDU tepe=$peak GB"
        exit 2
    }
}
"SONUC: cikis=$($p.ExitCode) tepe=$peak GB"
exit $p.ExitCode
