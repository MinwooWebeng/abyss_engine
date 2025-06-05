Write-Output "Deleting \AbyssUI\AbyssCLI"
Remove-Item D:\unity\AbyssUI\AbyssCLI\* -Recurse

Write-Output "Deleting \AbyssUIBuild\AbyssCLI"
Remove-Item D:\unity\AbyssUIBuild\AbyssCLI\* -Recurse

Write-Output "Copying \Release to \AbyssUI\AbyssCLI"
Copy-Item -Path .\bin\Release\net8.0\* -Destination D:\unity\AbyssUI\AbyssCLI -Recurse

Write-Output "Copying \Releaseto \AbyssUIBuild\AbyssCLI"
Copy-Item -Path .\bin\Release\net8.0\* -Destination D:\unity\AbyssUIBuild\AbyssCLI -Recurse

Write-Output "Copying \ABI to \AbyssUI\Assets\Host\ABI"
Copy-Item -Path .\ABI\* -Destination D:\Unity\AbyssUI\Assets\Host\ABI -Recurse