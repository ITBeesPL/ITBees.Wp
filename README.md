# ITBees.Wp - konektor do WordPressa (REST API wp/v2)

Biblioteka .NET 8 do zarządzania stronami (`pages`) w WordPressie **7.x** przez wbudowane REST API
(`/wp-json/wp/v2/`). Pozwala z poziomu aplikacji .NET:

- tworzyć nowe strony (`POST /wp/v2/pages`) - szkic, publikacja, zaplanowanie, strona prywatna,
- aktualizować istniejące strony (`POST /wp/v2/pages/{id}`) - aktualizacja częściowa: zmienia się tylko to, co ustawisz,
- tworzyć **albo** aktualizować stronę po jej slugu (`CreateOrUpdateBySlugAsync`) - aplikacja „jest właścicielem"
  strony o stałej nazwie (np. `regulamin`, `cennik`) i może ją publikować ponownie bez pamiętania id,
- pobierać stronę po id lub slugu, listować z filtrami (status, rodzic, wyszukiwanie, sortowanie) i stronicowaniem,
- przenosić strony do kosza lub kasować trwale,
- sprawdzić połączenie: indeks API (`GET /wp-json/`) oraz zalogowanego użytkownika (`GET /wp/v2/users/me`).

Uwierzytelnianie: **Application Passwords** (w rdzeniu WordPressa od 5.6, Basic auth po HTTPS) albo token Bearer
z wtyczki JWT. Zero zależności od EF Core i stacka FAS - czysty klient HTTP, użyteczny w dowolnej aplikacji .NET.
Tryb wielu witryn (`IWpClientFactory`) dla hostów, które trzymają dane dostępowe klientów w bazie.

## Wymagania po stronie WordPressa

- WordPress 7.x (API `wp/v2` i Application Passwords są częścią rdzenia; biblioteka nie używa żadnych wtyczek).
- Witryna po **HTTPS** - WordPress domyślnie nie przyjmuje Application Passwords po zwykłym HTTP
  (wyjątek: środowisko lokalne). Biblioteka też odmawia wysłania poświadczeń na adres `http://` - dla lokalnej
  witryny deweloperskiej trzeba świadomie ustawić `AllowInsecureHttp = true`.
- Użytkownik z rolą **Redaktor** lub **Administrator** (uprawnienie `edit_pages`). Hasło aplikacji generuje się
  w WP Admin → Użytkownicy → Profil → „Hasła aplikacji". WordPress pokazuje je ze spacjami - można je wkleić
  w tej postaci.
- Włączone „ładne” bezpośrednie odnośniki (`/wp-json/...`). Dla witryn bez nich ustaw `UseRestRouteQuery = true`
  (adresy `/?rest_route=/wp/v2/...`).

## Szybki start

```csharp
// Program.cs
services.AddITBeesWp(configuration); // sekcja "Wp"
```

```json
{
  "Wp": {
    "SiteUrl": "https://example.com",
    "Username": "redaktor",
    "ApplicationPassword": "abcd efgh ijkl mnop qrst uvwx",
    "HttpTimeoutSeconds": 100
  }
}
```

```csharp
public class CennikPublisher
{
    private readonly IWpPageService _wpPageService;

    public CennikPublisher(IWpPageService wpPageService) => _wpPageService = wpPageService;

    public async Task PublishAsync(string html)
    {
        var page = await _wpPageService.CreateOrUpdateBySlugAsync(new WpPageIm
        {
            Slug = "cennik",
            Title = "Cennik",
            Content = html,
            Status = WpPageStatuses.Publish
        });

        Console.WriteLine($"{page.Id} -> {page.Link}");
    }
}
```

## Tworzenie i aktualizacja stron

```csharp
// Nowa strona - bez Status powstaje szkic
var created = await wpPageService.CreateAsync(new WpPageIm
{
    Title = "O nas",
    Content = "<!-- wp:paragraph --><p>Treść</p><!-- /wp:paragraph -->", // zwykły HTML też działa
    Slug = "o-nas",
    Status = WpPageStatuses.Publish,
    Parent = 12,          // strona nadrzędna (0 = najwyższy poziom)
    MenuOrder = 3,
    Template = "",        // szablon motywu, "" = domyślny
    CommentStatus = WpDiscussionStatuses.Closed
});

// Aktualizacja częściowa - wysyłane są tylko ustawione pola
var updated = await wpPageService.UpdateAsync(created.Id, new WpPageUm
{
    Content = created.Content.Raw + "<p>Dopisek</p>",
    Status = WpPageStatuses.Private
});

// Publikacja zaplanowana
await wpPageService.UpdateAsync(created.Id, new WpPageUm
{
    Status = WpPageStatuses.Future,
    Date = new DateTime(2026, 11, 1, 8, 0, 0) // czas w strefie witryny
});
```

Odpowiedzi (`WpPage`) przychodzą w kontekście `edit`: `Title.Raw` / `Content.Raw` to wartości z bazy WordPressa,
`Title.Rendered` / `Content.Rendered` - HTML po renderowaniu bloków i shortcode'ów. Pola `date`, `date_gmt`,
`modified` są mapowane na `DateTime?` (dla szkiców `DateGmt` bywa null).

## Odczyt i listowanie

```csharp
var page = await wpPageService.GetAsync(21);                 // null, gdy nie istnieje
var bySlug = await wpPageService.GetBySlugAsync("regulamin"); // szuka we wszystkich statusach poza koszem

var drafts = await wpPageService.GetPaginatedAsync(new WpPageQuery
{
    Status = new List<string> { WpPageStatuses.Draft, WpPageStatuses.Pending },
    Search = "regulamin",
    OrderBy = WpPageOrderBy.Modified,
    Order = WpSortOrder.Desc,
    Page = 1,
    PerPage = 50 // WordPress pozwala maks. 100
});
Console.WriteLine($"{drafts.Total} stron, {drafts.TotalPages} stron wyników");

var all = await wpPageService.GetAllAsync(new WpPageQuery { Status = new List<string> { WpPageStatuses.Any } });
```

`WpPageQuery.Context` domyślnie to `Edit` (pełne dane, wymaga uprawnień). Do anonimowego odczytu opublikowanych
stron użyj `WpContext.View` i `AuthMode = WpAuthMode.None`.

Slug musi być pojedynczym slugiem WordPressa - bez przecinków i białych znaków (REST API potraktowałoby je jako
listę kilku slugów, np. `nowa,regulamin` dopasowałby też stronę `regulamin`). Taka wartość kończy się
`ArgumentException` zanim cokolwiek zostanie wysłane, a `GetBySlugAsync` dodatkowo porównuje slug zwróconej strony
z żądanym, więc `CreateOrUpdateBySlugAsync` nigdy nie nadpisze innej strony niż ta, o którą prosisz.

## Usuwanie

```csharp
var trashed = await wpPageService.DeleteAsync(21);              // do kosza, zwraca stronę ze statusem "trash"
var deleted = await wpPageService.DeleteAsync(21, force: true); // trwale, zwraca ostatni stan strony
```

## Niższy poziom: `IWpApiClient`

`IWpPageService` opiera się na `IWpApiClient` - cienkim wrapperze 1:1 na endpointy REST (`GetSiteInfoAsync`,
`GetCurrentUserAsync`, `GetPageAsync`, `GetPagesAsync`, `CreatePageAsync`, `UpdatePageAsync`, `DeletePageAsync`).
Używaj go, gdy potrzebujesz surowego dostępu lub chcesz sprawdzić połączenie, np. w panelu administracyjnym:

```csharp
var site = await wpApiClient.GetSiteInfoAsync();   // działa bez logowania
var me = await wpApiClient.GetCurrentUserAsync();  // 401 = złe dane dostępowe
Console.WriteLine($"{site.Name}: wp/v2={site.HasWpV2Namespace}, app passwords={site.SupportsApplicationPasswords}, user={me.Username} [{string.Join(",", me.Roles ?? [])}]");
```

## Błędy

Każda odpowiedź inna niż 2xx (oraz odpowiedź, która nie jest JSON-em - np. strona HTML z wtyczki bezpieczeństwa
lub trybu konserwacji) kończy się wyjątkiem `WpApiException` z polami `HttpStatusCode`, `WpErrorCode`
(np. `rest_cannot_create`, `rest_post_invalid_id`, `rest_forbidden_context`) i `ResponseBody`. Wyjątek: `GetAsync` /
`GetPageAsync` zwraca `null` dla 404.

Najczęstsze przyczyny:

| Kod | Przyczyna |
|---|---|
| 401 `rest_cannot_create` / `rest_cannot_edit` | złe hasło aplikacji, witryna po HTTP, użytkownik bez roli Redaktor/Administrator |
| 401 `rest_forbidden_context` | kontekst `edit` bez uprawnień - użyj `WpContext.View` |
| 400 `rest_invalid_param` | np. `PerPage` > 100 albo nieznany status |
| 404 `rest_no_route` | wyłączone REST API lub zły `RestApiPath`; dla witryn bez „ładnych” odnośników ustaw `UseRestRouteQuery` |

## Wiele witryn w jednym procesie

Gdy host obsługuje wielu klientów, każdy ze swoim WordPressem, zarejestruj samą fabrykę i buduj konektory
z opcji odczytanych np. z bazy:

```csharp
services.AddITBeesWpClientFactory(); // zamiast AddITBeesWp(...)

var pageService = wpClientFactory.CreatePageService(new WpOptions
{
    SiteUrl = customer.WordPressUrl,
    Username = customer.WordPressUser,
    ApplicationPassword = customer.WordPressApplicationPassword
});
```

`AddITBeesWp(...)` rejestruje fabrykę również, więc oba tryby mogą współistnieć.

## Konfiguracja (`WpOptions`)

| Pole | Domyślnie | Opis |
|---|---|---|
| `SiteUrl` | - | adres witryny bez `/wp-json` |
| `RestApiPath` | `wp-json` | prefiks REST API |
| `UseRestRouteQuery` | `false` | `true` dla witryn bez „ładnych” odnośników (`/?rest_route=/...`) |
| `AuthMode` | `ApplicationPassword` | `ApplicationPassword`, `BearerToken` (wtyczka JWT) lub `None` (tylko odczyt publiczny) |
| `Username`, `ApplicationPassword` | - | login i hasło aplikacji |
| `BearerToken` | - | token dla `AuthMode = BearerToken` |
| `AllowInsecureHttp` | `false` | zgoda na wysyłanie poświadczeń na `http://` - tylko dla lokalnej witryny deweloperskiej |
| `HttpTimeoutSeconds` | `100` | limit czasu żądania |

Opcje są sprawdzane przy pierwszym użyciu klienta (nie przy starcie hosta) - niepełna sekcja `Wp` nie blokuje
uruchomienia aplikacji, a błąd pojawia się dopiero przy wywołaniu WordPressa.

## Test na prawdziwej witrynie

Projekt `WpTestConsoleApp` sprawdza dane dostępowe, tworzy (lub aktualizuje) szkic strony o slugu
`itbees-wp-test` i dopisuje do niego treść:

```
WpTestConsoleApp https://example.com redaktor "abcd efgh ijkl mnop qrst uvwx" [slug]
```

## Testy

`ITBees.Wp.Tests` (xunit) - klient i serwis testowane na podstawionym `HttpMessageHandler`: budowanie adresów
(w tym `rest_route`), nagłówek Basic/Bearer, JSON snake_case z pominięciem nulli, odczyt nagłówków
`X-WP-Total`, obsługa 404 i błędów WordPressa, upsert po slugu, stronicowanie `GetAllAsync`, rejestracja DI.

```
dotnet test
```
