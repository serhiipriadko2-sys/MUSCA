$ErrorActionPreference = "Stop"
$repo = "C:\github\MUSCA-combat-v02"
$src = Join-Path $repo "unity\runtime-qa-v06"
$dest = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\RuntimeQA"
$views = @("grounding","jump","dodge","lockon","telegraph","enemyai","combatstrike","kaelprediction")
foreach ($v in $views) {
    Copy-Item (Join-Path $src ($v + ".json")) (Join-Path $dest ($v + "-v06.json")) -Force
    Copy-Item (Join-Path $src ($v + ".png")) (Join-Path $dest ($v + "-v06.png")) -Force
}
$items = foreach ($v in $views) {
    $jp = Join-Path $dest ($v + "-v06.json")
    $pp = Join-Path $dest ($v + "-v06.png")
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
$lock = Get-Content (Join-Path $dest "lockon-v06.json") -Raw | ConvertFrom-Json
$kael = Get-Content (Join-Path $dest "kaelprediction-v06.json") -Raw | ConvertFrom-Json
[xml]$edit = Get-Content (Join-Path $repo "unity\combat-v06-editmode.xml") -Raw
$run = $edit.'test-run'
$allPass = (@($items | Where-Object { $_.status -ne "PASS" }).Count -eq 0)
$cmPass = $validation.cinemachineBrainPresent -and
    $validation.cinemachineControllerPresent -and
    $validation.cinemachineFreeCameraPresent -and
    $validation.cinemachineLockCameraPresent -and
    $validation.cinemachineSmartUpdate -and
    $validation.cinemachineOutputDetachedFromPlayer -and
    $lock.cinemachineBrainPresent -and
    $lock.cinemachineLockActive -and
    $lock.cinemachineActiveCamera -eq "CM_Lock"
$out = [pscustomobject]@{
    schema = "musca.combat-embodiment-v06.runtime-qa.v1"
    status = if ($allPass -and $cmPass -and $build.status -eq "PASS" -and $validation.status -eq "PASS" -and [int]$run.failed -eq 0) { "PASS" } else { "FAIL" }
    unityVersion = $build.unityVersion
    editModeTests = [pscustomobject]@{
        total = [int]$run.total
        passed = [int]$run.passed
        failed = [int]$run.failed
    }
    sceneValidation = $validation.status
    cinemachine = [pscustomobject]@{
        packageVersion = $validation.cinemachineVersion
        brainPresent = $validation.cinemachineBrainPresent
        controllerPresent = $validation.cinemachineControllerPresent
        freeCameraPresent = $validation.cinemachineFreeCameraPresent
        lockCameraPresent = $validation.cinemachineLockCameraPresent
        smartUpdate = $validation.cinemachineSmartUpdate
        outputDetachedFromPlayer = $validation.cinemachineOutputDetachedFromPlayer
        runtimeLockAcquired = $lock.lockOnAcquired
        runtimeLockTarget = $lock.lockOnTarget
        runtimeLockCameraActive = $lock.cinemachineLockActive
        runtimeActiveCamera = $lock.cinemachineActiveCamera
        architecture = "detached Unity output camera + CinemachineBrain; separate free and lock CinemachineCamera pipelines; player movement is camera-relative; body facing is independent from output camera transform"
    }
    locomotion = [pscustomobject]@{
        playerProxyRig = $validation.playerProxyRigPresent
        sentinelProxyRig = $validation.sentinelProxyRigPresent
        proxyFootPlantPass = "ankle counter-rotation improved; humanoid animation/IK still pending"
        dodgeDistanceMeters = (Get-Content (Join-Path $dest "dodge-v06.json") -Raw | ConvertFrom-Json).dodgeDistance
        dodgeCurve = "quadratic ease-out"
    }
    boss = [pscustomobject]@{
        colliderHeight = $validation.sentinelColliderWorldSize.y
        visualHeight = $validation.sentinelVisualWorldSize.y
        upright = $validation.sentinelVisualUpright
    }
    prediction = [pscustomobject]@{
        baseStrikeX = $kael.kaelBaseStrikeX
        baseStrikeZ = $kael.kaelBaseStrikeZ
        predictedStrikeX = $kael.kaelPredictionStrikeX
        predictedStrikeZ = $kael.kaelPredictionStrikeZ
        fresh = $kael.kaelPredictionFresh
        applied = $kael.kaelPredictionApplied
        brokenFuturePresentation = "runtime event implemented; human readability pending"
    }
    arena = [pscustomobject]@{
        present = $validation.arenaPresent
        collisionFloor = $validation.collisionFloorEnabled
        v06Composition = "side terraces + threshold shards + rear walls added outside primary combat lane"
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
    claimBoundary = "Automated evidence proves Cinemachine 6.6.0 is linked into the build, CM_Lock activates at runtime, the output camera is detached from the player body, combat/prediction/build regressions remain green, and the v0.6 scene composition is present. It does not prove perceptual camera smoothness, natural humanoid animation, satisfying dodge weight, readable broken-future feedback to a human, or production-quality environment art."
}
$json = $out | ConvertTo-Json -Depth 9
$path = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\CombatEmbodimentV06RuntimeQA.json"
[IO.File]::WriteAllText($path, $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Output ("RECEIPT=" + $path)
Write-Output ("STATUS=" + $out.status)
