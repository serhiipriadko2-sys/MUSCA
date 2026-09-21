$ErrorActionPreference = "Stop"
$exe = "C:\github\MUSCA-combat-v02\unity\MUSCA-Gate3D\Builds\CombatSandbox-v01\MUSCA-CombatSandbox-v01.exe"
$dir = "C:\github\MUSCA-combat-v02\unity\runtime-qa-v061"
Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$views = @("cameraidle","grounding","jump","dodge","lockon","telegraph","enemyai","combatstrike","kaelprediction")
foreach ($v in $views) {
    $png = Join-Path $dir ($v + ".png")
    $log = Join-Path $dir ($v + "-player.log")
    $args = @("--musca-qa=$v","--musca-qa-output=$png","-screen-width","1600","-screen-height","900","-screen-fullscreen","0","-logFile",$log)
    $p = Start-Process -FilePath $exe -ArgumentList $args -PassThru -Wait
    $json = Join-Path $dir ($v + ".json")
    if (Test-Path $json) {
        $q = Get-Content $json -Raw | ConvertFrom-Json
        Write-Output "$v exit=$($p.ExitCode) status=$($q.status)"
    } else {
        Write-Output "$v exit=$($p.ExitCode) status=MISSING_JSON"
    }
}
