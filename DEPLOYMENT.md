# Wdrożenie produkcyjne

## Wymagania

Projekt jest aplikacją ASP.NET Core MVC (.NET 10) i wymaga działającego procesu serwerowego. Netlify może hostować statyczny frontend, ale nie uruchomi bezpośrednio tego projektu MVC. Dla obecnej architektury użyj hostingu obsługującego kontenery .NET, np. Azure App Service for Containers, Render lub Railway.

## Kontener

Repozytorium zawiera `Dockerfile`. Host powinien budować obraz z katalogu głównego repozytorium i udostępnić port `8080`. Zamontuj trwały wolumen pod `/app/data`, ponieważ baza SQLite jest przechowywana w `/app/data/autopartstore.db`.

W ustawieniach hostingu skonfiguruj sekrety jako zmienne środowiskowe:

| Zmienna | Opis |
|---|---|
| `Smtp__Enabled` | `true`, aby wysyłać wiadomości |
| `Smtp__Host` | Host SMTP |
| `Smtp__Port` | Port SMTP |
| `Smtp__Username` | Login SMTP |
| `Smtp__Password` | Hasło aplikacji SMTP |
| `Smtp__EnableSsl` | `true` dla TLS |
| `Smtp__From` | Adres nadawcy |

Nie dodawaj haseł ani produkcyjnego `appsettings.json` do repozytorium. Migracje bazy są stosowane przy starcie aplikacji.

## Płatności

Symulator płatności działa wyłącznie w Development. W Production zamówienie nie zostanie oznaczone jako opłacone, dopóki nie zostanie zintegrowany operator płatności. Przed przyjmowaniem prawdziwych zamówień wdroż integrację z operatorem i użyj jego potwierdzenia serwer-serwer.

## CI

Workflow `.github/workflows/ci.yml` buduje aplikację i uruchamia automatyczne testy UAT dla push i pull requestów.
