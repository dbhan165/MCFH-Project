# Cập nhật fb_cookie.json và tiktok_cookie.json từ file Word nhóm chia sẻ.
# Cách dùng:
#   powershell -ExecutionPolicy Bypass -File .\Scripts\UpdateCookiesFromDocx.ps1 -DocxPath "C:\path\Cookie FB+TT.docx"
param(
    [Parameter(Mandatory = $true)]
    [string]$DocxPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $DocxPath)) {
    Write-Error "Không tìm thấy file: $DocxPath"
}

$outDir = Join-Path $PSScriptRoot '..\cookies' | Resolve-Path
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($DocxPath)
$entry = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
if (-not $entry) {
    $zip.Dispose()
    Write-Error 'File .docx không có word/document.xml'
}

$sr = New-Object System.IO.StreamReader($entry.Open())
$xml = $sr.ReadToEnd()
$sr.Close()
$zip.Dispose()

$text = [regex]::Replace($xml, '<w:tab[^/]*/>', "`t")
$text = [regex]::Replace($text, '</w:p>', "`n")
$text = [regex]::Replace($text, '<[^>]+>', '')
$text = [System.Net.WebUtility]::HtmlDecode($text)

function Get-JsonBlock([string]$content, [string]$marker) {
    $start = $content.IndexOf($marker)
    if ($start -lt 0) { throw "Không tìm thấy mục '$marker' trong file Word." }

    $jsonStart = $content.IndexOf('[', $start)
    if ($jsonStart -lt 0) { throw "Không tìm thấy JSON sau '$marker'." }

    $depth = 0
    for ($i = $jsonStart; $i -lt $content.Length; $i++) {
        if ($content[$i] -eq '[') { $depth++ }
        elseif ($content[$i] -eq ']') {
            $depth--
            if ($depth -eq 0) {
                return $content.Substring($jsonStart, $i - $jsonStart + 1)
            }
        }
    }

    throw "JSON sau '$marker' không đóng ngoặc đúng."
}

$fbJson = Get-JsonBlock $text 'fb_cookie.json'
$ttJson = Get-JsonBlock $text 'tiktok_cookie.json'

# Validate JSON trước khi ghi
$fbObj = $fbJson | ConvertFrom-Json
$ttObj = $ttJson | ConvertFrom-Json

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $outDir 'fb_cookie.json'), ($fbObj | ConvertTo-Json -Depth 20), $utf8NoBom)
[System.IO.File]::WriteAllText((Join-Path $outDir 'tiktok_cookie.json'), ($ttObj | ConvertTo-Json -Depth 20), $utf8NoBom)

Write-Host "Da cap nhat cookie thanh cong."
Write-Host "  Facebook : $($fbObj.Count) cookie -> $outDir\fb_cookie.json"
Write-Host "  TikTok   : $($ttObj.Count) cookie -> $outDir\tiktok_cookie.json"
Write-Host "Khoi dong lai backend (dotnet run) neu dang chay de scraper doc cookie moi."
