# HT demo MCP (neovisni POC)

Neslužbena simulacija; svi podaci, cijene i statusi su fiktivni. Bez stvarne HT integracije. Prijave se čuvaju samo u RAM-u i nestaju nakon restarta. PostgreSQL nije implementiran u ovoj besplatnoj početnoj verziji.

## Lokalno
Postavi MCP_API_KEY na nasumičan tajni niz od najmanje 32 znaka. Ne commitaj ključ. Pokreni dotnet run (.NET 10 SDK) ili izgradi Docker image. Health: GET /health. MCP: /mcp (Streamable HTTP, Authorization: Bearer <ključ>).

## Render
Web Service, runtime Docker, Free ako je dostupan, Frankfurt, Dockerfile u rootu. Postavi MCP_API_KEY kroz Render Environment kao tajnu. Nakon uspješnog builda provjeri /health i MCP initialize/tools/list s autentifikacijom. U ElevenLabsu konfiguriraj Bearer header kroz tajnu. Za testiranje koristi DEMO-001 ili DEMO-002. Ne preusmjeravaj produkcijski promet bez provjere.

## Ograničenja
Ovo je demo bez stvarnih podataka, stvarnog transfera poziva i trajne pohrane. Lokalni build i integracijski testovi još nisu izvršeni.
