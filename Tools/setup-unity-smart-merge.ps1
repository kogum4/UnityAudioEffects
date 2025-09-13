param(
    [string]$UnityYAMLMergePath
)

function Find-UnityYAMLMerge {
    $candidates = @(
        "$env:ProgramFiles/Unity/Hub/Editor/*/Editor/Data/Tools/UnityYAMLMerge.exe",
        "$env:ProgramFiles(x86)/Unity/Hub/Editor/*/Editor/Data/Tools/UnityYAMLMerge.exe"
    )
    foreach ($pattern in $candidates) {
        Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    }
}

if (-not $UnityYAMLMergePath -or -not (Test-Path $UnityYAMLMergePath)) {
    $found = Find-UnityYAMLMerge
    if ($found) {
        $UnityYAMLMergePath = $found.FullName
    }
}

if (-not (Test-Path $UnityYAMLMergePath)) {
    Write-Error "Could not locate UnityYAMLMerge.exe. Pass -UnityYAMLMergePath or install Unity Hub Editor."
    exit 1
}

Write-Host "Configuring Git merge driver for UnityYAMLMerge at: $UnityYAMLMergePath"

& git config merge.unityyamlmerge.name "Unity Smart Merge (UnityYAMLMerge)" | Out-Null
& git config merge.unityyamlmerge.driver '"'+$UnityYAMLMergePath+'" merge -p %O %A %B %A' | Out-Null

Write-Host "Done. Git will use Unity Smart Merge for file types defined in .gitattributes."

