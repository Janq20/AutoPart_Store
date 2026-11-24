# Testy akceptacyjne UAT

## Warunki wstępne

- Uruchom aplikację w środowisku Development z lokalną bazą testową.
- Załóż konta klienta, pracownika i administratora.
- Dodaj co najmniej dwa aktywne produkty z dostępnym stanem magazynowym.
- Do testu wiadomości skonfiguruj działające SMTP.

## Scenariusze

| ID | Scenariusz | Oczekiwany wynik |
|---|---|---|
| UAT-01 | Klient składa zamówienie z poprawnymi danymi płatności i dostawy. | Zamówienie jest zapisane jako opłacone, stan magazynowy maleje, klient widzi potwierdzenie z kwotą. |
| UAT-02 | Klient próbuje kupić ilość większą niż dostępna. | Zamówienie nie powstaje, a stan magazynowy pozostaje bez zmian. |
| UAT-03 | Klient otwiera „Moje zamówienia”. | Widzi wyłącznie własne zamówienia i może pobrać fakturę PDF z polskimi znakami. |
| UAT-04 | Administrator zmienia status zamówienia. | Status zapisuje się i klient otrzymuje e-mail z nowym statusem. |
| UAT-05 | Administrator anuluje zamówienie. | Stan magazynowy zwiększa się o ilości z zamówienia. |
| UAT-06 | Klient lub pracownik otwiera raport sprzedaży. | Dostęp jest zabroniony; raport może otworzyć administrator. |
| UAT-07 | Administrator filtruje raport po dacie. | Raport uwzględnia opłacone i nieanulowane zamówienia z wybranego okresu, a pomija anulowane i nieopłacone. |
| UAT-08 | Pięć kolejnych prób logowania na istniejące konto kończy się błędem. | Konto zostaje czasowo zablokowane; poprawne logowanie resetuje licznik po zakończeniu blokady. |
| UAT-09 | Strony katalogu, koszyka, finalizacji, raportu i zamówień są otwarte na ekranie 360 px. | Treść nie wychodzi poza ekran; tabele można przewijać poziomo, a formularze i przyciski pozostają używalne. |
| UAT-10 | Aplikacja jest uruchomiona w środowisku Production i klient próbuje zapłacić symulatorem. | Symulator nie zatwierdza płatności; do sprzedaży produkcyjnej należy skonfigurować prawdziwego operatora. |

Automatyczne testy raportu i jego autoryzacji uruchom poleceniem:

```powershell
dotnet test Tests\AutoPart.Store.UatTests\AutoPart.Store.UatTests.csproj
```

W środowisku Development aplikacja automatycznie dodaje 50 kont demonstracyjnych (2 administratorów, 8 pracowników i 40 klientów), 120 produktów oraz 438 zamówień rozłożonych na ostatnie 6 miesięcy. Konta demonstracyjne używają hasła `AutoPartDemo!2026`:

- administrator: `admin@autopart.test`
- administrator: `admin2@autopart.test`
- pracownicy: `pracownik01@autopart.test`–`pracownik08@autopart.test`
- klienci: `klient1@autopart.test`–`klient3@autopart.test` i `klient04@autopart.test`–`klient40@autopart.test`

Dane seedera nie są tworzone w środowisku produkcyjnym. Seeder dopisuje brakującą partię demonstracyjną tylko raz i zachowuje istniejące rekordy.
