# MES

Szkielet aplikacji ASP.NET Core Razor Pages + API, .NET 10, ADO.NET i Microsoft.Data.SqlClient.
Istniejąca baza MS SQL jest zarządzana poza aplikacją. Projekt nie tworzy bazy, tabel ani migracji.

## Visual Studio

1. Otwórz MES.sln w Visual Studio obsługującym .NET 10, z obciążeniem ASP.NET i tworzenie aplikacji internetowych.
2. Ustaw MES.Web jako projekt startowy.
3. Uruchom profilem https (F5). Bez konfiguracji bazy działa panel; lista maszyn pokazuje komunikat o braku dostępu.

## Warstwy

- MES.Domain: modele biznesowe.
- MES.Application: serwisy, DTO, interfejsy repozytoriów.
- MES.Infrastructure: połączenia SQL, zapytania i mapowanie SqlDataReader.
- MES.Web: Razor Pages, kontrolery API, konfiguracja dependency injection.

Razor Pages i API korzystają z tego samego serwisu aplikacyjnego.
Przykładowy model maszyny wymaga dopasowania do rzeczywistych danych.

## Połączenie z istniejącą bazą

W Visual Studio kliknij prawym przyciskiem MES.Web -> Zarządzaj wpisami tajnymi użytkownika.
Przykładowa zawartość secrets.json dla środowiska Development:

~~~json
{
  "ConnectionStrings": {
    "MES": "Server=SERWER;Database=BAZA;Integrated Security=True;Encrypt=True;TrustServerCertificate=False"
  }
}
~~~

Dla uwierzytelniania SQL zamiast Integrated Security podaj User ID i Password w sekretach.
Na serwerze ustaw zmienną środowiskową ConnectionStrings__MES.
Nie zapisuj haseł w repozytorium.

W src/MES.Web/appsettings.json uzupełnij Database:MachinesQuery.
Poniższe nazwy są PRZYKŁADOWE, trzeba zastąpić je rzeczywistymi nazwami z bazy:

~~~sql
SELECT TOP (200)
    MachineId AS Id,
    MachineCode AS Code,
    MachineName AS Name
FROM dbo.YourExistingMachineTable
WHERE (@Search IS NULL
    OR CHARINDEX(@Search, MachineCode) > 0
    OR CHARINDEX(@Search, MachineName) > 0)
ORDER BY MachineCode;
~~~

Kontrakt wyniku: Id = SQL int NOT NULL, Code i Name = tekst NOT NULL.
Przy innych typach lub NULL dopasuj SELECT albo mapowanie w MachineRepository.
@Search jest przekazywany jako parametr nvarchar(100), nie jest składany z tekstem SQL.
Zapytanie musi ograniczać liczbę wyników i definiować kolejność.
CommandTimeoutSeconds domyślnie wynosi 30.
Zapytanie jest zaufaną konfiguracją administratora. Korzystaj z konta z uprawnieniami odpowiadającymi potrzebom aplikacji.

## Dostępne adresy

### Prototyp stanowisk i elastycznych właściwości

Uruchom projekt profilem `https` lub `http` z Visual Studio (środowisko Development).
W menu wybierz **Stanowiska** → **Słowniki i właściwości**.

1. Dodaj rodzaj stanowiska (np. Wtryskarka).
2. Dodaj definicję cechy: kod, nazwę, typ danych, opcjonalny opis i jednostkę dla liczb.
   Dla typów słownikowych podaj opcje, każdą w osobnej linii.
   Lokalizację zdefiniuj jako **Słownik** (np. Hala A, Hala B),
   a centrum produkcyjne jako **Słownik — wiele wyborów** (np. Centrum A, Centrum B).
3. Przypisz definicję do rodzaju stanowiska, zaznaczając opcjonalnie wymagalność.
4. Na liście stanowisk wybierz **Dodaj stanowisko**, wybierz rodzaj i uzupełnij formularz.
5. Link **Właściwości** otwiera edycję wartości konkretnego stanowiska.

Formularz pokazuje automatycznie cechy wymagane oraz zapisane cechy opcjonalne.
Pozostałe wybierasz z listy **Dostępne właściwości opcjonalne** przyciskiem **Dodaj właściwość**.
Dodana cecha musi mieć wartość; możesz też wycofać ją przyciskiem **Usuń właściwość**.
Te przyciski zmieniają wyłącznie formularz, zachowując pozostałe wpisane dane.
Dopiero **Utwórz stanowisko** albo **Zapisz wartości** zatwierdza zmiany.
Wymaganych cech nie można usuwać.

Dostępne typy to tekst, liczba dziesiętna, Tak/Nie, data, słownik i słownik z wielokrotnym wyborem.
Lokalizacja i centrum produkcyjne są właściwościami; ich wymagalność ustalasz przy przypisaniu do rodzaju.
Nowe opcjonalne cechy można przypisać do rodzaju już po utworzeniu stanowisk.
Wymagalność można włączyć po uzupełnieniu wartości we wszystkich stanowiskach tego rodzaju.

**Dane prototypu są w pamięci i znikają po restarcie.** Wszystkie operacje używają
operatora demonstracyjnego ID 1. Moduł jest włączony przez `WorkstationPrototype:Enabled`
w konfiguracji Development; nie jest podłączony do SQL ani systemu logowania.
Dokładne zasady opisuje [model właściwości](docs/workstation-properties.md).

Test przepływu HTTP po kompilacji Release (PowerShell 7):

~~~powershell
pwsh -NoProfile -File scripts/Test-WorkstationPrototype.ps1
~~~

### Moduł maszyn (SQL)

- / - panel
- /Machines - lista i wyszukiwanie
- /api/v1/machines?search=ABC - JSON
- Brak konfiguracji / błąd bazy: API zwraca 503 ProblemDetails.
- Wyszukiwanie powyżej 100 znaków: API zwraca 400.

## CLI

~~~powershell
dotnet restore MES.sln
dotnet build MES.sln -c Release
dotnet run --project src/MES.Web --launch-profile https
~~~

Nie skonfigurowano jeszcze uwierzytelniania użytkowników ani uprawnień biznesowych.
Przed udostępnieniem aplikacji należy dopasować je do środowiska organizacji.
Nie wykonano testu połączenia z rzeczywistą bazą: nie podano połączenia ani schematu.
