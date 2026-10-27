param([int]$Port = 5189)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$webRoot = Join-Path $projectRoot 'src/MES.Web'
$baseUri = "http://127.0.0.1:$Port"
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$process = $null
$env:ASPNETCORE_ENVIRONMENT = 'Development'

function Assert-Contains($response, [string]$text) {
    if ($response.StatusCode -ne 200 -or ![System.Net.WebUtility]::HtmlDecode($response.Content).Contains($text)) {
        $summary = [regex]::Match($response.Content, '(?s)<div[^>]*validation-summary-errors[^>]*>.*?</div>').Value
        throw "Oczekiwano tekstu: $text (HTTP $($response.StatusCode)); $([System.Net.WebUtility]::HtmlDecode($summary))"
    }
}
function Read-Page([string]$path) {
    Invoke-WebRequest "$baseUri$path" -WebSession $script:session -SkipHttpErrorCheck
}
function Post-Form([string]$path, [hashtable]$fields, [string]$getPath = '/Workstations/Setup') {
    $fields = $fields.Clone()
    $page = Read-Page $getPath
    $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
    if (!$token) { throw "Brak tokena anty-CSRF: $getPath" }
    $fields['__RequestVerificationToken'] = [System.Net.WebUtility]::HtmlDecode($token)
    $encoded = foreach ($key in $fields.Keys) {
        foreach ($value in @($fields[$key])) {
            [uri]::EscapeDataString($key) + '=' + [uri]::EscapeDataString([string]$value)
        }
    }
    Invoke-WebRequest "$baseUri$path" -Method Post -Body ($encoded -join '&') -ContentType 'application/x-www-form-urlencoded' -WebSession $script:session -SkipHttpErrorCheck
}

try {
    $process = Start-Process dotnet -ArgumentList @('bin/Release/net10.0/MES.Web.dll', '--urls', $baseUri) -WorkingDirectory $webRoot -WindowStyle Hidden -PassThru
    $script:session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw 'Aplikacja zakończyła działanie podczas startu.' }
        try { $response = Read-Page '/Workstations'; $ready = $true; break } catch { Start-Sleep -Milliseconds 200 }
    }
    if (!$ready) { throw 'Aplikacja nie uruchomiła się.' }
    Assert-Contains $response 'Stanowiska produkcyjne'
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Type' @{TypeName='Wtryskarka'}) 'Rodzaj dodany'
    $definitions = @(
        @{PropertyCode='FORCE';PropertyName='Siła';DataType='2';Unit='kN'},
        @{PropertyCode='MAKER';PropertyName='Producent';DataType='1'},
        @{PropertyCode='AUTO';PropertyName='Automatyczna';DataType='3'},
        @{PropertyCode='DATE';PropertyName='Data instalacji';DataType='4'},
        @{PropertyCode='MODE';PropertyName='Tryb';DataType='5';Options="Ręczny`nAutomatyczny"},
        @{PropertyCode='OTHER';PropertyName='Inny słownik';DataType='5';Options='Obca opcja'},
        @{PropertyCode='LOCATION';PropertyName='Lokalizacja';DataType='5';Options="Hala A`nHala B"},
        @{PropertyCode='CENTRE';PropertyName='Centrum produkcyjne';DataType='6';Options="Centrum A`nCentrum B"}
    )
    foreach ($definition in $definitions) { Assert-Contains (Post-Form '/Workstations/Setup?handler=Property' $definition) 'Definicja właściwości dodana' }
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Property' @{PropertyCode='force';PropertyName='Duplikat';DataType='2'}) 'takim kodzie'
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Property' @{PropertyCode='BAD';PropertyName='Błędna';DataType='99'}) 'validation-summary-errors'
    foreach ($id in 1..5) {
        Assert-Contains (Post-Form '/Workstations/Setup?handler=Rule' @{TypeId='1';PropertyId="$id";IsRequired=($id -eq 1).ToString()}) 'Przypisanie zapisane'
    }
    foreach ($id in 7..8) {
        Assert-Contains (Post-Form '/Workstations/Setup?handler=Rule' @{TypeId='1';PropertyId="$id";IsRequired='true'}) 'Przypisanie zapisane'
    }
    $initial = Read-Page '/Workstations/Create?TypeId=1'
    Assert-Contains $initial 'id="property-1"'
    if ($initial.Content.Contains('id="property-2"')) { throw 'Opcjonalna cecha widoczna przed dodaniem.' }
    $draft = @{Code='DRAFT';Name='Robocze';TypeId='1';'Values[1]'='123';AddPropertyId='2'}
    $added = Post-Form '/Workstations/Create?handler=AddProperty' $draft '/Workstations/Create?TypeId=1'
    Assert-Contains $added 'id="property-2"'
    Assert-Contains $added 'value="123"'
    if ((Read-Page '/Workstations').Content.Contains('DRAFT')) { throw 'Dodanie pola zapisało stanowisko.' }
    $draft.Remove('AddPropertyId'); $draft['Values[2]']='Producent roboczy'; $draft['propertyId']='2'
    $removed = Post-Form '/Workstations/Create?handler=RemoveProperty' $draft '/Workstations/Create?TypeId=1'
    if ($removed.Content.Contains('id="property-2"')) { throw 'Nie usunięto opcjonalnego pola.' }
    Assert-Contains $removed 'value="123"'
    $draft['propertyId']='1'
    Assert-Contains (Post-Form '/Workstations/Create?handler=RemoveProperty' $draft '/Workstations/Create?TypeId=1') 'opcjonalną właściwość'
    $fields = @{Code='W-01';Name='Wtryskarka 1';TypeId='1';'Values[1]'='1200,5';'Values[2]'='Engel';'Values[3]'='false';'Values[4]'='2026-10-22';'Values[5]'='1';'Values[7]'='4';'Values[8]'='';'SelectedOptions'=@('8:6','8:7')}
    $missing = $fields.Clone(); $missing['Values[1]']=''
    Assert-Contains (Post-Form '/Workstations/Create' $missing '/Workstations/Create?TypeId=1') 'Uzupełnij'
    $foreignOption = $fields.Clone(); $foreignOption['Values[5]']='3'
    Assert-Contains (Post-Form '/Workstations/Create' $foreignOption '/Workstations/Create?TypeId=1') 'wybierz opcję'
    $result = Post-Form '/Workstations/Create' $fields '/Workstations/Create?TypeId=1'
    Assert-Contains $result 'W-01'
    Assert-Contains $result '1200.5'
    foreach ($id in 6..7) {
        if ($result.Content -notmatch "id=`"property-8-$id`"[^>]*checked") { throw 'Brak zapisanego centrum w formularzu.' }
    }
    Assert-Contains (Post-Form '/Workstations/Create' $fields '/Workstations/Create?TypeId=1') 'takim kodzie'
    $removeDraft = @{'Values[1]'='1200.5';'Values[2]'='Engel';'Values[3]'='false';'Values[4]'='2026-10-22';'Values[5]'='1';'Values[7]'='4';'Values[8]'='';SelectedOptions=@('8:6','8:7');propertyId='2'}
    $removedDraft = Post-Form '/Workstations/Details/1?handler=RemoveProperty' $removeDraft '/Workstations/Details/1'
    if ($removedDraft.Content.Contains('id="property-2"')) { throw 'Pole pozostało w edycji.' }
    Assert-Contains (Read-Page '/Workstations/Details/1') 'Engel'
    $edit = @{'Values[1]'='1500';'Values[3]'='false';'Values[4]'='2026-10-23';'Values[5]'='2';'Values[7]'='5';'Values[8]'='';'SelectedOptions'=@('8:7')}
    Assert-Contains (Post-Form '/Workstations/Details/1' $edit '/Workstations/Details/1') 'Wartości zapisane'
    $updated = Read-Page '/Workstations/Details/1'
    if ($updated.Content.Contains('id="property-2"')) { throw 'Usunięta cecha nadal widoczna po zapisie.' }
    $addDraft = $edit.Clone(); $addDraft['AddPropertyId']='2'
    Assert-Contains (Post-Form '/Workstations/Details/1?handler=AddProperty' $addDraft '/Workstations/Details/1') 'id="property-2"'
    $blank = $edit.Clone(); $blank['Values[2]']=''
    Assert-Contains (Post-Form '/Workstations/Details/1' $blank '/Workstations/Details/1') 'Uzupełnij'
    if ($updated.Content -match 'id="property-8-6"[^>]*checked') { throw 'Usunięte centrum jest nadal zaznaczone.' }
    if ($updated.Content -notmatch 'id="property-8-7"[^>]*checked') { throw 'Brak aktualnego centrum.' }
    $foreignCentre = $edit.Clone(); $foreignCentre['SelectedOptions']=@('8:4')
    Assert-Contains (Post-Form '/Workstations/Details/1' $foreignCentre '/Workstations/Details/1') 'wybierz opcje'
    $duplicateCentre = $edit.Clone(); $duplicateCentre['SelectedOptions']=@('8:7','8:7')
    Assert-Contains (Post-Form '/Workstations/Details/1' $duplicateCentre '/Workstations/Details/1') 'nie mogą się powtarzać'
    $emptyCentre = $edit.Clone(); $emptyCentre.Remove('SelectedOptions')
    Assert-Contains (Post-Form '/Workstations/Details/1' $emptyCentre '/Workstations/Details/1') 'Uzupełnij'
    $invalid = $edit.Clone(); $invalid['Values[1]']='nie-liczba'; $invalid['Values[2]']='Nie zapisuj'
    Assert-Contains (Post-Form '/Workstations/Details/1' $invalid '/Workstations/Details/1') 'podaj liczbę'
    $unchanged = Read-Page '/Workstations/Details/1'
    Assert-Contains $unchanged '1500'
    if ($unchanged.Content.Contains('Nie zapisuj')) { throw 'Błędny zapis zmienił stan.' }
    $invalidDate = $edit.Clone(); $invalidDate['Values[4]']='2026-02-30'
    Assert-Contains (Post-Form '/Workstations/Details/1' $invalidDate '/Workstations/Details/1') 'prawidłową datę'
    $unknown = $edit.Clone(); $unknown['Values[6]']='3'
    Assert-Contains (Post-Form '/Workstations/Details/1' $unknown '/Workstations/Details/1') 'nie jest dostępna'
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Rule' @{TypeId='1';PropertyId='2';IsRequired='true'}) 'Najpierw przypisz'
    $notFound = Read-Page '/Workstations/Details/999'
    if ($notFound.StatusCode -ne 404) { throw 'Oczekiwano 404.' }
    $noToken = Invoke-WebRequest "$baseUri/Workstations/Details/1" -Method Post -Body $edit -WebSession $script:session -SkipHttpErrorCheck
    if ($noToken.StatusCode -ne 400) { throw 'Brak ochrony anty-CSRF.' }
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Rule' @{TypeId='1';PropertyId='8';IsRequired='false'}) 'Przypisanie zapisane'
    $emptyCentre.Remove('Values[8]')
    Assert-Contains (Post-Form '/Workstations/Details/1' $emptyCentre '/Workstations/Details/1') 'Wartości zapisane'
    $cleared = Read-Page '/Workstations/Details/1'
    if ($cleared.Content -match 'id="property-8-[67]"[^>]*checked') { throw 'Centra nie zostały usunięte.' }
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Type' @{TypeName='Bez wymaganych'}) 'Rodzaj dodany'
    Assert-Contains (Post-Form '/Workstations/Setup?handler=Rule' @{TypeId='2';PropertyId='2';IsRequired='false'}) 'Przypisanie zapisane'
    Assert-Contains (Post-Form '/Workstations/Create' @{Code='EMPTY';Name='Bez cech';TypeId='2'} '/Workstations/Create?TypeId=2') 'EMPTY'
    Write-Output 'OK: wymagane pola, dodawanie i usuwanie opcjonalnych w formularzu, zachowanie szkicu, 6 typów wartości, walidacja, atomowy zapis, 404 i anty-CSRF.'
}
finally {
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}
