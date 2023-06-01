

$subId = "b3087308-e129-438c-94cf-7812be946e2b"
$resourceGroupName = "test-fileupload"
$functionAppName = "uploadedarchive"
$zipFilePath = "published\ArchiveFile.zip"

Connect-AzAccount

Set-AzContext -SubscriptionId $subId

$functionApp = Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $functionAppName
[xml]$publishingProfile = Get-AzWebAppPublishingProfile -WebApp $functionApp

$zipProfile = $publishingProfile.publishData.publishProfile | where { $_.publishMethod -eq "ZipDeploy" }

$uri = "https://{0}/api/zipdeploy" -f $zipProfile.publishUrl
$headers = @{
    Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $zipProfile.userName, $zipProfile.userPWD)))
}

Invoke-RestMethod -Uri $uri -Headers $headers -Method Post -InFile $zipFilePath -ContentType "application/octet-stream"
