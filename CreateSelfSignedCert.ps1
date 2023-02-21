$certname = "azurefunction"    ## Replace {certificateName}
$cert = New-SelfSignedCertificate -Subject "CN=$certname" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256
Export-Certificate -Cert $cert -FilePath "C:\Users\alexgrover\$certname.cer"   ## Specify your preferr


$pass = Read-Host -AsSecureString
# Export cert to PFX - uploaded to Azure App Service
Export-PfxCertificate -cert $cert -FilePath "C:\Users\alexgrover\$certname.pfx" -Password $pass



## Note the thumbprint
$cert.Thumbprint
