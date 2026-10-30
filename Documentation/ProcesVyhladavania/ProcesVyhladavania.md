# Ako funguje vyhľadávanie klienta

## 1. `KlientSearchUcVM.SearchKlientAsync` (WPF ViewModel)

1. **Reset stavu**: vynulujú sa `Base.Statics.H_KLIENT`, `H_PRODUKTY`, `H_UCET_INFO` a ďalšie. Zavolá sa `RozpracovaneUveryManager.ResetKlient()` a skryje sa panel histórie.
2. **Kontrola filtra**: pokračuje sa len vtedy, ak `KlientFilter` nie je prázdny. Zapíše sa log "Vyhladanie klienta".
3. **Zostavenie `KlientService.KlientFilterClass`** z `KlientFilter`:
   - `CNUM` sa doplní nulami na 6 znakov (`PadLeft(6,'0')`).
   - `CisloUctu` sa doplní nulami na 10 znakov.
   - `KlientID` sa doplní nulami na 10 znakov.
   - `ICO` prejde cez `FixIco`.
   - Rodné číslo sa doplní o `/` na pozícii 6, ak chýba.
   - `IdentifikatorKlienta` je rodné číslo, a ak nie je zadané, tak IČO.
   - **Číslo karty**: cez `Karta.ServiceAgent.VratUdajeKarty` sa zistí rodné číslo. Ak sa karta nájde, nastaví sa `ZobrazDetailKarty` a `isExactSearch = true`.
4. **Príznaky zobrazenia**: `ZobrazTelCislo`, `ZobrazEmail`, `ZobrazAdresaUlica` a `ZobrazAdresaCDomu` sa nastavia podľa toho, ktoré polia sú vyplnené.
5. **Doplňujúci popis**: ak bol zadaný CNUM, KlientID alebo RC (`isDoplnujuciPopis`), zavolá sa `_ServiceAgentKlient.KlientiByFilter(filter)`. Z výsledku sa nastaví:
   - `PopisVysledkuVyhladavania` (názov a identifikátor klienta),
   - `_klientDoplnujuciPopisNazov`,
   - `_klientIdDoplnujuciPopis`,
   - `_klientExactTyp`.
6. **Adresné vyhľadávanie**: ak je `AdresaTyp` Z, Y, X alebo A, validuje sa ulica a číslo domu (`IsErrorUlica` atď.). Zvyšok metódy je v kontexte skrátený.

## 2. `ZoznamKlientovByFilter` (COP_Functions)

Metóda dostane `KlientFilterClass` a príznak `exact`. Ak `exact == false`, nastaví sa `exactInt = 0`, čo znamená vyhľadávanie aj v referenciách. Inak ostane `null`.

Každé vyplnené pole filtra spustí samostatné vyhľadávanie a výsledky sa spoja cez `result.AddRange`. Vyhľadávania sú nezávislé, takže pri viacerých vyplnených poliach sa výsledky zlučujú a môžu sa duplikovať (zoskupenie podľa `KlientID` je zakomentované).

| Pole filtra | Spôsob vyhľadania |
|---|---|
| KlientID | `VratKlientaByProcedureSearch` (`exactInt`) |
| CNUM | procedúra (`exact = null`, vždy presne) |
| Rodné číslo | procedúra (`exactInt`) |
| IČO | procedúra (`exactInt`) |
| IFO | procedúra (`exactInt`) |
| Názov klienta | `VratKlientaByNazov`. Ak je nájdených viac klientov, vrátia sa všetci. Ak je nájdený presne jeden, dohľadá sa cez procedúru podľa jeho `KlientID`. |
| Číslo účtu | `VratKlientaByCisloUctu` (len ak sa dá parsovať na `long`) |
| Číslo úveru | `VratKlientaByCisloUver` |
| Telefón | procedúra |
| E-mail | procedúra |
| Ulica, číslo domu | procedúra, v troch variantoch: len ulica, ulica s číslom domu, len číslo domu |

**Záver metódy**: odstránia sa `null` záznamy. Ak je zadaný `KlientTyp`, výsledok sa prefiltruje len na tento typ.

## 3. `VratKlientaByProcedureSearch`

1. Vytvorí `DB.Klient` a zavolá `klientC.VratKlientaByProcedureSearch(...)`. Je to DB procedúra s parametrami IČO, RC, klient_id, CNUM, tel, exact, email, ulica, číslo domu a IFO. Vráti zoznam `DB.vKlient`.
2. Každý záznam sa transformuje cez `TransformaciaKlientClassFunctionByProcedureSearch` na `KlientClass`.
3. Ak je `allParams == true` (z `ZoznamKlientovByFilter` vždy), doplnia sa aj:
   - adresy,
   - kontakty,
   - zástupcovia,
   - kontaktné osoby,
   - KUV osoby,
   - PEP (politicky exponovaná osoba).

   Pre každého klienta to znamená niekoľko ďalších dotazov (N+1).

## Súhrn toku

`SearchKlientAsync` (UI) pripraví a normalizuje filter, získa popis a zavolá službu. Služba zavolá `ZoznamKlientovByFilter`, ktorá podľa vyplnených polí spúšťa `VratKlientaByProcedureSearch` (DB procedúra), zlúči výsledky a prefiltruje ich podľa typu klienta.

**Poznámky**:
- CNUM sa hľadá vždy presne, ostatné polia podľa `exact`.
- Duplicity pri kombinácii viacerých polí nie sú odstraňované.
- Vyhľadávanie podľa čísla účtu a úveru nepoužíva procedúru.



## Procedura PSearchKlient()

### Účel procedúry `dbo.pSearchKlient`

Procedúra je univerzálne vyhľadávanie klientov v banke (databáza `MAC`). Podľa zadaných kritérií nájde `klient_id` a vráti záznamy z pohľadu `dbo.vKlient`. Každý záznam má typ vzťahu k hľadanému klientovi.

### Vstupné parametre
| Parameter | Význam |
|---|---|
| `@ico` | IČO. Porovnáva sa bez lomítka, hľadá sa aj v `ico_zriadovatela`. |
| `@rc` | Rodné číslo. Porovnáva sa bez lomítka. |
| `@klient_id` | Priame ID klienta. |
| `@cnum` | Číslo klienta (CNUM). |
| `@ifo` | IFO. |
| `@mobil` | Telefón. |
| `@email` | E-mail. |
| `@adresaUlica`, `@adresaCDomu` | Ulica a číslo domu. |
| `@exact` | Ak nie je `NULL`, procedúra hľadá aj súvisiace osoby. |

### Postup

1. **Zber ID.** Každé zadané kritérium vloží nájdené `klient_id` do `@TABLE_KLIENT`. Pri telefóne, e-maile a adrese sa `@exact` nastaví na `NULL`. Kontakty sa hľadajú len v zdroji `KLIENT_KONTAKT`.
2. **Priame výsledky.** Pre tieto ID sa z `dbo.vKlient` naplní `@TABLE_KLIENT_RESULT` so pôvodným `klient_typ`. Vďaka tomu sú presné zhody prvé.
3. **Súvisiace osoby** (len ak `@exact IS NOT NULL`). Pre nájdených klientov sa hľadajú ďalšie osoby. Každý vzťah dostane vlastný typ a názov:

| Typ | Názov | Popis |
|---|---|---|
| 12 | Konajúca osoba | zástupca klienta |
| 17 | Konečný užívateľ výhod | KUV |
| 13 | Držiteľ karty | disponent platobnej karty podľa rodného čísla |
| 14 | Užívateľ EB | prístupy do elektronického bankovníctva |
| 10 | Disponent na PV | disponenti PV podľa rodného čísla, doplnené v roku 2025 |

   Držiteľ karty sa nepridá, ak je klient už vedený ako zástupca. Ak má osoba viac väzieb, zobrazí sa len hlavná.
4. **Výstup.** Na konci sa vráti `@TABLE_KLIENT_RESULT`, ktorý sa podľa komentára obmedzí na maximálny počet výsledkov (štandardne 100). Prvých riadkov za 445 som sa nedotkol, takže koniec popisujem podľa komentára v kóde.

### Ďalšie vlastnosti
- Premenná `@debug` (štandardne 0) zapína `PRINT` hlásenia o priebehu.
- Procedúra číta aj z iných databáz: `BANKA`, `COP`, `PS`, `IMPORT_AS_PC`. Používa `COLLATE` kvôli rozdielnym collation.
- Dôležité je poradie výsledkov: presné zhody musia byť prvé, lebo niektoré funkcie očakávajú presne jeden výsledok.
- Dlhý zoznam stĺpcov je v kóde opakovaný pri každom `INSERT`. Komentár v kóde navrhuje stabilnejšie riešenie.

Procedúra `dbo.pSearchKlient` (DB `MAC`) číta tieto objekty. Prečítal som celý kód okrem posledných riadkov za 445 (záverečný `SELECT` výsledku).

### Priame vyhľadávanie (vždy)

| Objekt | Použitie |
|---|---|
| `MAC.dbo.KLIENT` | podľa IČO (aj `ico_zriadovatela`), CNUM, rodného čísla, IFO; aj pri disponentoch PV |
| `MAC.dbo.KLIENT_ADRESA` | podľa ulice a čísla domu |
| `mac.dbo.fKlietsByPhoneContact(@mobil)` | funkcia, filter `zdroj = '[MAC].[dbo].[KLIENT_KONTAKT]'` (telefón) |
| `mac.dbo.fKlientsByEmailContact(@email)` | funkcia, rovnaký filter (e-mail) |
| `dbo.vKlient` | pohľad, z ktorého sa plní výsledok |

### Exaktné vyhľadávanie (`@exact IS NOT NULL`)

| Objekt | Použitie |
|---|---|
| `MAC.dbo.KLIENT_ZASTUPCA` | zástupcovia, konajúce osoby (typ 12) |
| `MAC.dbo.KLIENT_KUV` | konečný užívateľ výhod (typ 17) |
| `BANKA.dbo.BANK_CRD_SAFE` | držitelia kariet (typ 13) |
| `banka.dbo.ibsaclmt` | používatelia EB (typ 14) |
| `banka.dbo.IBS_VLASTNIK` | používatelia EB (typ 14) |
| `COP.dbo.UCET` | používatelia EB a disponenti PV |
| `IMPORT_AS_PC.dbo.LN_PV_DISPONET` | disponenti PV (typ 10) |
| `PS.dbo.ACCOUNT_Document_disponent` | disponenti PV (typ 10) |
| `PS.dbo.ACCOUNT_SIGNATURE_DOCUMENT` | disponenti PV (typ 10) |

### Poznámky

- Tabuľky `banka.dbo.IBSCLNT` a `KLIENT` v zakomentovanom kóde sa už nepoužívajú.
- Disponenti PV sa hľadajú len pri `@rc` dlhšom ako 6 znakov. Blok je vnútri `IF @exact IS NOT NULL`.
- `@exact` sa pri vyhľadávaní podľa telefónu, e-mailu a adresy nastaví na `NULL`. Vtedy sa tieto exaktné vyhľadávania nespustia.
- Pri `@rc` bez lomítka sa lomítko doplní, pretože disponenti sa hľadajú v oboch tvaroch.
- Pomocné table premenné: `@TABLE_KLIENT`, `@TABLE_ZASTUPCA`, `@TABLE_KUV`, `@TABLE_KARTA`, `@TABLE_UZIVATEL_EB`, `@TABLE_DISPONENT` a `@TABLE_KLIENT_RESULT`.

Stĺpce výsledku sú len z pohľadu `dbo.vKlient`. Ostatné tabuľky procedúra používa iba na zistenie `klient_id`. Pri stĺpcoch bez aliasu v kóde je pôvod odvodený z kontextu.

## Výsledok (`@TABLE_KLIENT_RESULT`)

Všetky stĺpce idú z `dbo.vKlient`, napríklad `klient_id`, `cnum`, `klient_typ`, `meno`, `priezvisko`, `nazov_klienta`, `rodne_cislo`, `ico`, `dic`, `ifo`, `datum_narodenia`, doklady, štátna príslušnosť, marketingové skupiny a `poznamka`. Je ich asi 120.

- Pri typoch 12, 13, 14, 17 a 10 sa `klient_typ` a `klient_typ_nazov` prepíšu konštantou. Napríklad pri konajúcej osobe je to `12` a `'Konajúca osoba'`.
- Z ktorých tabuliek `vKlient` skladá údaje, z kódu procedúry nezistím. Môžem ti vypísať definíciu pohľadu.

## Stĺpce použité na hľadanie

| Tabuľka alebo funkcia | Použité stĺpce | Na čo |
|---|---|---|
| `MAC.dbo.KLIENT` | `klient_id` (výstup), `ico`, `ico_zriadovatela`, `cnum`, `rodne_cislo`, `ifo` | filtre podľa parametrov |
| `MAC.dbo.KLIENT_ADRESA` | `klient_id` (výstup), `ulica`, `cislo` | adresa |
| `fKlietsByPhoneContact(@mobil)` | `klient_id` (výstup), `zdroj` | telefón |
| `fKlientsByEmailContact(@email)` | `klient_id` (výstup), `zdroj` | e-mail |
| `MAC.dbo.KLIENT_ZASTUPCA` | `klient_id` (výstup), `ZASTUPCA_ID` | zástupcovia |
| `MAC.dbo.KLIENT_KUV` | `klient_id` (výstup), `kuv_id`, `status` | KUV, vylúčené `status = 2` |
| `BANKA.dbo.BANK_CRD_SAFE` | `RC`, `OWNER`, `CNUM` | držitelia kariet |
| `MAC.dbo.KLIENT` (`klient`) | `klient_id` (výstup), `cnum`, `rodne_cislo` | prepojenie karty cez `cnum` |
| `COP.dbo.ucet` | `klient_id` (výstup), `cislo_uctu`, `cnum` | EB a disponenti PV |
| `banka.dbo.ibsaclmt` | `ACNOC`, `CLNTSN`, `sluzba`, `limit` | EB, služby `DPP` a `ZPP` s limitom nad 0 |
| `banka.dbo.IBS_VLASTNIK` | `klient_id`, `clntsn` | EB, väzba klient a `CLNTSN` |
| `IMPORT_AS_PC.dbo.LN_PV_DISPONET` | `ACNO`, `RCP` | disponenti PV, rodné číslo s lomkou aj bez |
| `PS.dbo.ACCOUNT_Document_disponent` | `ID_QR`, `ACNOC`, `disponent_rodne_cislo` | disponenti PV |
| `PS.dbo.ACCOUNT_SIGNATURE_DOCUMENT` | `ID_QR`, `CRT_DATE`, `AUTHORIZED`, `NUMBER_SIGNATURE` | disponenti PV, len dnešný dátum, autorizované, podpis č. 1 |

## Poznámky

- `ACCOUNT_Document_disponent` a `ACCOUNT_SIGNATURE_DOCUMENT` majú stĺpce `CRT_DATE`, `AUTHORIZED`, `NUMBER_SIGNATURE` a `disponent_rodne_cislo` v kóde bez aliasu. Ku ktorej z nich patria, je odvodené z názvov. Presné pridelenie overím cez `sys.columns`, ak chceš.
- Vo vzťahu k `COP.dbo.ucet` sa pri disponentoch PV vracia `cnum`. Podľa neho sa potom v `KLIENT` nájde `klient_id`.