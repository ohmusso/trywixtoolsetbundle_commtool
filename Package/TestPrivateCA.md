# 自己認証局とサーバ証明書

## 自己認証局の証明書作成

``` powershell
# 設定
$dnsName = "TestPrivateRootCA" # 自己認証局の名前を設定
$rootCertExportPath = ".\TestRootCA.cer"

# 自己認証局の証明書作成
$rootCert = New-SelfSignedCertificate -DnsName $dnsName `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(10) `
    -KeyUsage CertSign, CRLSign, DigitalSignature

# 証明書をcerでエクスポート
Export-Certificate -Cert $rootCert -FilePath $rootCertExportPath

# ルート証明機関にインポート
Import-Certificate -FilePath $rootCertExportPath -CertStoreLocation "Cert:\CurrentUser\Root"
```

上記を実行すると、certmgrの"証明書-現在のユーザー\個人\証明書"に$dnsNameに設定した名前で証明書が作成される。

この証明書を"証明書-信頼されたルート証明機関\証明書"にカット&ペーストして、ルート証明書とする。

## 中間証明書作成

``` powershell
# 設定
$rootCertFingerprint = "b56f4cfcf534573710d4375e810e7ae9329f7d9a" # 自己認証局の証明書作成で作成した証明書の拇印
$intermediateCertExportPath = ".\TestIntermediateCA.cer"

# 証明書ストアから自己認証局の証明書を取得
$rootCert = Get-ChildItem -Path Cert:\CurrentUser\Root | Where-Object { $_.Thumbprint -eq $rootCertFingerprint }

if($rootCert -eq $null) {Write-Host "ルート証明書が見つかりませんでした。"; return}

# サーバ証明書を作成(自己認証局の証明書で署名)
$intermediateCert = New-SelfSignedCertificate -DnsName "TestIntermediateCA" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Signer $rootCert `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyUsage CertSign, CRLSign, DigitalSignature `
    -TextExtension @("2.5.29.19={text}ca=1&pathlength=0") # CA権限の付与

# 証明書をcerでエクスポート
Export-Certificate -Cert $intermediateCert -FilePath $intermediateCertExportPath

# 中間証明機関にインポート
Import-Certificate -FilePath $intermediateCertExportPath -CertStoreLocation "Cert:\CurrentUser\CA"
```

## 自己認証局で署名したサーバ証明書作成

``` powershell
# 設定
$intermediateCertFingerprint = "922be50f63715b1c7cead452ec59e854df038f8a" # 中間証明書の拇印
$serverCertFriendlyName = "TestServerCertificate" # サーバ証明書のフレンドリ名
$serverCertExportPath = ".\TestSever.cer"

# 証明書ストアから自己認証局の証明書を取得
$intermediateCert = Get-ChildItem -Path Cert:\CurrentUser\CA | Where-Object { $_.Thumbprint -eq $intermediateCertFingerprint }

if($intermediateCert -eq $null) {Write-Host "中間証明書が見つかりませんでした。"; return}

# サーバ証明書を作成(中間証明書で署名)
$serverCert = New-SelfSignedCertificate -DnsName "myserver.local" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Signer $intermediateCert `
    -FriendlyName $serverCertFriendlyName

# 証明書をcerでエクスポート
Export-Certificate -Cert $serverCert -FilePath $serverCertExportPath
```

## msiパッケージから証明書をインストール

wix拡張機能  Internet Information Services Extension のcertificateを使用する。

<https://docs.firegiant.com/wix/schema/iis/certificate/>
