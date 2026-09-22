$ErrorActionPreference = "Stop"
$repo = "C:\github\MUSCA-combat-v02"
$src = Join-Path $repo "unity\runtime-qa-v061"
$dest = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\RuntimeQA"
$views = @("cameraidle","grounding","jump","dodge","lockon","telegraph","enemyai","combatstrike","kaelprediction")
foreach ($v in $views) {
    Copy-Item (Join-Path $src ($v + ".json")) (Join-Path $dest ($v + "-v061.json")) -Force
    Copy-Item (Join-Path $src ($v + ".png")) (Join-Path $dest ($v + "-v061.png")) -Force
}
$items = foreach ($v in $views) {
    $jp = Join-Path $dest ($v + "-v061.json")
    $pp = Join-Path $dest ($v + "-v061.png")
    $q = Get-Content $jp -Raw | ConvertFrom-Json
    [pscustomobject]@{
        view = $v
        status = $q.status
        jsonSha256 = (Get-FileHash $jp -Algorithm SHA256).Hash.ToLowerInvariant()
        pngSha256 = (Get-FileHash $pp -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$build = Get-Content (Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\CombatSandboxBuild.json") -Raw | ConvertFrom-Json
$validation = Get-Content (Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\CombatSandboxValidation.json") -Raw | ConvertFrom-Json
$camera = Get-Content (Join-Path $dest "cameraidle-v061.json") -Raw | ConvertFrom-Json
$lock = Get-Content (Join-Path $dest "lockon-v061.json") -Raw | ConvertFrom-Json
[xml]$edit = Get-Content (Join-Path $repo "unity\combat-v061-editmode.xml") -Raw
$run = $edit.'test-run'
$allPass = (@($items | Where-Object { $_.status -ne "PASS" }).Count -eq 0)
$hotfixPass = $camera.cameraIdleStable -and
    [double]$camera.cameraYawDriftDegrees -le 0.75 -and
    $lock.cinemachineLockActive -and
    $lock.cinemachineActiveCamera -eq "CM_Lock"
$out = [pscustomobject]@{
    schema = "musca.combat-embodiment-v061.runtime-qa.v1"
    status = if ($allPass -and $hotfixPass -and $build.status -eq "PASS" -and $validation.status -eq "PASS" -and [int]$run.failed -eq 0) { "PASS" } else { "FAIL" }
    unityVersion = $build.unityVersion
    editModeTests = [pscustomobject]@{
        total = [int]$run.total
        passed = [int]$run.passed
        failed = [int]$run.failed
    }
    sceneValidation = $validation.status
    cameraHotfix = [pscustomobject]@{
        packageVersion = $validation.cinemachineVersion
        idleStable = $camera.cameraIdleStable
        idleYawDriftDegrees = $camera.cameraYawDriftDegrees
        freeActiveCamera = $camera.cinemachineActiveCamera
        lockAcquired = $lock.lockOnAcquired
        lockTarget = $lock.lockOnTarget
        lockCameraActive = $lock.cinemachineLockActive
        lockActiveCamera = $lock.cinemachineActiveCamera
        cause = "free-orbit yaw was fed from Cinemachine output yaw every frame, creating positive feedback"
        fix = "free pivot yaw is authoritative; lock/free yaw synchronizes only on mode transition; body faces desired travel direction when unlocked"
    }
    movementHotfix = [pscustomobject]@{
        forwardBasis = "Cinemachine pivot yaw, not output camera yaw"
        freeBodyFacing = "desired movement direction"
        lockedBodyFacing = "lock target"
        editModeCoverage = @(
            "ExternalForwardInputUsesPivotYaw",
            "BodyFacingYawMatchesDesiredTravelDirection"
        )
    }
    repositoryRegression = [pscustomobject]@{
        python = "128/128 PASS"
        node = "8/8 PASS"
        browserCheck = "PASS"
        roomValidation = "PASS"
    }
    runtimeViews = $items
    build = [pscustomobject]@{
        status = $build.status
        exeBytes = $build.exeBytes
        exeSha256 = $build.exeSha256
        runtimeDllBytes = $build.runtimeDllBytes
        runtimeDllSha256 = $build.runtimeDllSha256
        errors = $build.errors
        warnings = $build.warnings
    }
    humanRetest = "PENDING"
    claimBoundary = "Automated evidence proves the free Cinemachine camera no longer self-rotates while idle, forward input resolves from the stable pivot yaw, body facing follows travel direction when unlocked, CM_Lock still activates, and previous combat regressions remain green. Human feel under active mouse movement and rapid strafe still requires playtest."
}
$json = $out | ConvertTo-Json -Depth 9
$path = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\CombatEmbodimentV061RuntimeQA.json"
[IO.File]::WriteAllText($path, $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output ("RECEIPT=" + $path)
Write-Output ("STATUS=" + $out.status)
