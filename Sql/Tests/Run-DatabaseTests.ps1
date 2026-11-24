param(
    [string]$Server = '(localdb)\MSSQLLocalDB'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testScriptPath = Join-Path $PSScriptRoot 'ConnectivitySchemaTest.sql'
$schemaScriptPath = Join-Path $projectRoot 'Sql\001_CreateDomainSchema.sql'
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$databaseName = 'AmanatDomainTest_' + $stamp + '_' + [guid]::NewGuid().ToString('N').Substring(0, 6)

if (-not (Test-Path -LiteralPath $testScriptPath)) {
    throw "Missing test script: $testScriptPath"
}
if (-not (Test-Path -LiteralPath $schemaScriptPath)) {
    throw "Missing schema script: $schemaScriptPath"
}

Push-Location $projectRoot
try {
    "TestDatabase: $databaseName"
    & sqlcmd -S $Server -d master -E -b -v "TestDatabaseName=$databaseName" -i $testScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Database verification failed with sqlcmd exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
