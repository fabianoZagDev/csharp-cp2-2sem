#Requires -Version 7.0
<#
.SYNOPSIS
    Baixa a planilha de um relatório concluído e a abre no Microsoft Excel (Windows), registrando o conteúdo lido.

.DESCRIPTION
    Comprova que o download é um .xlsx real: o Excel abre o arquivo e lê quantidades e valores como números.
    Requer Microsoft Excel instalado (automação COM). Não grava tokens nem senhas.

.EXAMPLE
    ./scripts/cp3/Invoke-Cp3ExcelCheck.ps1 -ReportId 9bac42f5-b3e3-40b6-a03c-ac1c2fe9919f
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ReportId,
    [string] $BaseUrl = 'http://localhost:5245',
    [string] $Email = 'employee-a@expensehub.local',
    [string] $OutFile = 'docs/cp3/evidencias/E01b-excel-aberto-no-excel.txt'
)

$ErrorActionPreference = 'Stop'

if (Test-Path .env) {
    foreach ($line in Get-Content .env) {
        if ($line -match '^\s*([A-Z_]+)=(.*)$' -and -not [Environment]::GetEnvironmentVariable($Matches[1])) {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
        }
    }
}

$loginBody = @{ email = $Email; password = $env:EXPENSEHUB_USER_PASSWORD } | ConvertTo-Json
$token = (Invoke-RestMethod -Method Post -Uri "$BaseUrl/login" -ContentType 'application/json' -Body $loginBody).accessToken
$file = Join-Path ([IO.Path]::GetTempPath()) "report-$ReportId.xlsx"
$response = Invoke-WebRequest -Uri "$BaseUrl/api/reports/$ReportId/excel" -Headers @{ Authorization = "Bearer $token" } -OutFile $file -PassThru

$out = [System.Collections.Generic.List[string]]::new()
$out.Add('# E01 (complemento) — planilha aberta no Microsoft Excel')
$out.Add((Get-Date -AsUTC -Format o))
$out.Add("> GET /api/reports/$ReportId/excel (Bearer <token omitido>)")
$out.Add("< $($response.StatusCode)  Content-Type: $($response.Headers['Content-Type'])  Content-Disposition: $($response.Headers['Content-Disposition'])")

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
    $out.Add("Aplicativo: Microsoft Excel $($excel.Version)")
    $workbook = $excel.Workbooks.Open($file, 0, $true)
    $sheet = $workbook.Worksheets.Item(1)
    $out.Add("Aba: $($sheet.Name); intervalo usado: $($sheet.UsedRange.Address($false, $false))")

    foreach ($row in 1..$sheet.UsedRange.Rows.Count) {
        $cells = foreach ($column in 1..3) {
            $cell = $sheet.Cells.Item($row, $column)
            $value = $cell.Value2
            if ($null -ne $value) { "$($cell.Address($false, $false))=$value [$($value.GetType().Name)]" }
        }

        if ($cells) { $out.Add('  ' + ($cells -join ' | ')) }
    }

    $out.Add("Soma calculada pelo Excel (SUM C8:C12) = $($excel.WorksheetFunction.Sum($sheet.Range('C8:C12')))")
    $workbook.Close($false)
}
finally {
    $excel.Quit()
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($excel)
    Remove-Item $file -ErrorAction SilentlyContinue
}

$out | Set-Content -Path $OutFile -Encoding utf8
$out
