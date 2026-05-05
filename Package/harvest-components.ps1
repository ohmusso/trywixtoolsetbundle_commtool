# --- 設定項目 ---
# wxsファイルから見たインストール対象フォルダの相対パス
$relativeSourcePath = "..\ConsoleApp1\bin\Release\net10.0"
$manufacturer = "!(bind.Property.Manufacturer)"
$productName = "!(bind.Property.ProductName)"

# --- 変換関数 ---
function Get-WixSafeId {
    param([string]$name)
    $safeId = $name -replace '[^a-zA-Z0-9_]', '_'
    if ($safeId -match '^[0-9]') { $safeId = "_" + $safeId }
    return $safeId
}

# --- データ収集 ---
# 実行環境のフルパスを取得して計算の基準にする
$basePath = (Get-Item $relativeSourcePath).FullName
$allEntries = Get-ChildItem -Path $basePath -Recurse
$files = $allEntries | Where-Object { -not $_.PSIsContainer }
$dirs = $allEntries | Where-Object { $_.PSIsContainer }

$fileComponents = @()
$directoryDefinitions = @()
$cleanupComponents = @()

# --- 1. ディレクトリ定義とクリーンアップ ---
foreach ($dir in $dirs) {
    $relDir = $dir.FullName.Substring($basePath.Length).TrimStart("\")
    $dirId = Get-WixSafeId -name $relDir
    
    $directoryDefinitions += "    <Directory Id=""$dirId"" Name=""$($dir.Name)"" />"
    $cleanupComponents += @"
        <Component Id="Cleanup_$dirId" Guid="$([Guid]::NewGuid().ToString().ToUpper())" Directory="$dirId">
            <RegistryValue Root="HKCU" Key="Software\$manufacturer\$productName\Components" Name="Cleanup_$dirId" Value="1" Type="integer" KeyPath="yes" />
            <RemoveFolder Id="Remove_$dirId" On="uninstall" />
        </Component>
"@
}

# --- 2. ファイルコンポーネント (相対パス Source) ---
foreach ($file in $files) {
    # basepathからの相対パスを取得 (例: "settings\settings.json")
    $internalPath = $file.FullName.Substring($basePath.Length).TrimStart("\")
    $subDir = Split-Path $internalPath -Parent
    $dirId = if ([string]::IsNullOrEmpty($subDir)) { "INSTALLFOLDER" } else { Get-WixSafeId -name $subDir }
    
    # WiXに書き出す相対ソースパス (例: "..\ConsoleApp1\bin\Release\net10.0\settings\settings.json")
    $wixSourcePath = Join-Path $relativeSourcePath $internalPath
    
    $fileNameSafe = Get-WixSafeId -name $file.Name
    
    $fileComponents += @"
            <Component Id="cmp_$($fileNameSafe)" Guid="$([Guid]::NewGuid().ToString().ToUpper())" Directory="$dirId">
                <File Source="$wixSourcePath" KeyPath="no" />
                <RegistryValue Root="HKCU" Key="Software\$manufacturer\$productName\Components" Name="$($file.Name)" Type="integer" Value="1" KeyPath="yes" />
            </Component>
"@
}

# --- 3. 出力 ---
Write-Host "`n==== [1] FILE COMPONENTS (Relative Path Mode) ====" -ForegroundColor Cyan
$fileComponents | Out-String | Write-Host

Write-Host "`n==== [2] DIRECTORY DEFINITIONS ====" -ForegroundColor Yellow
$directoryDefinitions | Out-String | Write-Host

Write-Host "`n==== [3] CLEANUP COMPONENTS ====" -ForegroundColor Green
$cleanupComponents | Out-String | Write-Host