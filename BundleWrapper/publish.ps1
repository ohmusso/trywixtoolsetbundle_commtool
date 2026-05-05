# 事前にvisual studioでReleaseビルドを実行する

# 出力文字コードを一時的にUTF-8（BOMなし）に変更
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

dotnet publish  -c release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o .\bin\Release\net10.0\publish\win-x64
Copy-Item -Path ".\bin\Release\net10.0\publish\win-x64\BundleWrapper.exe" -Destination ".\artifact\" -Force
Copy-Item -Path "..\Bundle\bin\x64\Release\Bundle.exe" -Destination ".\artifact\Bundle" -Force

. ".\signing.ps1"
