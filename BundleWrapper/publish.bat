dotnet publish  -c release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o .\bin\Release\net10.0\publish\win-x64
copy /Y ".\bin\Release\net10.0\publish\win-x64\BundleWrapper.exe" ".\artifact"
copy /Y "..\Bundle\bin\x64\Release\Bundle.exe" ".\artifact\Bundle"