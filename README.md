# HT Customer Support MCP

Neovisna aplikacija za glasovnu podršku. Svi korisnički zapisi, adrese, identifikatori, tarife, zaposlenici i statusi su sintetički. Nema stvarne HT integracije, autentikacije krajnjeg korisnika, transfera, rezervacije termina ili slanja e-maila. Koristi samo podatke interne evidencije. E-mail adrese koriste rezerviranu domenu example.invalid.

## Arhitektura
MCP Tools → CustomerSupportService → ICustomerRepository → InMemoryCustomerRepository → JSON seed.
Podaci se učitavaju pri pokretanju. Prijave i outbox zapisi nastali tijekom rada čuvaju se samo u RAM-u; restart vraća početni seed. Nema PostgreSQL ni zapisivanja u JSON.

## Seed
- Data/customers.json: 50
- Data/contracts.json: 80
- Data/invoices.json: 300
- Data/tariffs.json: 15
- Data/services.json: 20
- Data/customer-services.json: 100
- Data/employees.json: 20
- Data/troubleshooting.json: 40
- Data/outages.json: 10
- Data/tickets.json: 30

## Pokretanje
.NET 10 SDK: postavi MCP_API_KEY na nasumičan tajni niz od najmanje 32 znaka, zatim dotnet run. Docker: Dockerfile u rootu. GET /health, MCP /mcp (Streamable HTTP, Authorization: Bearer token). Na Renderu konfiguriraj MCP_API_KEY kao tajnu; ne commitaj ključ.

## MCP alati
Zadržani su postojeći nazivi i parametri za ElevenLabs. Prošireni opcionalni argumenti: search_customers (customerId, firstName, lastName, address, invoiceNumber, contractNumber; najmanje jedan), get_troubleshooting_steps (service, symptom), find_available_employees (category, area), create_support_ticket (customerId, summary, confirmed, category, employeeId). Dodani list_service_catalog i get_support_tickets. Neki odgovori sada sadrže niz zapisa umjesto samo prvog rezultata.
Primjeri: 710000001 (Ana Anić, Zagreb), 710000002 (Marko Horvat, Osijek), UG-001, RAC-001. Imena i prezimena u seedu su izmišljene kombinacije za prirodniji razgovor, ne predstavljaju stvarne osobe ni zaposlenike HT-a; podudarnosti sa stvarnim osobama su slučajne. Identifikatori, adrese i e-mailovi ostaju sintetički. search_customers vraća najviše 20 zapisa. Ne predstavlja autentikaciju; sva čitanja koriste isključivo fiktivne zapise.
create_support_ticket zahtijeva confirmed=true, sažetak do 500 znakova, poznatog korisnika i dostupnog zaposlenika; automatski stvara e-mail outbox zapis, EmailSent=false. Nema stvarne dostave niti trajne pohrane. Ne ponavljati automatski poziv koji mijenja stanje.
Kategorije zaposlenika: internet, mobile, tv, billing, contracts, sales, security, complaints, field, general.
 
## Provjera
python3 scripts/validate_seed.py provjerava seed količine, jedinstvenost i referencijalni integritet. dotnet build provjerava C# kompilaciju. Nakon deploya provjeriti MCP tools/list, dohvat 710000001/710000002, račun RAC-001, ugovor UG-001, provjeru područja iz interne evidencije i kreiranje prijave uz potvrdu. Ne koristiti stvarne podatke.

## Microsoft Teams
See [TeamsApp/README.md](TeamsApp/README.md) for the channel tab package and installation. This is a public synthetic-data application, not a native Teams calling bot.

## Relocation service
Six additional tools: check_address_availability, check_relocation_eligibility, calculate_relocation_cost, get_installation_slots, create_relocation_request, schedule_customer_callback. Coverage is exact-match against Data/coverage.json; unknown addresses require review. Internal eligible relocation fee is EUR 29.90. Installation windows are weekdays from five days ahead within a 60-day horizon, reserved atomically in memory. Explicit confirmation is required for mutations; duplicate relocation and callback requests return existing records. Pending callbacks are records, not actual outbound calls. No real carrier coverage, pricing or contract changes are represented. The three synthetic addresses cover available, unavailable and survey-required scenarios; Ivana's destination is Ulica mirnih vrtova 28, 34000 Požega. Requests remain in memory and reset on restart.

## Renewal and retention
Internal campaign for an active Internet L contract expiring within 30 days, with no outstanding balance. Standard renewal EUR 34.90/month, 24 months; explicit switch intent permits retention offers: PS5 Digital Edition EUR 39.90/month or Galaxy A56 5G EUR 34.90/month, each with no separate device charge. `get_renewal_offers` returns full terms; `create_renewal_request` requires acceptance, records one request per contract, pending signature. Paper contract delivery requested to the stored address; device delivery planned within five calendar days after signature, no actual dispatch. Synthetic campaign and in-memory requests; no live carrier changes. UG-002 expires 2026-12-11.
