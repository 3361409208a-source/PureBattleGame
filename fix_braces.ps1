$path = "c:\Users\18229\.openclaw\workspace\PureBattleGame\Games\StarCoreDefense\BattleForm.cs"
$lines = Get-Content $path
if ($lines[-1] -match "}") {
    $newLines = $lines[0..($lines.Count - 2)]
    Set-Content $path $newLines
    Write-Host "Removed last line"
}
