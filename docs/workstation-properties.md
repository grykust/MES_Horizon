# Właściwości stanowisk

Model Domain definiuje wartości cech, w tym zestawy opcji słownikowych.
Obecnie dane przechowuje prototyp w pamięci; docelowe mapowanie ADO.NET będzie jawne.

- PropertyDefinition: kod, nazwa, opis, typ danych i opcjonalna jednostka.
- PropertyOption: opcja słownikowa należąca do jednej definicji.
- WorkstationTypeProperty: dostępność cechy dla rodzaju stanowiska i IsRequired.
- WorkstationProperty: wartość cechy konkretnego stanowiska.

Przykład: definicja SILA_ZWARCIA ma typ Number i jednostkę kN. Przypisanie
do rodzaju Wtryskarka udostępnia tę cechę. Stanowisko zapisuje np. NumberValue = 1200.
Przy typie Option stanowisko zapisuje OptionId wskazujące wybraną opcję.
Przy typie MultiOption zapisuje listę OptionIds. Przykłady konfiguracji:

- LOCATION / Lokalizacja: Option, np. Hala A, Hala B; jeden wybór.
- CENTRE / Centrum produkcyjne: MultiOption, np. Centrum A, Centrum B; wiele wyborów.

Są to zwykłe definicje dodawane w ekranie konfiguracji, bez specjalnego traktowania kodów.
Nie ma osobnych encji lokalizacji, centrum ani relacji WorkstationProductionCentre.
Jedna opcja centrum może występować przy wielu stanowiskach. Rodzaj stanowiska nadal określa dostępne cechy.
W formularzu zestaw opcji jest przesyłany jako powtarzane zaznaczenia; warstwa Web
przekazuje identyfikatory do Application w formie rozdzielonej przecinkami.
Model domenowy przechowuje je jako listę int, nie jako tekst. Przy integracji SQL
listę należy odwzorować na wiersze powiązań opcji wartości; prototyp nie zmienia schematu bazy.

## Prototyp Application i Web

WorkstationCatalogService obsługuje dodawanie rodzajów i definicji z opcjami,
przypisywanie cech do rodzajów, tworzenie stanowisk i zapis kompletu wartości stanowiska.
Razor Pages znajdują się pod /Workstations, /Workstations/Setup, /Workstations/Create
i /Workstations/Details/{id}. Formularze POST korzystają z ochrony anty-CSRF i przekierowania po zapisie.

InMemoryWorkstationCatalogStore przechowuje dane tylko w jednym procesie, wspólnie dla
wszystkich odwiedzających prototyp. Blokada obejmuje sprawdzenie warunków i zapis.
Serwis zwraca niezależne DTO. Walidacja całego zestawu wartości następuje przed zmianą stanu.
Zapis wartości zastępuje cały aktywny zestaw cech stanowiska; pominięta opcjonalna wartość
jest miękko usuwana. Przy równoczesnej edycji obowiązuje ostatni poprawny zapis.

Formularze pokazują wymagane cechy i opcjonalne obecne w aktualnym szkicu lub zapisane
przy stanowisku. Pozostałe opcjonalne cechy są dostępne przez „Dodaj właściwość”.
Dodanie/usunięcie pola jest operacją na szkicu formularza; nie zmienia magazynu.
Wpisane wartości pozostałych pól są zachowywane. Zapis wymaga uzupełnienia wszystkich
wybranych cech. Opcjonalną cechę usuwa się przyciskiem „Usuń właściwość”, a nie pustą wartością.
Wymagana cecha nie może zostać usunięta. Brak opcjonalnej cechy w zapisywanym zestawie
oznacza usunięcie przypisania (z audytem miękkiego usunięcia).

Prototyp jest włączony w appsettings.Development.json (WorkstationPrototype:Enabled).
Bez tej flagi strony zwracają 404. Operator demonstracyjny ma ID 1; nie jest to logowanie.
Przed podłączeniem MS SQL należy wdrożyć autoryzację i odczyt operatora z uwierzytelnionej sesji,
zastąpić magazyn prototypu repozytoriami ADO.NET i zapewnić transakcje oraz ograniczenia unikalności.

## Obowiązujące reguły

- Aktywna para WorkstationId + PropertyId jest unikalna.
- Aktywna para WorkstationTypeId + PropertyId jest unikalna.
- Kody definicji są unikalne, a kody opcji są unikalne w obrębie definicji.
- Wartość ma dokładnie jedno wypełnione pole zgodne z DataType lub niepustą listę OptionIds dla MultiOption.
- NumberValue to decimal, DateValue to data kalendarzowa bez czasu.
- OptionId należy do PropertyId; definicja i opcja muszą być aktywne.
- Wszystkie OptionIds należą do tej samej definicji; duplikaty i obce opcje są odrzucane.
- Wybrany MultiOption musi mieć co najmniej jedną opcję. Usunięcie opcjonalnego pola i zapis miękko usuwa wartość.
- Cecha musi być dostępna dla rodzaju danego stanowiska.
- Przy zapisie stanowiska należy sprawdzić komplet cech wymaganych.
- Wymagalność nowo przypisywanej cechy nie może pozostawić istniejących stanowisk bez wartości.
- Opcje podaje się podczas tworzenia definicji słownikowej, po jednej nazwie na linię.
- Prototyp obsługuje do 100 opcji na definicję; rodzaj musi istnieć przed utworzeniem stanowiska.

## Dalsza rozbudowa

Edycja i usuwanie definicji, opcji i rodzajów oraz zmiana rodzaju stanowiska
nie są jeszcze dostępne. Przy dodawaniu tych operacji obowiązują dodatkowo następujące zasady:
- Zmiana rodzaju stanowiska wymaga ponownej walidacji przypisanych cech.
- Zmiana typu definicji używanej przez stanowiska wymaga kontrolowanej konwersji danych.
- Przy usunięciu definicji lub opcji trzeba obsłużyć istniejące odwołania.

AuditableEntity przechowuje autora i datę utworzenia, ostatniej modyfikacji
oraz miękkiego usunięcia. Nie przechowuje poprzednich wartości. Pełna historia
wymaga osobnego dziennika zmian. Entity dziedziczy audyt i dodaje Id, Name i Description.

Schemat i ograniczenia istniejącej bazy należy dopasować oddzielnie; aplikacja nie wykonuje migracji.
