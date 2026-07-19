$files = Get-ChildItem -Path src, tests -Recurse -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match 'using ATS\.Application\.Common\.Models;') {
        $content = $content -replace 'using ATS\.Application\.Common\.Models;', "using ATS.Application.Common.Models;`r`nusing ATS.Domain.Common;"
        Set-Content -Path $f.FullName -Value $content -Encoding UTF8
    }
}
