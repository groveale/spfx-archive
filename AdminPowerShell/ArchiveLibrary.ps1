##############################################
# Description: This script will archive an entire library
#
# Parameters: LibraryName, SiteUrl
# Example: .\ArchiveLibrary.ps1 -LibraryName "Documents" -SiteUrl "http://intranet.contoso.com"

##############################################
# Parameters
##############################################

# param(
#     [Parameter(Mandatory=$true)][string]$LibraryName = "LibraryTesting",
#     [Parameter(Mandatory=$true)][string]$SiteUrl = "https://groverale.sharepoint.com/sites/archivedev"
# )

$LibraryName = "LibraryTesting"
$SiteUrl = "https://groverale.sharepoint.com/sites/archivedev"

##############################################
# Dependencies
##############################################

# PnP PowerShell
Import-Module PnP.PowerShell

##############################################
# Variables
############################################## 

# Archive API Url
$archiveUrl = "https://ag-spfx-archive.azurewebsites.net/api/ArchiveFile"

# Restore API Url
$restoreUrl = "https://ag-spfx-archive.azurewebsites.net/api/RehydrateFile"

$archiveVersions = $false
$archiveVersionCount = 5

# Restore (are we restoring the library?)
$restore = $false


##############################################
# Globals
##############################################

$activity = "Archiving" 

# Activity to restore if we are restoring
if ($restore) { $activity = "Restoring" }

$archiveMethod = "Admin"

##############################################
# Functions
##############################################

function ConnectToSPO($siteUrl) {
    Connect-PnPOnline -Url $siteUrl -Interactive
    $web = Get-PnPWeb
    $ctx = Get-PnPContext
    $ctx.Load($web.CurrentUser)
    $ctx.ExecuteQuery()
    Write-Host "Connected to $($web.Title) as $($web.CurrentUser.Email)"
    return $web
}

function GetSiteId() {
    $siteId = Get-PnPSite -Includes Id | Select-Object -ExpandProperty Id
    return $siteId.Guid
}


function GetLibraryObject($listName, $siteUrl) {

    ## Check Library Exists
    $list = Get-PnPList -Identity $listName -Includes Id

    $action = "Achive"

    if ($restore) { $action = "Restore" }

    if ($list) {
        Write-Host "Please confirm you want to $action $listName (y/n)"
        $confirm = Read-Host

        if ($confirm -eq "y") {
            Write-Host "Archiving $listName"
            
            return $list
        }
        else {
            Write-Host "Exiting"
            exit
        }
    }
    else {
        Write-Host "Library $listName does not exist in $siteUrl"
        exit
    }
}

function GetListItems($listId)
{
    $items = Get-PnPListItem -List $listId -PageSize 2500

    return $items
}

function ProcessItem($item, $libraryId, $web, $siteUrl, $siteId)
{

    if($restore) {
        ## If Item has not been archived, skip
        if (!($item["FileLeafRef"]).EndsWith("_archive.txt")) {
            Write-Host " Item $($item["FileLeafRef"]) has already been restored" -ForegroundColor Yellow
            return
        }
    }
    else {
        ## If Item has already been archived, skip
        if (($item["FileLeafRef"]).EndsWith("_archive.txt")) {
            Write-Host " Item $($item["FileLeafRef"]) has already been archived" -ForegroundColor Yellow
            return
        }
    }
    

    ## HTTP Post Req to Archive API
    $body = @{
        "fileLeafRef" = $item["FileLeafRef"]
        "fileRelativeUrl" = $item["FileRef"]
        "serverRelativeUrl" = $web.ServerRelativeUrl
        "siteUrl" = $siteUrl
        "archiveVersions" = $archiveVersions
        "archiveVersionCount" = $archiveVersionCount
        "archiveMethod" = $archiveMethod
        "archiveUserEmail" = $web.CurrentUser.Email
        ## to build the .spItemURL
        "siteId" = $siteId
        "listId" = $libraryId
        "itemId" = $item.Id
    }

    $jsonBody = $body | ConvertTo-Json

    try {
        ## Post to Archive API

        if ($restore) {
            $archiveUrl = $restoreUrl
        }

        $response = Invoke-RestMethod -Uri $archiveUrl -Method Post -Body $jsonBody -ContentType "application/json"

        Write-Host " Item $($item["FileLeafRef"]) has been successfully archived" -ForegroundColor Green
    }
    catch {
        Write-Host " Item $($item["FileLeafRef"]) has failed to archive" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
    
}

##############################################
# Main
##############################################

# Connect the Site
$web = ConnectToSPO -siteUrl $SiteUrl

# Get the site ID
$siteId = GetSiteId

# Get the library
$list = GetLibraryObject -listName $LibraryName -siteUrl $SiteUrl

# Get the documents
$items = GetListItems -listId $list.Id

# Current Item
$currentItem = 0
# Loop through each item
foreach ($item in $items) {
    ## Show progress to console
    $percent = [Math]::Round(($currentItem / $items.Count) * 100) 
    Write-Progress -Activity "$activity in Progress" -Status "$percent% Complete:" -PercentComplete $percent
    
    ProcessItem -item $item -libraryId $list.Id -siteId $siteId -web $web -siteUrl $SiteUrl

    $currentItem++
}
