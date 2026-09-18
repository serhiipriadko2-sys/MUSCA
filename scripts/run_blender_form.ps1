param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$RunRoot
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repo 'blender\gate-lab-v0.2\scripts'
if (-not $RunRoot) {
    $runId = 'form-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
    $RunRoot = Join-Path $repo ('experiments\results\' + $runId)
}
$RunRoot = [IO.Path]::GetFullPath($RunRoot)
if (Test-Path -LiteralPath $RunRoot) { throw 'RunRoot already exists; choose a new run directory.' }
if (-not (Test-Path -LiteralPath $Blender -PathType Leaf)) { throw 'Blender executable missing.' }
New-Item -ItemType Directory -Path $RunRoot | Out-Null
$output = Join-Path $RunRoot 'artifacts'
$build = Join-Path $source 'build_gate_lab_form.py'
$readback = Join-Path $source 'validate_gate_lab_form.py'
function Invoke-BlenderStage([string]$Stage, [string[]]$StageArguments) {
    $stdout = Join-Path $RunRoot ($Stage + '.log')
    $stderr = Join-Path $RunRoot ($Stage + '.stderr.log')
    $job = Start-Process -FilePath $Blender -ArgumentList $StageArguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Write-Output ($Stage + '_PID=' + $job.Id)
    Write-Output ($Stage + '_EXITCODE=' + $job.ExitCode)
    if ($job.ExitCode -ne 0) { throw ($Stage + ' failed. Logs: ' + $stdout + ' / ' + $stderr) }
}
Write-Output ('RUN_ROOT=' + $RunRoot)
Invoke-BlenderStage 'build' @('--background','--factory-startup','--python-exit-code','1','--python',('"' + $build + '"'),'--','--output-dir',('"' + $output + '"'))
$built = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $output 'receipts\form-build.json') | ConvertFrom-Json
if ($built.status -ne 'PASS') { throw 'Build receipt failed.' }
$blendFile = Join-Path $output 'GateLab_Form_v0.1.blend'
Invoke-BlenderStage 'readback' @('--background','--factory-startup','--disable-autoexec',('"' + $blendFile + '"'),'--python-exit-code','1','--python',('"' + $readback + '"'))
$checked = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $output 'receipts\form-readback.json') | ConvertFrom-Json
if ($checked.status -ne 'PASS') { throw 'Read-back receipt failed.' }
Write-Output ('FORM_BUILD_AND_READBACK=PASS; OUTPUT=' + $output)
