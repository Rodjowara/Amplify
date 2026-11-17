# Amp: vodič kroz cijeli codebase

Ovaj vodič pretpostavlja da nikad nisi radio s Rustom ni s audiom. Ide redom:

1. [Što je Amp i gdje je u sustavu](#1-što-je-amp-i-gdje-je-u-sustavu)
2. [Audio osnove](#2-audio-osnove)
3. [Rust osnove (samo ono što se koristi u Ampu)](#3-rust-osnove)
4. [Struktura projekta](#4-struktura-projekta)
5. [Datoteka po datoteka](#5-datoteka-po-datoteka)
6. [Put jedne pjesme od Maestra do zvučnika](#6-put-jedne-pjesme-od-maestra-do-zvučnika)
7. [Bug "svira 1 sekundu"](#7-bug-svira-1-sekundu)
8. [Što je mrtvo, prazno ili nedovršeno](#8-što-je-mrtvo-prazno-ili-nedovršeno)
9. [Pojmovnik](#9-pojmovnik)

---

## 1. Što je Amp i gdje je u sustavu

Amplify ima dva programa:

- **Maestro** (C#, `maestro/`) je "mozak". Korisnik u njemu bira pjesmu i šalje je na zvučnike.
- **Amp** (Rust, `amp/`) radi na uređaju uz zvučnik (npr. Raspberry Pi). Prima audio i pušta ga.

Između njih su dvije veze:

```
            MAESTRO (C#)                                      AMP (Rust)
 ┌─────────────────────────────────┐              ┌──────────────────────────────────┐
 │ sample.mp3                      │              │                                  │
 │   │ ffmpeg (dekodira u PCM)     │              │                                  │
 │   ▼                             │   audio      │                                  │
 │ snapserver (Docker) ────────────┼──port 1704──►│ snapcast klijent ─► cpal ─► 🔊   │
 │                                 │              │        ▲                         │
 │ AmpConnection ──────────────────┼──port 50051─►│ gRPC server (amp-control)        │
 │   "spoji se na mene, port 1704" │   kontrola   │                                  │
 └─────────────────────────────────┘              │ Qt UI (amp-ui) na ekranu         │
                                                  └──────────────────────────────────┘
```

1. **Kontrolna veza (gRPC, port 50051).** Maestro Ampu kaže: "spoji se na snapserver na adresi X, port 1704". To je jedina naredba koja zasad postoji (`Handshake`).
2. **Audio veza (Snapcast, port 1704).** Amp se spoji na snapserver i od njega neprekidno prima audio.

**Snapcast** je gotov open-source sustav za sinkronizirano puštanje glazbe na više zvučnika. Ima dva dijela: *server* (snapserver) i *klijente*. Server šalje audio s vremenskim oznakama, pa svi klijenti sviraju točno isti trenutak pjesme u isto vrijeme. Maestro pokreće pravi snapserver (C++ program) u Docker kontejneru. Amp koristi Rust biblioteku `snapcast-client` da bude klijent.

---

## 2. Audio osnove

### 2.1 Zvuk kao brojevi

Zvuk je titranje zraka. Mikrofon ga pretvara u napon koji se mijenja kroz vrijeme. Računalo taj napon **mjeri mnogo puta u sekundi** i svaki put zapiše jedan broj. Jedan takav broj zove se **sample** (uzorak).

```
napon
  │    ╭─╮        ╭─╮
  │   ╱   ╲      ╱   ╲          • = jedan sample
  │──•─────•────•─────•────     (vrijednost u tom trenutku)
  │         ╲  ╱
  │          ╰╯
  └──────────────────────► vrijeme
```

Zvučna kartica radi obrnuto: dobije niz brojeva, svaki pretvori u napon i to pošalje zvučniku.

### 2.2 Sample rate

**Sample rate** je koliko se sampleova uzme u sekundi, u hercima (Hz):

- **44100 Hz** (44.1 kHz): standard za CD i većinu MP3-jeva. `sample.mp3` je takav.
- **48000 Hz** (48 kHz): standard za video i profesionalni audio. Maestro sve šalje u 48 kHz.

**Ključno:** niz brojeva sam po sebi ne zna kojim tempom se treba svirati. Ako zvučnoj kartici daš 48000 sampleova snimljenih u sekundi, a ona ih svira tempom 44100 u sekundi, ta "sekunda" traje 1.088 s: zvuk je sporiji i dublji. Zato **onaj tko proizvodi i onaj tko svira moraju se dogovoriti o sample rateu**. Ako se ne dogovore, netko mora **resamplati**, tj. preračunati niz na drugi rate (to radi `ffmpeg -ar 48000` u Maestru za MP3 od 44.1 kHz). Upravo je zbog ovoga nastao bug iz poglavlja 7.

### 2.3 Kanali, frameovi i interleaving

**Kanal** je jedan zaseban zvučni signal. Stereo ima 2 kanala, lijevi (L) i desni (D).

**Frame** je skup sampleova za sve kanale u istom trenutku. U stereu je frame = 1 lijevi + 1 desni sample. Sample rate zapravo znači "frameova u sekundi".

Sampleovi se u memoriji spremaju **isprepleteno** (*interleaved*):

```
[ L0, D0, L1, D1, L2, D2, ... ]
  └frame 0┘ └frame 1┘ └frame 2┘
```

### 2.4 Format samplea (bit depth i encoding)

Isti zvuk se može zapisati u različitim vrstama brojeva:

| Format | Što je | Raspon |
|---|---|---|
| `s16le` / PCM 16-bit | cijeli broj, 2 bajta | −32768 … 32767 |
| PCM 24-bit | cijeli broj, u Snapcastu spremljen u 4 bajta | ±8388607 |
| PCM 32-bit | cijeli broj, 4 bajta | ±2147483647 |
| `f32` / float | decimalni broj, 4 bajta | −1.0 … 1.0 |

`le` znači *little endian*: najmanje značajan bajt ide prvi u memoriji. Tako se piše na x86 i ARM procesorima.

**PCM** (*Pulse-Code Modulation*) jednostavno znači "sirovi niz sampleova, bez kompresije".

Zvučne kartice (preko cpala) u Ampu primaju `f32`. Zato `snapcast.rs` svaki format pretvara u `f32` dijeljenjem s maksimumom, npr. `sample_i16 / 32767.0`.

### 2.5 Kodeci (MP3, FLAC)

Sirovi PCM je velik: 48000 × 2 kanala × 2 bajta = 192 KB u sekundi. **Kodek** ga sažima:

- **MP3** gubi dio informacija (*lossy*). Maestro ga čita iz datoteke, a `ffmpeg` ga **dekodira** natrag u PCM.
- **FLAC** ne gubi ništa (*lossless*), samo sažima. Snapserver PCM kodira u FLAC prije slanja preko mreže, a `snapcast-client` u Ampu ga dekodira natrag.

Format streama sa servera zapisuje se kao `rate:bits:channels`, npr. `48000:16:2`. FLAC dekoder u `snapcast-client` uvijek vraća `f32`, pa Amp vidi format **`48000:32:2`**.

### 2.6 Buffer i latencija

Zvučna kartica ne čita sample po sample. Svakih nekoliko milisekundi traži cijeli komad (npr. 480 frameova = 10 ms) i kaže "**daj mi sljedećih N frameova, odmah**". Funkcija koja taj komad popunjava zove se **audio callback**. Pravila za callback:

- Mora vratiti podatke **na vrijeme**. Ako zakasni, čuje se pucketanje (*underrun*).
- Zato u callbacku **nikad ne smiješ čekati**: ni zaključavanje koje bi moglo blokirati, ni mrežu, ni disk.

**Latencija** je vrijeme od trenutka kad su podaci "spremni" do trenutka kad zvuk izađe iz zvučnika. **DAC** (*digital-to-analog converter*) je čip koji brojeve pretvara u napon. "DAC latencija" je koliko još traje dok podaci koje sad dajemo stignu do DAC-a.

### 2.7 Kako Snapcast sinkronizira više zvučnika

Ovo je najzahtjevniji dio, ali bez njega se ne razumije `pull_synced_f32`.

1. Snapserver audio reže na **chunkove** (komade, npr. 20 ms). Svakom chunku zapiše **vremensku oznaku** (*timestamp*): "ovo treba zasvirati u trenutku T po satu servera".
2. Server dodaje **buffer** (zadano 1000 ms). Chunk koji je nastao sad treba zasvirati tek za 1 s. Tako svi klijenti imaju vremena primiti ga.
3. Satovi na Maestru i na Ampu nisu isti. Klijent zato stalno šalje `Time` poruke i mjeri **razliku između svog sata i sata servera** (`diff_to_server_usec`). Tako zna koliko je "sad" po serverovom satu.
4. Svaki put kad zvučna kartica traži podatke, klijent računa **age** (starost):

   ```
   age = (server_sad) − (timestamp chunka) − buffer + DAC_latencija
   ```

   - `age ≈ 0`: sviramo točno na vrijeme.
   - `age > 0`: kasnimo, a `age < 0`: žurimo.
5. Kad odstupanje postane preveliko, klijent napravi **hard sync**: baci zastarjele chunkove (ili ubaci tišinu) i skoči na pravo mjesto. Pragovi u `snapcast-client` su npr. medijan zadnjih 20 mjerenja > 50 ms, medijan zadnjih 100 mjerenja > 5 ms ili `|age|` > 500 ms. Hard sync se čuje kao skok ili prekid.
6. Kod manjih odstupanja radi **soft sync**: povremeno ubaci ili izbaci jedan sample, što se ne čuje.

---

## 3. Rust osnove

Samo ono što se pojavljuje u Ampu.

### 3.1 Cargo, crateovi i workspace

- **Cargo** je Rustov build alat i package manager. To je otprilike `dotnet` + NuGet u jednom.
- **Crate** je jedan Rust paket: biblioteka ili program. Odgovara C# projektu (`.csproj`).
- **`Cargo.toml`** opisuje crate: ime, ovisnosti (`[dependencies]`) i ovisnosti samo za testove (`[dev-dependencies]`).
- **Workspace** je više crateova koji se grade zajedno. Odgovara C# solutionu (`.sln`). `amp/Cargo.toml` je workspace.
- **`Cargo.lock`** sadrži točne verzije svih ovisnosti, da build bude isti na svakom računalu.
- **`build.rs`** je mali Rust program koji Cargo pokrene **prije** kompajliranja cratea. Koristi se za generiranje koda. Kod nas generira gRPC klase iz `.proto` datoteka i ugrađuje QML datoteke u binarnu datoteku.
- `crates/<ime>/src/lib.rs` je ulaz u biblioteku, a `src/main.rs` ulaz u program.
- `tests/` sadrži **integracijske testove**: svaka datoteka je zaseban test-program koji koristi crate izvana.

Naredbe:

```
cargo build                   # kompajlira
cargo test                    # pokreće sve testove
cargo clippy --all-targets    # linter, upozorava na loš ili sumnjiv kod
cargo run -p amp-app          # pokreće program (vidi README za Qt varijable)
```

### 3.2 Moduli i vidljivost

```rust
pub mod chain;      // modul iz datoteke chain.rs, vidljiv izvana
mod snapcast;       // modul vidljiv samo unutar ovog cratea
```

- Sve je **privatno** dok ne napišeš `pub`.
- `pub(crate)` znači "javno, ali samo unutar ovog cratea". Odgovara C# `internal`.
- `use nešto::Nešto;` uvozi ime, kao C# `using`.
- `crate::` je korijen trenutnog cratea, npr. `use crate::session::PlayerSlot;`.

### 3.3 Varijable, tipovi, strukture

```rust
let x = 5;              // nepromjenjiva (immutable) varijabla
let mut y = 5;          // promjenjiva
const FORMAT_POLL_INTERVAL: Duration = Duration::from_millis(50);  // konstanta

pub struct StreamConfig {      // kao C# class/record, samo podaci
    pub host: String,
    pub port: u16,             // u16 = cijeli broj 0..65535
}

impl StreamConfig {            // metode se pišu odvojeno, u impl bloku
    fn nesto(&self) { }        // &self = "this", samo za čitanje
}
```

Tipovi brojeva: `u16`, `u32`, `u64` su bez predznaka, `i16`, `i32`, `i64` s predznakom, `f32` i `f64` su decimalni, a `usize` je veličina u memoriji (za indekse).

`type PlayerSlot = Arc<...>;` je samo **alias**, drugo ime za postojeći tip.

**`#[derive(...)]`** automatski generira kod za strukturu:
- `Debug` omogućuje ispis s `{:?}`.
- `Clone` daje metodu `.clone()`.
- `Default` daje `::default()` s praznim ili nultim vrijednostima.
- `PartialEq` i `Eq` omogućuju usporedbu s `==`.

### 3.4 Vlasništvo (ownership) i posuđivanje (borrowing)

Ovo je **najvažniji Rust koncept** i ono što ga razlikuje od C#-a.

- Svaka vrijednost ima **točno jednog vlasnika**. Kad vlasnik izađe iz scopea, vrijednost se odmah uništi (**drop**). Nema garbage collectora.
- `let b = a;` **premješta** (*move*) vrijednost u `b`. Nakon toga `a` više ne smiješ koristiti.
- `&a` je **posudba za čitanje** (referenca). Takvih može biti koliko god.
- `&mut a` je **posudba za pisanje**. U jednom trenutku može postojati samo jedna i tada nema drugih posudbi.

Zašto je to bitno u Ampu: `cpal::Stream` svira **dok postoji**. Kad ga se dropa (npr. varijabla izađe iz scopea ili se postavi na `None`), zvuk prestaje. Zato `output.rs` drži stream u varijabli `active_stream` u petlji koja nikad ne završava.

`drop(x)` eksplicitno uništi `x` odmah.

### 3.5 `Option` i `Result`: nema `null` i nema exceptiona

```rust
enum Option<T> { Some(T), None }        // vrijednost ili ništa
enum Result<T, E> { Ok(T), Err(E) }     // uspjeh ili greška
```

Kao da je `null` (odnosno exception) postao dio tipa: kompajler te tjera da obradiš oba slučaja.

Česte metode:

| Kod | Značenje |
|---|---|
| `x.map(\|v\| ...)` | ako je `Some(v)`, pretvori `v`; `None` ostaje `None` |
| `x.filter(\|v\| uvjet)` | `Some(v)` ostaje samo ako uvjet vrijedi, inače postane `None` |
| `x.and_then(\|v\| ...)` | kao `map`, ali funkcija i sama vraća `Option` |
| `x.as_ref()` | `&Option<T>` → `Option<&T>`, pogled unutra bez premještanja |
| `x.take()` | izvadi vrijednost i ostavi `None` na njenom mjestu |
| `x.ok()` | `Result` → `Option`, greška postane `None` |
| `x?` | ako je `None` ili `Err`, odmah izađi iz funkcije i vrati to |
| `x.unwrap()` / `x.expect("poruka")` | "sigurno je `Some`/`Ok`"; ako nije, **panic** (srušeni thread) |
| `if let Some(v) = x { ... }` | uđi u blok samo ako je `Some` |
| `match x { Some(v) => ..., None => ... }` | kao `switch` koji mora pokriti sve slučajeve |

**Panic** odgovara neuhvaćenom exceptionu i ruši thread. Koristi se samo kad je nešto "nemoguće".

### 3.6 Closure (lambda)

```rust
|err| eprintln!("greška: {err}")           // lambda s jednim argumentom
move |data: &mut [f32], _| { ... }         // move: lambda preuzima vlasništvo nad varijablama koje koristi
```

`move` je potreban kad lambda živi dulje od funkcije u kojoj je napisana, npr. audio callback ili kod novog threada.

### 3.7 Makroi

Sve što završava s `!` je **makro**: kod koji generira kod.

- `println!("x = {x}")` ispisuje na stdout, `eprintln!` na stderr.
- `format!(...)` vraća `String`.
- `vec![0u8; n]` stvara vektor od `n` nula.
- `include_bytes!("datoteka")` ugradi sadržaj datoteke u binarnu datoteku kod kompajliranja.
- `tonic::include_proto!` uključi kod koji je generirao `build.rs`.

### 3.8 Traitovi

**Trait** odgovara C# **interfaceu**.

```rust
impl AmpService for AmpHandler { ... }   // AmpHandler implementira interface AmpService
```

`use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};` je potreban jer metodu traita (npr. `stream.play()`) možeš zvati samo ako je trait uvezen.

### 3.9 Threadovi i dijeljenje podataka

Amp ima nekoliko threadova koji rade istovremeno:
- **main**: Qt UI;
- **amp-control**: gRPC server;
- **amp-audio-output**: drži cpal stream;
- **audio callback thread**: njega stvara cpal, ne mi;
- tokio radnici (vidi 3.10).

Za dijeljenje podataka među threadovima:

- **`Arc<T>`** (*Atomically Reference Counted*) omogućuje da više vlasnika dijeli istu vrijednost. `Arc::clone(&a)` ne kopira podatke, samo poveća brojač. Vrijednost se uništi kad zadnji `Arc` nestane.
- **`Mutex<T>`**: samo jedan thread u isto vrijeme smije pristupiti `T`.
  - `m.lock()` **čeka** dok ne dobije pristup i vraća "guard". Kad guard nestane (kraj scopea), mutex se automatski otključa.
  - `m.try_lock()` **ne čeka**: ako je zauzeto, odmah vrati grešku. Zato se koristi u audio callbacku.
  - **Poisoning**: ako thread panica dok drži mutex, mutex postane "otrovan" i `lock()` vraća `Err`. `.unwrap_or_else(|err| err.into_inner())` znači "svejedno mi daj podatke".
- **`RwLock<T>`** dopušta više čitatelja (`read()`) **ili** jednog pisca (`write()`).
- **`Send`** je oznaka tipa koji se smije premjestiti na drugi thread. `cpal::Stream` **nije** `Send`, pa mora ostati na threadu na kojem je stvoren.
- **Deadlock**: thread A drži lock X i čeka Y, a thread B drži Y i čeka X. Izbjegava se tako da svi uvijek zaključavaju istim redoslijedom.

`Arc<Mutex<Option<Arc<SnapcastClientHandle>>>>` (to je `PlayerSlot`) se čita iznutra prema van:
- `SnapcastClientHandle` je snapcast klijent;
- `Arc<...>` znači da ga više mjesta dijeli;
- `Option<...>` znači da ga može i ne biti;
- `Mutex<...>` znači da ga više threadova smije mijenjati, jedan po jedan;
- vanjski `Arc<...>` znači da cijelu tu "kutiju" dijeli više threadova.

### 3.10 Async i tokio

`async fn` je funkcija koja se može **pauzirati** dok čeka (mrežu, timer) i pustiti drugi kod da radi u međuvremenu. Isto je kao C# `async`/`await`, samo se piše `.await` na kraju:

```rust
let reply = client.handshake(req).await;
```

- **tokio** je runtime koji izvršava async kod, na nekoliko radnih threadova.
- `tokio::spawn(async { ... })` pokreće novi **task** (lagani "thread" unutar tokija), kao C# `Task.Run`.
- `tokio::select!` čeka više stvari odjednom i nastavi s onom koja prva završi.
- `tokio::sync::mpsc` je kanal za poruke: jedna strana šalje (`tx`, transmit), druga prima (`rx`, receive).
- `tokio::sync::oneshot` je kanal za samo jednu poruku.
- `tokio::sync::Mutex` je async mutex, koji smije ostati zaključan preko `.await`. Običan `std::sync::Mutex` ne smije.
- `#[tokio::test]` je test koji se izvršava u tokio runtimeu.
- `#[tonic::async_trait]` je makro koji omogućuje `async fn` u traitu (za gRPC).

### 3.11 Ostalo što ćeš vidjeti

- `impl Into<String>` znači "bilo što što se može pretvoriti u `String`".
- `'static` je životni vijek "cijeli program". Najčešće ga vidiš kod `&'static str`, string ugrađenog u program.
- `let ... else { return ... };` znači "raspakiraj; ako ne uspije, izađi":

  ```rust
  let Some(device) = host.default_output_device() else { return None; };
  ```

- `if let A = x && let B = y { ... }` spaja više `if let` uvjeta u jedan. Dostupno je od edition 2024.
- `#[cfg(test)] mod tests { ... }` je kod koji se kompajlira samo za `cargo test`.

---

## 4. Struktura projekta

```
Amplify/
├── amp/                          ← Rust workspace (ovaj vodič)
│   ├── Cargo.toml                ← popis crateova u workspaceu
│   ├── README.md                 ← kako instalirati Qt i pokrenuti
│   ├── deploy/                   ← amp.service, install.sh (zasad prazni)
│   └── crates/
│       ├── amp-app/              ← PROGRAM: main(), spaja sve ostalo
│       ├── amp-core/             ← zajedničko stanje (AmpState)
│       ├── amp-control/          ← gRPC server (Maestro → Amp naredbe)
│       ├── amp-audio/            ← snapcast klijent + zvučna kartica
│       └── amp-ui/               ← Qt/QML sučelje
├── shared/                       ← dijele ga Amp i Maestro
│   ├── protos/                   ← gRPC ugovor (.proto)
│   └── qml/Amplify/              ← zajedničke QML komponente i tema
└── maestro/                      ← C# aplikacija
```

Tko o kome ovisi (strelica = "koristi"):

```
amp-app ──► amp-control ──► amp-audio
   │              │              │
   ├──► amp-ui ◄──┼──────────────┘  (amp-audio ga ima u Cargo.toml, ali ga ne koristi)
   │      │       │
   └──────┴───────┴──► amp-core
```

Vanjske biblioteke (crateovi s crates.io):

| Crate | Čemu služi |
|---|---|
| `tokio` | async runtime |
| `tonic`, `prost` | gRPC server i (de)serijalizacija protobuf poruka |
| `tonic-prost-build` | (build.rs) generira Rust kod iz `.proto` |
| `snapcast-client`, `snapcast-proto` | Snapcast klijent (prima, dekodira i sinkronizira audio) |
| `snapcast-server` | (samo testovi) pravi snapserver u memoriji za testiranje |
| `cpal` | pristup zvučnoj kartici (Cross-Platform Audio Library) |
| `qtbridge` | Qt-ova službena veza Rust ↔ QML |
| `fundsp`, `anyhow` | u `Cargo.toml`, ali se zasad ne koriste |

---

## 5. Datoteka po datoteka

Redoslijed je od ulaza programa prema dubini.

### 5.1 `amp-app/src/main.rs`: početak programa

```rust
fn main() {
    let state = SharedState::default();     // prazno stanje: nismo spojeni ni na što
    let slot = PlayerSlot::default();       // prazan slot: nema streama za svirati

    start_output(slot.clone());             // thread za zvučnu karticu

    let addr = "0.0.0.0:50051".parse().expect("invalid listen address");
    amp_control::server::spawn(addr, state.clone(), slot.clone());  // thread za gRPC

    amp_ui::run(state);                     // Qt; blokira dok se prozor ne zatvori
}
```

- **`state`** (`SharedState`) govori na koji je snapserver Amp spojen. Piše ga gRPC, a čitat će ga UI.
- **`slot`** (`PlayerSlot`) je "utičnica" u koju gRPC stavi snapcast klijenta, a audio izlaz iz nje vadi zvuk. To je jedini most između kontrolnog i audio dijela.
- `.clone()` na njima kopira samo `Arc`, pa svi dijele **isti** podatak.
- `0.0.0.0:50051` znači "slušaj na portu 50051 na svim mrežnim sučeljima".
- Qt mora biti na **main threadu**, zato je on zadnji i blokira. Kad se prozor zatvori, `main` završi i cijeli program se ugasi, zajedno sa svim threadovima.

### 5.2 `amp-core/src/state.rs`: zajedničko stanje

```rust
pub struct StreamConfig { pub host: String, pub port: u16 }

pub struct AmpState {
    pub stream: Option<StreamConfig>,   // None = nismo spojeni
}

pub type SharedState = Arc<RwLock<AmpState>>;
```

Ovdje se drži ono što više dijelova programa treba znati. Zasad je to samo adresa snapservera. `RwLock` je izabran jer će UI često čitati, a gRPC rijetko pisati.

### 5.3 `shared/protos/amp.proto`: gRPC ugovor

```proto
service AmpService {
    rpc Handshake(HandshakeRequest) returns (HandshakeResponse);
}
message HandshakeRequest  { string snapcast_host = 1; uint32 snapcast_port = 2; }
message HandshakeResponse { bool success = 1; optional string error = 2; }
```

**gRPC** je način pozivanja funkcija na drugom računalu preko mreže. `.proto` datoteka je **ugovor** između dvije strane: koje funkcije postoje i kako izgledaju poruke. Iz istog `.proto`:
- Maestro (C#) generira **klijenta**, `AmpService.AmpServiceClient`;
- Amp (Rust) generira **server trait**, `AmpService`, koji mi implementiramo.

Brojevi `= 1`, `= 2` su ID-ovi polja na mreži, ne vrijednosti. Ne smiju se mijenjati kad je ugovor jednom u upotrebi.

`dummy.proto` (`Ping` → `"pong: ..."`) služi samo za test da gRPC uopće radi.

### 5.4 `amp-control`: gRPC server

**`build.rs`** prije kompajliranja pozove `tonic_prost_build`. Taj alat iz `shared/protos/*.proto` generira Rust kod: strukture `HandshakeRequest`, `HandshakeResponse` i trait `AmpService`. Kod završi u `target/.../out/`.

**`lib.rs`**:

```rust
pub mod proto {
    pub mod amp { tonic::include_proto!("amplify.amp"); }   // "uključi generirani kod ovdje"
    ...
}
mod amp_handler;   // privatno
mod handlers;      // privatno
pub mod server;    // javno: main.rs zove server::spawn
```

**`server.rs`**:

```rust
pub async fn serve(addr, state, slot) -> Result<(), tonic::transport::Error> {
    Server::builder()
        .add_service(DummyServiceServer::new(DummyHandler))
        .add_service(AmpServiceServer::new(AmpHandler::new(state, slot)))
        .serve(addr)
        .await
}
```

Napravi gRPC server s dva servisa i vrti ga dok program radi.

```rust
pub fn spawn(addr, state, slot) -> JoinHandle<()> {
    thread::Builder::new().name("amp-control".into()).spawn(move || {
        let runtime = tokio::runtime::Builder::new_multi_thread().enable_all().build()...;
        runtime.block_on(serve(addr, state, slot))   // pokreni async kod i čekaj
    })
}
```

`main` nije async jer Qt treba main thread. Zato gRPC dobije **vlastiti thread s vlastitim tokio runtimeom**. `block_on` je most iz običnog koda u async kod.

**`handlers.rs`** implementira `Ping`: vrati `"pong: " + poruka`.

**`amp_handler.rs`** je najvažniji dio ovog cratea:

```rust
#[derive(Clone)]
pub struct AmpHandler {
    state: SharedState,
    slot: PlayerSlot,
    session: Arc<Mutex<Option<StreamSession>>>,   // tokio Mutex: drži se preko .await
}
```

`tonic` može obrađivati više zahtjeva paralelno i za to klonira handler, zato `Clone` i `Arc`. `session` je trenutna veza na snapserver.

Što radi `handshake`:

1. **Validacija.** Host ne smije biti prazan, a port mora biti 1–65535:

   ```rust
   let port = u16::try_from(req.snapcast_port)   // u32 → u16, ne uspije ako je > 65535
       .ok()                                     // Result → Option
       .filter(|p| *p != 0)                      // 0 nije dozvoljen
       .ok_or_else(|| Status::invalid_argument(...))?;  // None → vrati gRPC grešku
   ```

2. `self.session.lock().await` osigurava da istovremeno teče samo jedan handshake.
3. **Gašenje starog.** Prvo isprazni slot (izlaz odmah utihne), pa ugasi staru sesiju, pa obriši `state.stream`.
4. **Spajanje.** `StreamSession::start(host, port, 5 s)`.
   - Ako uspije: stavi handle u **slot** (od tog trenutka izlaz svira), spremi sesiju i upiše adresu u `state`. Vrati `success: true`.
   - Ako ne uspije: ispiše grešku i vrati `success: false` s porukom.

Primijeti razliku: **neispravan zahtjev** vraća gRPC grešku (`Err(Status)`), a **neuspjelo spajanje** vraća normalan odgovor sa `success: false`.

### 5.5 `amp-audio/src/snapcast.rs`: omot oko snapcast klijenta

`SnapcastClientHandle` je naš omot (*wrapper*) oko `snapcast-client` biblioteke.

```rust
pub struct SnapcastClientHandle {
    command_tx: mpsc::Sender<ClientCommand>,     // šalje naredbe klijentu (npr. Stop)
    time_provider: Arc<Mutex<TimeProvider>>,     // razlika sata Amp ↔ server
    stream: Arc<Mutex<Stream>>,                  // red dekodiranih chunkova + sync logika
    task: Mutex<Option<JoinHandle<()>>>,         // tokio task u kojem klijent radi
}
```

**`spawn(host, port)`**:

1. Napravi `SnapClient` s adresom servera.
2. Uzme si kopije (`Arc::clone`) njegovog `time_provider` i `stream`. Klijent ih puni, a mi iz njih čitamo.
3. `tokio::spawn(client.run())` pokrene klijenta u pozadini: on se spaja, prima poruke, dekodira FLAC i puni `stream`.
4. Vrati handle i `event_rx`, kanal kroz koji klijent javlja događaje (`Connected`, `StreamStarted`, `Disconnected`...).

**`format()`** vrati trenutni format streama, npr. `48000:32:2`. Prije nego server pošalje header, vraća `0:0:0`.

**`pull_synced_f32(output, dac_latency)`** je srce audio dijela. Zove se iz audio callbacka:

```rust
let diff_usec = self.time_provider.lock()...diff_to_server_usec();
let server_now_usec = snapcast_proto::time::now_usec() + diff_usec;   // "sad" po satu servera
```

Zatim:

```rust
let mut stream = self.stream.lock()...;
let frames = (output.len() / channels) as u32;          // koliko frameova kartica traži
let mut raw_bytes = vec![0u8; frames as usize * frame_size];
let has_audio = stream.get_player_chunk_or_silence(server_now_usec, dac_latency_usec, &mut raw_bytes, frames);
```

`get_player_chunk_or_silence` (iz biblioteke) radi sve iz poglavlja [2.7](#27-kako-snapcast-sinkronizira-više-zvučnika): izračuna `age`, napravi hard ili soft sync i upiše točno one bajtove koji trebaju svirati **sada**. Ako ih nema, upiše tišinu (nule).

Na kraju se ti sirovi bajtovi pretvore u `f32` za karticu, ovisno o formatu:

```rust
match encoding {
    SampleEncoding::Float32 => { /* svaka 4 bajta → f32::from_le_bytes */ }
    SampleEncoding::PcmInt => match format.bits() {
        16 => { /* 2 bajta → i16 → / i16::MAX */ }
        24 => { /* 4 bajta → i32 → / PCM_24BIT_MAX */ }
        32 => { /* 4 bajta → i32 → / i32::MAX */ }
        _ => output.fill(0.0),   // nepoznat format → tišina
    },
}
```

`as_chunks::<N>()` reže niz bajtova na komade od točno N bajtova. Kod nas (FLAC) se uvijek ide granom `Float32`.

**`stop()`** pošalje klijentu `Stop`, prekine (`abort`) njegov task i pričeka da završi.

**Test** na dnu datoteke (`#[cfg(test)]`) pokrene pravi snapserver u memoriji (`snapcast-server` crate), pošalje 20 chunkova i provjeri dvije stvari: da klijent javi `StreamStarted` s formatom `48000 Hz / 2 kanala` i da u `stream` stignu chunkovi.

### 5.6 `amp-audio/src/session.rs`: jedna veza na snapserver

```rust
pub type PlayerSlot = Arc<Mutex<Option<Arc<SnapcastClientHandle>>>>;
```

`PlayerSlot` je "utičnica" iz [5.1](#51-amp-appsrcmainrs-početak-programa). Ovo je **`std::sync::Mutex`**, ne tokio, jer ga čita audio callback, koji nije async.

**`StreamSession::start(host, port, timeout)`**:

1. `SnapcastClientHandle::spawn(...)`.
2. Čeka događaje dok ne stigne `Connected` (uspjeh) ili `Disconnected` (neuspjeh), najdulje `timeout`. U slučaju greške ugasi klijenta i vrati `Err(poruka)`.
3. Nakon uspjeha pokrene task koji **prazni** `event_rx` dok ne dobije signal za stop (preko `oneshot` kanala). Tek tada pozove `handle.stop()`.

`tokio::select!` u tom tasku čeka dvije stvari odjednom: signal za stop ili sljedeći događaj. Događaji se trenutno ne obrađuju, samo se čitaju i bacaju.

**`stop()`** pošalje signal za stop i pričeka da task završi.

### 5.7 `amp-audio/src/output.rs`: zvučna kartica

**cpal** pojmovi:
- **Host** je audio sustav operacijskog sustava. Na Linuxu je to ALSA (PipeWire i PulseAudio se predstavljaju kao ALSA uređaj).
- **Device** je jedna zvučna kartica (`default_output_device()`).
- **StreamConfig** sadrži broj kanala, sample rate i veličinu buffera.
- **Stream** je otvoreni izlaz s callbackom. `play()` ga pokreće.

**`start_output(slot)`** pokrene thread `amp-audio-output`:

```rust
let mut current_format = None;   // format za koji je izlaz trenutno otvoren
let mut active_stream = None;    // otvoreni cpal stream (svira dok postoji)
loop {
    let format = slot.lock()...
        .as_ref()                            // ima li sesije?
        .map(|synced| synced.format())       // koji format šalje server?
        .filter(|format| format.rate() > 0); // 0 = header još nije stigao
    if format != current_format {
        drop(active_stream.take());                                     // zatvori stari izlaz
        active_stream = format.and_then(|format| open_stream(&slot, format));  // otvori novi
        current_format = format;
    }
    thread::sleep(FORMAT_POLL_INTERVAL);     // 50 ms
}
```

Svakih 50 ms provjeri koji format ima stream u slotu. Kad se promijeni, zatvori stari izlaz i otvori novi **točno na tom formatu**. Ako nema sesije, izlaz ostane zatvoren. Poseban thread je potreban jer `cpal::Stream` nije `Send` ([3.9](#39-threadovi-i-dijeljenje-podataka)) i jer stream mora negdje "živjeti".

**`open_stream(slot, format)`**:

```rust
let stream_config = cpal::StreamConfig {
    channels: format.channels(),                   // 2
    sample_rate: cpal::SampleRate(format.rate()),  // 48000, isto kao server
    buffer_size: cpal::BufferSize::Default,
};
```

Callback:

```rust
move |data: &mut [f32], _| {
    if let Ok(guard) = slot.try_lock()         // try_lock: callback nikad ne smije čekati
        && let Some(synced) = guard.as_ref()   // ima li streama?
    {
        synced.pull_synced_f32(data, DAC_LATENCY_USEC);
        return;
    }
    data.fill(0.0);                            // inače tišina
}
```

`data` je buffer koji kartica treba **odmah**. Callback ga mora popuniti, sa zvukom ili s nulama.

**Redoslijed lockova.** I poll petlja i callback zaključavaju prvo `slot`, pa onda `stream` unutar handlea. Redoslijed je uvijek isti, pa nema deadlocka.

`DAC_LATENCY_USEC = 0`: zasad se ne javlja prava latencija kartice, pa zvuk kasni za nekoliko ms u odnosu na idealno. `chain.rs` pokazuje kako se ona može izračunati iz `OutputCallbackInfo`.

### 5.8 `amp-audio/src/chain.rs`: alternativna implementacija (ne koristi se)

`AudioPipeline` radi isto što i `session.rs` + `output.rs` zajedno, samo drugačije: na događaj `StreamStarted` šalje format kroz kanal threadu koji otvara cpal. Usput računa i pravu DAC latenciju (`dac_delay_usec`). **Nitko ga ne poziva.** Vidi [poglavlje 8](#8-što-je-mrtvo-prazno-ili-nedovršeno).

### 5.9 `amp-ui`: Qt sučelje

**QML** je Qt-ov jezik za opis sučelja, nešto između JSON-a i JavaScripta. Rust i QML su povezani preko crate-a `qtbridge`.

**`build.rs`** prođe kroz `shared/qml/` i `amp-ui/qml/Amp/` i generira datoteku s listom:

```rust
pub const SHARED_QML_FILES: &[(&str, &[u8])] = &[
    ("Amplify/Theme.qml", include_bytes!("/puna/putanja/Theme.qml")),
    ...
];
```

Tako su sve QML datoteke **ugrađene u binarnu datoteku**, pa se program na Raspberry Piju može pokrenuti bez ikakvih drugih datoteka.

**`lib.rs`**:
- `include!(... "embedded_qml.rs")` uključi gore generirani kod.
- `extract_qml` kod pokretanja zapiše te datoteke u privremenu mapu, npr. `/tmp/amplify-shared-qml/`, jer ih Qt želi čitati s diska.
- `run(state)` napravi `QApp`, registrira Rust objekt `Greeter` da ga QML vidi, doda mape s QML modulima, učita `main.qml` i pokrene Qt. `run` blokira dok se prozor ne zatvori. `state` se zasad ne koristi (`_state`).

**`backend.rs`**: `Greeter` je primjer Rust objekta vidljivog u QML-u. Ima svojstvo `message` = `"Hello from Rust!"`.
- `#[qobject(Singleton)]` znači da u QML-u postoji jedna globalna instanca.
- `qproperty!("message", Read = message, Constant)` znači da QML smije čitati `Greeter.message` i da se vrijednost ne mijenja.

**QML datoteke:**
- `qml/main.qml` je prozor 1480×320: lijevo `Sidebar`, desno `StackLayout` sa stranicama (Now Playing, Channel, Levels, Equalizer). `sidebar.currentIndex` određuje koja je stranica vidljiva.
- `qml/Amp/Sidebar.qml` je bočna navigacija.
- `qml/Amp/pages/*.qml` su stranice, zasad uglavnom prazne.
- `qml/Amp/qmldir` je popis što QML modul `Amp` izvozi.
- `shared/qml/Amplify/` sadrži komponente koje dijele Amp i Maestro: `Theme` (boje), `Icons`, gumbe, slider, `AmplifyTrackInfo`...

---

## 6. Put jedne pjesme od Maestra do zvučnika

Ovo je cijeli tok, korak po korak.

**Pokretanje**

1. Amp: `main()` napravi prazan `state` i `slot`, pokrene thread `amp-audio-output` (slot je prazan, pa je izlaz zatvoren), gRPC thread na portu 50051 i Qt prozor.
2. Maestro: `SnapcastBootstrap` pokrene Docker kontejner sa snapserverom. Konfiguracija snapservera je:
   ```
   source = tcp://0.0.0.0:4953?name=Local&mode=server&sampleformat=48000:16:2&codec=flac
   ```
   To znači: "slušaj na portu 4953 i primaj sirovi PCM u formatu 48000:16:2, a klijentima ga šalji kao FLAC na portu 1704".

**Handshake**

3. Maestro: `AmpConnection.HandshakeAsync()` pošalje Ampu `Handshake(snapcast_host = <svoja IP adresa>, snapcast_port = 1704)`.
4. Amp: `AmpHandler::handshake` provjeri zahtjev i pozove `StreamSession::start`. Snapcast klijent se spoji na Maestro:1704 i dobije `Connected`.
5. Amp: handle ide u `slot`, adresa u `state`, a Maestro dobije `success: true`.
6. Snapserver klijentu pošalje **CodecHeader** (FLAC header s formatom). `snapcast-client` iz njega napravi novi `Stream` s formatom `48000:32:2`.
7. Amp: thread `amp-audio-output` u roku od 50 ms vidi novi format i otvori cpal izlaz na **48000 Hz, 2 kanala**. Od tog trenutka kartica zove callback, ali chunkova još nema, pa svira tišinu.

**Puštanje pjesme**

8. Maestro: `LocalFileSource.PlayAsync` se TCP-om spoji na snapserver:4953 i pokrene:
   ```
   ffmpeg -re -i sample.mp3 -vn -f s16le -ar 48000 -ac 2 pipe:1
   ```
   - `-re` čita realnim tempom (1 sekunda pjesme u 1 sekundi);
   - `-f s16le` daje 16-bitni PCM;
   - `-ar 48000` resampla s 44.1 na 48 kHz;
   - `-ac 2` daje stereo.

   Izlaz ffmpega (`pipe:1` = stdout) Maestro kopira u TCP vezu prema snapserveru.
9. Snapserver PCM reže na chunkove, svakom da timestamp, kodira ga u FLAC i šalje svim klijentima.
10. Amp: `snapcast-client` prima chunkove, dekodira FLAC u `f32` i stavlja ih u red u `Stream`.
11. Amp: otprilike svakih 10 ms kartica pozove callback, a callback pozove `pull_synced_f32`. Ona izračuna serverovo "sad", uzme točno odgovarajuće frameove iz reda (s hard ili soft syncom) i pretvori ih u `f32`.
12. Kartica pretvori brojeve u napon i čuje se zvuk.

---

## 7. Bug "svira 1 sekundu"

**Simptom:** glazba svira otprilike sekundu, onda prekid ili skok, pa opet.

**Uzrok:** `output.rs` je izlaz prije otvarao s `device.default_output_config()`, dakle s "defaultnim" postavkama kartice. cpal na ALSA hostu **namjerno bira 44100 Hz** kad ga kartica podržava (`cpal-0.15.3/src/host/alsa/mod.rs:483`). PipeWire ga podržava, iako sam radi na 48 kHz.

Tako je nastala ovakva situacija:
- server šalje 48000 frameova po sekundi;
- kartica troši samo 44100 frameova po sekundi.

**Zašto baš ~1 s:** svake sekunde Amp "zaostane" za 48000 − 44100 = 3900 frameova, što je oko **81 ms**. `age` iz [2.7](#27-kako-snapcast-sinkronizira-više-zvučnika) raste za 81 ms u sekundi. Za otprilike 0.6–1 s medijan zadnjih 20 mjerenja prijeđe **50 ms** (a medijan zadnjih 100 mjerenja, koji se napuni za otprilike 1 s, prijeđe **5 ms**), pa snapcast napravi **hard sync**: baci zastarjele chunkove i skoči naprijed. Sve krene ispočetka, pa se ponavlja otprilike svake sekunde. Usput je i ono što se čulo bilo ~8 % sporije i dublje.

**Popravak:** `output.rs` sad čeka da server javi format i otvara izlaz **točno na sample rateu i broju kanala streama** ([5.7](#57-amp-audiosrcoutputrs-zvučna-kartica)). Općenito pravilo: **kartica i izvor moraju biti na istom sample rateu, ili netko mora resamplati.**

---

## 8. Što je mrtvo, prazno ili nedovršeno

Da ne trošiš vrijeme na razumijevanje nečega što ništa ne radi:

| Što | Stanje |
|---|---|
| `amp-audio/src/chain.rs` | radi, ali se ne koristi; duplikat `session.rs` + `output.rs` |
| `amp-audio/src/{crossover,delay,eq,volume}.rs` | prazne i nisu ni uključene u `lib.rs` (planirana DSP obrada) |
| `amp-ui/src/models.rs` | prazna |
| `amp-audio/tests/session.rs` | samo zakomentirani testovi |
| `amp/deploy/amp.service`, `install.sh` | prazne |
| `fundsp`, `anyhow`, `amp-core`, `amp-ui` u `amp-audio/Cargo.toml` | ovisnosti koje `amp-audio` ne koristi |
| `amp-ui`: parametar `_state` u `run` | UI zasad ne čita stanje |
| `amp-ui/backend.rs` `Greeter` | primjer "Hello from Rust!" |
| `DAC_LATENCY_USEC = 0` | prava latencija kartice se ne mjeri |
| Događaji iz snapcast klijenta (npr. `VolumeChanged`) | `session.rs` ih čita i baca |
| Ponovno spajanje | ako veza na snapserver pukne, Amp se sam ne spaja ponovno; treba novi handshake |

---

## 9. Pojmovnik

| Pojam | Značenje |
|---|---|
| **sample** | jedan broj, vrijednost signala u jednom trenutku za jedan kanal |
| **frame** | sampleovi svih kanala za isti trenutak (stereo: L + D) |
| **sample rate** | frameova u sekundi (44100, 48000 Hz) |
| **bit depth** | koliko bitova ima jedan sample (16, 24, 32) |
| **PCM** | sirovi, nekomprimirani niz sampleova |
| **interleaved** | kanali isprepleteni u memoriji: L D L D... |
| **kodek** | algoritam sažimanja audia (MP3, FLAC, Opus) |
| **resampling** | preračunavanje audia na drugi sample rate |
| **chunk** | komad audia s vremenskom oznakom (Snapcast) |
| **buffer** | zaliha podataka unaprijed; i "koliko kasnije sviramo" (Snapcast `buffer_ms`) |
| **latencija** | kašnjenje od podataka do zvuka |
| **DAC** | čip koji brojeve pretvara u napon |
| **callback** | funkcija koju netko drugi (ovdje kartica) zove kad treba podatke |
| **underrun** | kartica je tražila podatke, a nije ih dobila na vrijeme (pucketanje) |
| **age** | koliko kasnimo (> 0) ili žurimo (< 0) u odnosu na server |
| **hard sync** | nagli skok na pravo mjesto (čuje se) |
| **soft sync** | ubacivanje ili izbacivanje pojedinačnih sampleova (ne čuje se) |
| **crate** | Rust paket (biblioteka ili program) |
| **workspace** | više crateova u jednom buildu (kao .sln) |
| **trait** | Rust interface |
| **ownership / move / borrow** | vlasništvo nad vrijednošću, premještanje, posudba (vidi 3.4) |
| **drop** | uništenje vrijednosti kad vlasnik nestane |
| **`Arc`** | dijeljeno vlasništvo među threadovima |
| **`Mutex` / `RwLock`** | zaštita podataka od istovremenog pristupa |
| **`Send`** | tip se smije premjestiti na drugi thread |
| **panic** | neoporavljiva greška; ruši thread |
| **async / `.await`** | funkcija koja se može pauzirati dok čeka |
| **tokio** | async runtime za Rust |
| **task** | lagana async "dretva" unutar tokija |
| **gRPC / protobuf** | pozivanje funkcija preko mreže / format poruka za to |
| **QML** | Qt jezik za opis sučelja |
| **ALSA / PipeWire** | Linux audio sustav / moderni audio server iznad njega |
| **cpal** | Rust biblioteka za pristup zvučnoj kartici |
