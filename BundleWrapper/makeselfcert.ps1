# check certmgr at "Cert:\CurrentUser\My" after execute this script
$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject "CN=TestBundleWrapperCertificate" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(1)
$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject "CN=TestBundleCertificate" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(1)