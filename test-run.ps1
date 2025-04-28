# build the project
dotnet build ".\AzureDevOps.WikiPdfExport\AzureDevOps.WikiPdfExport.csproj"

# get the test folders
$wikis = Get-ChildItem -Recurse .\AzureDevOps.WikiPdfExport.Test\Tests -Depth 0 | Where-Object { $_.PSIsContainer }

$results = @()

# todo: read the previous result file to compare deltas
# todo: find a way to compare pdfs to find differences, maybe https://github.com/vslavik/diff-pdf

#iterate over each wiki in the test folder
foreach($wiki in $wikis)
{
    Write-Output "Running: $($wiki.FullName)"

    # run the converter
    $output = dotnet run --project ".\AzureDevOps.WikiPdfExport\AzureDevOps.WikiPdfExport.csproj" -- -p $wiki.FullName -o ".\tests\$($wiki.Name).pdf" --disableTelemetry

    # extrac the time
    $export = $output  | Where-Object {$_ -match 'Export done in (\d+):(\d+):(\d+).(\d+)'}
    $null = $export -match 'Export done in (\d+):(\d+):(\d+).(\d+)'
    $hours = $matches[1]
    $minutes = $matches[2]
    $seconds = $matches[3]
    $millis = $matches[4]

    Write-Output "$($wiki.Name): $($hours):$($minutes):$($seconds):$($millis)"

    # persist the time for later comparision
    $obj = [PSCustomObject]@{
        Name     = $wiki.Name
        Hours    = $hours
        Minutes  = $minutes
        Seconds  = $seconds
        Millis   = $millis
    }

    $results += $obj
}

# save the new results
$results | ConvertTo-Json | Set-Content -Path .\tests\results.json
