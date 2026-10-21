param([string]$Directory)
$ErrorActionPreference='Stop'
$file=Join-Path $Directory 'RuntimePaths.json'
$temp=$file+'.new'
[IO.File]::WriteAllText($file,'first')
[IO.File]::WriteAllText($temp,'second')
[IO.File]::Replace($temp,$file,($file+'.bak'))
if([IO.File]::ReadAllText($file) -ne 'second' -or [IO.File]::ReadAllText($file+'.bak') -ne 'first'){throw 'first replace failed'}
[IO.File]::WriteAllText($temp,'third')
[IO.File]::Replace($temp,$file,($file+'.bak'))
if([IO.File]::ReadAllText($file) -ne 'third' -or [IO.File]::ReadAllText($file+'.bak') -ne 'second'){throw 'second replace failed'}
Write-Output 'PASS: Windows PowerShell 5.1 atomic replacement and repeated backup update.'
