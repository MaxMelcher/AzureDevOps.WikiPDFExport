#build the project
dotnet build ".\AzureDevOps.WikiPDFExport\azuredevops-export-wiki.csproj"

#get the test folders
$wikis = Get-ChildItem -Recurse .\AzureDevOps.WikiPDFExport.Test\Tests -Depth 0 | ?{ $_.PSIsContainer }

$results = @()

#todo: read the previous result file to compare deltas
#todo: find a way to compare pdfs to find differences, maybe https://github.com/vslavik/diff-pdf

#iterate over each wiki in the test folder
foreach($wiki in $wikis)
{
    Write-Output "Running: $($wiki.FullName)"

    #run the converter
    $output = dotnet run --project ".\AzureDevOps.WikiPDFExport\azuredevops-export-wiki.csproj" -- -p $wiki.FullName -o ".\tests\$($wiki.Name).pdf" > $null

    #extrac the time
    $export = $output  | ? {$_ -match 'Export done in (\d+):(\d+):(\d+).(\d+)'}
    $null = $export -match 'Export done in (\d+):(\d+):(\d+).(\d+)'
    $hours = $matches[1]
    $minutes = $matches[2]
    $seconds = $matches[3]
    $millis = $matches[4]

    Write-Output "$wiki.Name: $($hours):$($minutes):$($seconds):$($millis)"

    #persist the time for later comparision
    $obj = [PSCustomObject]@{
        Name     = $wiki.Name
        Hours    = $hours
        Minutes  = $minutes
        Seconds  = $seconds
        Millis   = $millis
    }

    $results += $obj
}

#save the new results
$results | ConvertTo-Json | Set-Content -Path .\tests\results.json