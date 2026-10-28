# Plán Práce

## 3 hlavné kroky

- implementácia  riešenia s OpenSearch.

- návrh a implementácia vlastného riešenia a experimentálne porovnanie oboch prístupov s databázou.

## Čo má byť výsledkom

1. **Zdokumentovanú existujúcu architektúru COP** a konkrétne scenáre vyhľadávania.
2. **Funkčnú implementáciu OpenSearch + Logstash**, zapojenú cez vyhľadávaciu službu.
3. **Vlastné vyhľadávacie riešenie** využívajúce rovnaké rozhranie `ISearch.
4. **Databázový variant ako referenčné riešenie.** (Aktuálny stav)
5. **Automatizované výkonnostné experimenty**.
6. **Vyhodnotenie výkonu, záťaže, správnosti a škálovateľnosti.**
7. **Priebežne spracovaný text práce**

### Porovnávané varianty

| Variant | Charakteristika |
|---|---|
| **A — MSSQL** | Vyhľadávanie priamo cez existujúcu databázovú cestu. |
| **B — OpenSearch + Logstash** | Primárne vyhľadávanie v OpenSearch, pri prázdnom výsledku fallback do DB; synchronizácia na pozadí. |
| **C — vlastná implementácia** | Vlastné úložisko a vyhľadávacie mechanizmy za rozhraním `ISearch`, s definovanou synchronizáciou a fallbackom. |

---

## Podrobný plán

### 1. Spresnenie rozsahu a požiadaviek

**Cieľ:** presne určiť, čo bude predmetom implementácie a merania.

### Úlohy

- Výber konkrétnej vyhľadávacej operácie alebo skupiny operácií.
- Zistenie:
  - ktoré atribúty sa vyhľadávajú,
  - aké kombinácie filtrov sa používajú,
  - aký objem údajov je relevantný,
  - kto všetko mení a vyhľadáva údaje v DB

---

## 2. Analýza existujúceho systému a literatúry

**Cieľ:** pochopiť súčasný stav a vytvoriť teoretický základ práce.

### Úlohy

- Zdokumentovanie toku:
  **COP_Wpf → COP_WS → COP_Functions → COP_Database → MSSQL**.
- Analýza existujúcich procedúr a databázových indexov.
- orientačné meranie:
  - času databázového dotazu,
  - času serverového spracovania,
  - celkového času požiadavky.
- Rešerš tém:
  - relačné databázové vyhľadávanie,
  - invertovaný index,
  - cache-aside a materializované vyhľadávacie indexy,
  - OpenSearch a Logstash,
  - synchronizácia a konzistencia.

### Výstupy

- Diagram súčasnej architektúry.
- merania databázového variantu.
- Rozpracovaná kapitola **Analýza súčasných prístupov**.
- Zoznam odborných zdrojov.

---

## 3. Návrh riešení a metodiky

**Cieľ:** definovať pravidlá, ktoré budú spoločné pre všetky implementácie.

### Úlohy

- Návrh rozhrania `ISearch`:
  - vstupné kritériá,
  - filtre,
  - stránkovanie,
  - radenie,
  - výsledky a chyby.
- Presné definovanie textového vyhľadávania:
  - celé slovo alebo prefix,
  - citlivosť na veľkosť písmen,
  - práca s diakritikou,
- Návrh vyhľadávacieho dokumentu.
- Rozhodnutie o synchronizácii OpenSearch:
  - prvotné naplnenie,
  - inkrementálne zmeny,
  - odstránenia,
  - obnova po chybe.
- Návrh experimentálnej metodiky ešte pred implementáciou.

### Výstupy

- Komponentový a sekvenčný diagram.
- Špecifikácia vyhľadávania.
- Návrh testovacích scenárov a metrík.

---

## 4. OpenSearch a prvotné naplnenie

**Cieľ:** pripraviť fungujúci index s testovacími údajmi.

### Úlohy

- Príprava vývojového prostredia.
- Konfigurácia OpenSearch a Logstash.
- Definovanie mapovania polí a textových analyzátorov.
- Nastavenie stabilného identifikátora dokumentu.
- Prvotné naplnenie údajov z MSSQL.
- Kontrola počtu a obsahu dokumentov.
- Overenie jednoduchých, kombinovaných a textových dotazov.

### Výstupy

- Funkčný OpenSearch index.
- Verziované konfigurácie.
- Postup nasadenia a naplnenia.
- Základné testy správnosti.

---

## 5. Integrácia OpenSearch do COP

**Cieľ:** dokončiť používateľskú cestu vyhľadávania.

### Úlohy

- Implementácia `ServiceOpenSearch` za rozhraním `ISearch`.
- Zapojenie do `COP_Functions`.
- Doplnenie fallbacku do databázy pri prázdnom výsledku.
- Definovanie správania pri:
  - timeoute,
  - nedostupnosti OpenSearch,
  - neplatnom dotaze.
- Zachovanie autorizácie a obmedzení prístupu.
- Meranie času jednotlivých častí požiadavky.
- Integračné testovanie.

### Výstupy

- Funkčná implementácia vyhľadávania.
- Testy fallbacku a chybových stavov.

---

## 6. Synchronizácia a overenie riešenia

**Cieľ:** overiť, že riešenie funguje aj pri zmenách údajov.

### Úlohy

- Dokončenie synchronizácie.
- Riešenie odstránených záznamov.
- Testovanie:
  - vytvorenia klienta,
  - zmeny na klientovi
  - odstránenia klienta,
  - opakovaného spracovania,
  - prerušenia a obnovenia synchronizácie.
- Meranie času od potvrdenia zmeny v DB po jej viditeľnosť vo vyhľadávaní.
- Overenie obnovy alebo opätovného vybudovania indexu.

### Výstupy

- Overená implementácia OpenSearch + Logstash.
- Zoznam známych obmedzení.
- Merania synchronizačného oneskorenia.

---

## 7. Návrh vlastnej implementácie

### Odporúčaný rozsah

Pre ohraničený prototyp je vhodná kombinácia:

- úložisko záznamov podľa ID,
- indexy vybraných atribútov,
- invertovaný index pre textové tokeny `(???)`,
- prieniky a zjednotenia množín kandidátov pri kombinovaných dotazoch. `(???)`

### Úlohy

- Výber dátových štruktúr.
- Návrh normalizácie textu.
- Návrh súbežného čítania a aktualizácií.
- Návrh synchronizácie z databázou.
- Rozhodnutie o rozdelení údajov medzi viac inštancií.
- Určenie pamäťových obmedzení a obnovy po reštarte.

### Výstupy

- Návrh vlastného riešenia.
- Zdôvodnenie algoritmov a dátových štruktúr.
- Odhad časovej a pamäťovej zložitosti.

**Myslieť na škálovateľnosť.**

---

## 8. Základ vlastného vyhľadávacieho systému

**Cieľ:** spojazdniť vlastné vyhľadávanie cez rovnaké rozhranie.

### Úlohy

- Načítanie testovacích údajov.
- Vyhľadávanie podľa ID.
- Presné filtrovanie podľa atribútov.
- Kombinovanie filtrov.
- Základné textové vyhľadávanie.
- Radenie a stránkovanie v dohodnutom rozsahu.
- Jednotkové testy a porovnanie výsledkov s referenčnými údajmi.

### Výstupy

- Funkčný základ vlastnej implementácie `ISearch`.
- Automatizované testy správnosti.

---

## 9. Synchronizácia, súbežnosť a škálovanie vlastného riešenia

**Cieľ:** pripraviť vlastné riešenie na experimenty.

### Úlohy

- Spracovanie vložení, aktualizácií a odstránení.
- Ochrana pred aplikovaním staršej zmeny po novšej.
- Testovanie vyhľadávania počas aktualizácie indexov.
- Dokončenie obnovy po reštarte.
- Implementácia dohodnutého spôsobu škálovania.
- Pilotná záťažová skúška a odstránenie zásadných chýb.

### Výstupy

- použiteľná vlastná implementácia.
- Popis synchronizácie a škálovania.

---

## 10. Experimenty

**Cieľ:** overiť, že merania sú správne a opakovateľné.

### Úlohy

- Príprava automatizovaného generovania záťaže. `(???)`
- Príprava datasetov rôznych veľkostí.
- Vytvorenie opakovateľnej sady dotazov:
  - existujúce a neexistujúce ID,
  - selektívne a široké filtre,
  - kombinácie atribútov,
  - textové dotazy,
  - dotazy vyvolávajúce fallback.
- Zber latencií, priepustnosti, chýb a spotreby zdrojov.
- merania a kontrola, či generátor záťaže sám nie je úzkym miestom.

### Výstupy

- Presný výstup z meraní.
- Grafy a opravená metodika.

---

## 11. Hlavné výkonnostné experimenty

**Cieľ:** zmerať odozvu, priepustnosť a záťaž.

### Úlohy

- Porovnanie všetkých variantov pri rovnakých scenároch.
- Postupné zvyšovanie počtu súbežných požiadaviek.
- Meranie pri rôznych objemoch údajov.
- Opakované behy.
- Meranie jednotlivých vyhľadávacích poskytovateľov aj celej serverovej cesty.
- Zaznamenanie podielu fallbackov do DB.

### Výstupy

- Výsledky meraní.
- Grafy latencie a priepustnosti.
- Porovnanie záťaže aplikačných služieb, vyhľadávacieho úložiska a DB.

---

## 12. Škálovateľnosť, aktuálnosť a poruchové scenáre

**Cieľ:** overiť správanie mimo ideálnych podmienok.

### Úlohy

- Vyhľadávanie počas aktualizácií údajov.
- Meranie synchronizačného oneskorenia.
- Kontrola úplnosti a správnosti výsledkov.
- Test výpadku a obnovy vyhľadávacieho úložiska.
- Meranie zvýšenej záťaže DB pri fallbackoch.
- Experimenty s počtom inštancií alebo pridelenými zdrojmi.
- Meranie času prvotného naplnenia a obnovy indexu.

### Výstupy

- Vyhodnotenie škálovateľnosti.
- Vyhodnotenie konzistencie a odolnosti.
- Doplňujúce merania podľa zistených problémov.

---

## 13. Vyhodnotenie a dokončenie textu

**Cieľ:** premeniť výsledky na odpovede na ciele diplomovej práce.

### Úlohy

- Spracovanie tabuliek a grafov.
- Interpretácia výsledkov:
  - kedy sa oplatí OpenSearch,
  - kde je výhodné vlastné riešenie,
  - kedy postačuje databáza.
- Diskusia kompromisov:
  - výkon verzus pamäť,
  - rýchlosť verzus aktuálnosť,
  - jednoduchosť verzus prevádzková náročnosť.
- Popis obmedzení experimentov.
- Dokončenie záveru a odporúčaní.

---

## Čo merať

| Oblasť | Odporúčané metriky |
| --- | --- |
| **Odozva** | Medián, p95 a p99 latencie. |
| **Priepustnosť** | Počet úspešne dokončených požiadaviek za sekundu. |
| **Spoľahlivosť** | Chybovosť a počet timeoutov. |
| **Záťaž** | CPU, RAM, diskové a sieťové operácie podľa dostupnosti monitoringu. |
| **Databáza** | Počet volaní, ich trvanie a záťaž spôsobená synchronizáciou aj fallbackom. |
| **Synchronizácia** | Čas od potvrdenia zmeny v DB po jej viditeľnosť vo vyhľadávaní. |
| **Správnosť** | Chýbajúce, nadbytočné alebo zastarané výsledky voči referencii. |
| **Prevádzka** | Čas naplnenia, obnovy a veľkosť úložiska. |
| **Škálovanie** | Zmena výkonu pri raste údajov, záťaže a dostupných zdrojov. |

---

## Otázky pre diplomovú prácu

Hneď na začiatku stanoviť  otázky `(???)`:

1. **Ako sa líši latencia a priepustnosť jednotlivých prístupov pri rôznych typoch vyhľadávania?**
2. **Ako rastie spotreba zdrojov a odozva s objemom údajov a počtom súbežných požiadaviek?**
3. **Do akej miery vyhľadávacie úložiská odľahčujú MSSQL po započítaní synchronizácie a fallbackov?**
4. **Aký vplyv má synchronizačné oneskorenie na správnosť a úplnosť výsledkov?**
5. **Za akých podmienok je vlastná implementácia výhodnejšia než OpenSearch alebo priame databázové vyhľadávanie?**

## Poznámky k stretnutiu 1

1. Kritéria vyhľadávania (Klientské dáta):
    - Rodné číslo
    - IČO
    - KlientId
    - Meno a Priezvisko
    - Ulica, popisné číslo, Mesto
    - Cnum
    - Tel. číslo
    - email
    - ifo

2. Kroky
      - Vytvoriť projekt (.NET Aspire)
      - spojazdniť OpenSearch
      - vytvoriť indexy a json dokumenty
      - Select dát z databázy
      - naplniť DB
      - Napojiť .NET službu a dotiahnuť do nej dáta

3. Implementácia Open Search
      - Domyslieť a zahrnúť fallback do služieb
      - Premyslieť zmenu údajov (Odchytávať zmeny samostatne v službách, alebo dorovnávať z databázy? poprípade trigger?)
      - Domyslieť spôsob vyhľadávania po každom kliknutí  

Užitočné nástroje na Banchmark napr:

- .NET Benchmark
