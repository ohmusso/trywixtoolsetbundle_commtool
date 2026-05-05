# before make certification at windows cert store by doing makeselefcert.ps1

# set sign tool path
$signtoolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64"
$env:Path = "$signtoolPath;" + $env:Path

# get cert hash
$subjectName = "TestBundleWrapperCertificate"
$certBundleWrapper = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -match "CN=$subjectName" }
$certBundleWrapper.Thumbprint

# sign
# Bundle.exe is signed when building Bundle
signtool sign /sha1 $certBundleWrapper.Thumbprint /tr http://timestamp.digicert.com /td sha256 /fd sha256 ".\artifact\BundleWrapper.exe"
