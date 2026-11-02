<#
.SYNOPSIS
  Summarises viBro-metrics.csv: count, median, 90th percentile and max latency per
  measurement kind, storage type, seek mode and hardware decoder; plus peak memory.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build\analyze-metrics.ps1 -Path C:\Tools\viBro-metrics.csv
#>
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$Path)

$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
$rows = Import-Csv -Path $Path
if (-not $rows) { Write-Host 'No measurements.'; return }

function Get-Percentile([double[]]$Values, [double]$P) {
    $s = $Values | Sort-Object
    if ($s.Count -eq 1) { return $s[0] }
    $rank = $P * ($s.Count - 1)
    $lo = [math]::Floor($rank); $hi = [math]::Ceiling($rank)
    return $s[$lo] + ($s[$hi] - $s[$lo]) * ($rank - $lo)
}

$rows |
    Group-Object kind, storage, seek_mode, hwdec |
    ForEach-Object {
        $ms = [double[]]($_.Group | ForEach-Object { [double]::Parse($_.ms, $inv) })
        $first = $_.Group[0]
        [pscustomobject]@{
            kind      = $first.kind
            storage   = $first.storage
            seek_mode = $first.seek_mode
            hwdec     = $first.hwdec
            n         = $ms.Count
            median_ms = [math]::Round((Get-Percentile $ms 0.5), 0)
            p90_ms    = [math]::Round((Get-Percentile $ms 0.9), 0)
            max_ms    = [math]::Round(($ms | Measure-Object -Maximum).Maximum, 0)
        }
    } |
    Sort-Object kind, storage, seek_mode, hwdec |
    Format-Table -AutoSize

$peak = ($rows | ForEach-Object { [double]::Parse($_.private_mb, $inv) } | Measure-Object -Maximum).Maximum
$peakWs = ($rows | ForEach-Object { [double]::Parse($_.working_set_mb, $inv) } | Measure-Object -Maximum).Maximum
Write-Host ("Peak private memory: {0:0} MB, peak working set: {1:0} MB, rows: {2}" -f $peak, $peakWs, $rows.Count)
