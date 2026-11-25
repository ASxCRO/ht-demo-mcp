# HT demo MCP (neovisni PoC)

Neslužbena demonstracija. Svi korisnici, adrese, identifikatori, tarife, zaposlenici i statusi su sintetički. Nema stvarne HT integracije, autentikacije krajnjeg korisnika, transfera, rezervacije termina ili slanja e-maila. Koristi samo demo podatke. E-mail adrese koriste rezerviranu domenu example.invalid.

## Arhitektura
MCP Tools → DemoSupportService → IDemoRepository → InMemoryDemoRepository → JSON seed.
Podaci se učitavaju pri pokretanju. Prijave i simulirani outbox zapisi nastali tijekom rada čuvaju se samo u RAM-u; restart vraća početni seed. Nema PostgreSQL ni zapisivanja u JSON.

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

## Demo alati
Zadržani su postojeći nazivi i parametri za ElevenLabs. Prošireni opcionalni argumenti: search_customers (customerId, firstName, lastName, address, invoiceNumber, contractNumber; najmanje jedan), get_troubleshooting_steps (service, symptom), find_available_employees (category, area), create_support_ticket (customerId, summary, confirmed, category, employeeId). Dodani list_service_catalog i get_support_tickets. Neki odgovori sada sadrže niz zapisa umjesto samo prvog rezultata.
Primjeri: DEMO-001, DEMO-002, UG-001, RAC-001. search_customers vraća najviše 20 zapisa. Ne predstavlja autentikaciju; sva čitanja koriste isključivo fiktivne zapise.
create_support_ticket zahtijeva confirmed=true, sažetak do 500 znakova, poznatog korisnika i dostupnog zaposlenika; automatski stvara simulirani e-mail outbox zapis, EmailSent=false. Nema stvarne dostave niti trajne pohrane. Ne ponavljati automatski poziv koji mijenja stanje.
Kategorije zaposlenika: internet, mobile, tv, billing, contracts, sales, security, complaints, field, general.
 
## Provjera
python3 scripts/validate_seed.py provjerava seed količine, jedinstvenost i referencijalni integritet. dotnet build provjerava C# kompilaciju. Nakon deploya provjeriti MCP tools/list, dohvat DEMO-001/002, račun RAC-001, ugovor UG-001, provjeru demo područja i kreiranje prijave uz potvrdu. Ne koristiti stvarne podatke.
