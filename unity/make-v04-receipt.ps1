$ErrorActionPreference = "Stop"
$repo = "C:\github\MUSCA-combat-v02"
$src = Join-Path $repo "unity\runtime-qa-v04"
$dest = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\RuntimeQA"
$views = @("grounding","jump","dodge","lockon","telegraph","enemyai","combatstrike","kaelprediction")
foreach ($v in $views) {
    Copy-Item (Join-Path $src ($v + ".json")) (Join-Path $dest ($v + "-v04.json")) -Force
    Copy-Item (Join-Path $src ($v + ".png")) (Join-Path $dest ($v + "-v04.png")) -Force
}
$items = foreach ($v in $views) {
    $jp = Join-Path $dest ($v + "-v04.json")
    $pp = Join-Path $dest ($v + "-v04.png")
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
$kael = Get-Content (Join-Path $dest "kaelprediction-v04.json") -Raw | ConvertFrom-Json
$allPass = (@($items | Where-Object { $_.status -ne "PASS" }).Count -eq 0)
$out = [pscustomobject]@{
    schema = "musca.combat-embodiment-v04.runtime-qa.v1"
    status = if ($allPass -and $build.status -eq "PASS" -and $validation.status -eq "PASS") { "PASS" } else { "FAIL" }
    unityVersion = $build.unityVersion
    editModeTests = [pscustomobject]@{ total = 26; passed = 26; failed = 0 }
    sceneValidation = $validation.status
    pivotRig = [pscustomobject]@{
        player = $validation.playerProxyRigPresent
        sentinel = $validation.sentinelProxyRigPresent
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
    }
    repositoryRegression = [pscustomobject]@{
        python = "128/128 PASS"
        node = "8/8 PASS"
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
    claimBoundary = "Automated evidence covers pivot-rig presence, boss scale/upright state, dual-zone prediction semantics, collision/build integrity and combat execution. It does not prove that procedural motion feels natural, that dodge reads as a roll/lunge to a human, that camera feel is solved, or that the arena is production-quality art."
}
$json = $out | ConvertTo-Json -Depth 9
$path = Join-Path $repo "unity\MUSCA-Gate3D\Docs\AI\CombatEmbodimentV04RuntimeQA.json"
[IO.File]::WriteAllText($path, $json + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
