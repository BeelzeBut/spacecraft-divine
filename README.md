# Spaceship Divine

**Proiect de diplomă** — Universitatea Politehnica Timișoara
Facultatea de Automatică și Calculatoare, programul de studii Calculatoare și Tehnologia Informației
Sesiunea septembrie 2026

**Candidat:** Marco Buga
**Coordonator științific:** Prof. Răzvan Ciorga

---

## Despre proiect

*Spaceship Divine* este un joc mobil 2D de tip *roguelite*, cu perspectivă de sus, dezvoltat integral de autor în Unity. Jucătorul alege o navă spațială și parcurge niveluri generate procedural, formate din camere populate cu inamici, până la confruntarea finală.

Aplicația a fost publicată în Google Play în 2021 și a rămas disponibilă până în aprilie 2024. A fost portată și pe iOS, unde a fost distribuită prin TestFlight, fără a fi publicată în App Store.

### Componente implementate de autor

| Componentă | Descriere |
|---|---|
| Generare procedurală | Algoritm în două etape: schelet topologic prin plimbare aleatorie, apoi instanțierea camerelor |
| Navigație | Implementare proprie a algoritmului A\*, cu heap binar și planificator de cereri |
| Inteligență artificială | 30 de tipuri de inamici, dintre care 6 confruntări finale, derivate dintr-o clasă de bază comună |
| Sisteme de joc | 13 nave, 20 de abilități, 25 de îmbunătățiri, 20 de tipuri de proiectile |
| Progresie | Îmbunătățiri în cadrul parcurgerii și progresie persistentă între parcurgeri |
| Integrare mobilă | Control tactil, suport pentru controler, achiziții din aplicație, compilare și semnare |

Volum: **22.442 de linii de cod C#**, în 212 clase scrise de autor.

Detaliile de proiectare și de implementare sunt prezentate în lucrarea de diplomă care însoțește acest git repository.

---

## Cerințe

### Obligatoriu

- **Unity 2022.3.14f1** (versiune cu suport pe termen lung). Se instalează prin Unity Hub, din arhiva de versiuni.
  Se recomandă exact această versiune; alte versiuni pot declanșa o migrare automată a proiectului.
- **Git** pentru clonarea repository-ului.

### Pentru compilarea pe Android

- Modulul **Android Build Support** din Unity Hub, cu sub-componentele:
  - Android SDK & NDK Tools
  - OpenJDK

### Pentru compilarea pe iOS

- Modulul **iOS Build Support** din Unity Hub
- **macOS** cu **Xcode** instalat
- Un cont Apple pentru semnarea aplicației

### Pentru joc

- **Un controler** (recomandat) sau un dispozitiv cu ecran tactil.
  ⚠️ Jocul **nu poate fi controlat de la tastatură** — vezi secțiunea *Comenzi*.

---

## Structura repository-ului

```
Assets/
  Scripts/                    codul sursă propriu
    Grid1.cs, Node.cs, Heap.cs, Pathfinding1.cs, PathRequestManager.cs
                              sistemul de navigație A* (implementare proprie)
    Object Managers/          managerii de stare, generarea nivelului, jucătorul
    Enemies/                  clasa Enemy și cele 29 de specializări
    Bullets/                  clasa Bullet și cele 19 specializări
    Spaceships/               modelul navei și armamentul
    Upgrades/                 sistemul de îmbunătățiri
    Collectibles/             obiecte colectabile, planuri de construcție
    Tutorial/                 parcursul de instruire
    Main Menu/                meniul principal și magazinul
  Prefabs/                    prefabricate și resurse de configurare
    Player Abilities/         implementările celor 20 de abilități
    Upgrades Objects/         implementările celor 25 de îmbunătățiri
    Spaceships/               resursele celor 13 nave și armamentul lor
  Scenes/                     cele 7 scene incluse în versiunea finală
  Sprites/, Sounds/, Animations/, Shaders/, Tilesets/
                              conținut grafic și sonor
Packages/                     dependențele gestionate de motor
ProjectSettings/              configurația proiectului
```

Repository-ul este **public** și conține **exclusiv cod sursă și resurse**. Directoarele generate de motor (`Library/`, `Temp/`, `obj/`, `Logs/`) și fișierele binare compilate (`.apk`, `.aab`) sunt excluse prin `.gitignore`, conform cerințelor de predare.

---

## Deschiderea proiectului

```bash
git clone https://github.com/BeelzeBut/spacecraft-divine.git
cd spacecraft-divine
```

1. Se deschide Unity Hub → **Add** → se selectează directorul clonat.
2. Se deschide proiectul cu Unity **2022.3.14f1**.
3. **Prima deschidere durează 10–20 de minute.** Motorul importă peste 850 de prefabricate și peste 860 de imagini și reconstruiește directorul `Library/`, care nu este inclus în repository. Acest comportament este normal.
4. Se verifică platforma țintă: **File → Build Settings → Android → Switch Platform**.

### Rulare în editor

Se deschide scena `Assets/Scenes/Main Menu.unity` și se apasă **Play**.

---

## Comenzi

| Acțiune | Controler | Ecran tactil |
|---|---|---|
| Deplasare | Maneta stângă | Manetă virtuală (jumătatea stângă a ecranului) |
| Tragere principală | Trăgaci dreapta | Buton de tragere |
| Tragere secundară | Trăgaci stânga | Buton dedicat |
| Abilitate | Butoane frontale | Buton dedicat |
| Pauză | Start | Buton de interfață |

**Țintirea este automată** — nava se orientează singură către cel mai apropiat inamic. Jucătorul controlează deplasarea și momentul tragerii, nu direcția.

> **Important pentru rularea în editor:** proiectul nu conține legături pentru tastatură. Cu ajutorul șoarecelui se poate acționa maneta virtuală, dar numai un singur punct de contact simultan, deci **nu se poate trage în timpul deplasării**. Pentru o evaluare completă în editor se recomandă conectarea unui controler.

---

## Verificare rapidă a funcționalității

Cel mai rapid mod de a parcurge toate componentele aplicației este **modul de antrenament**, care permite construirea directă a oricărei confruntări, fără a parcurge un nivel întreg.

1. Meniu principal → **Training Mode**
2. Se aleg, pas cu pas: nava → abilitatea → biomul → tipul de confruntare
3. Pentru **confruntările finale**: se alege *Boss*, apoi oricare dintre inamicii finali ai biomului
4. Pentru **confruntările obișnuite**: se aleg dimensiunea camerei, tipurile de inamici, numărul de valuri și dificultatea

La eșec, configuratorul se redeschide în aproximativ o secundă, fără ecran de sfârșit de parcurgere.

### Vizualizarea sistemului de navigație

Traseele calculate de algoritmul A\* și grila de navigație pot fi inspectate direct în editor:

1. Se pornește o scenă de joc și se trece în fereastra **Scene**, cu **Gizmos** activate
2. Se selectează obiectul `A star` din ierarhie și se bifează **Display Grid Gizmos** pe componenta `Grid1`
   → celulele traversabile apar cu verde, cele blocate cu roșu
3. Traseele active ale inamicilor sunt desenate automat ca segmente

### Monitorizarea performanței

În scene există obiectul `[Graphy]`, dezactivat implicit. După activarea lui în ierarhie, se pot folosi:
- **Ctrl + G** — schimbă modul de afișare
- **Ctrl + H** — afișează sau ascunde suprapunerea

---

## Compilare pentru Android

1. **File → Build Settings** → platforma **Android**
2. Se verifică lista de scene; ordinea corectă este:
   `Main Menu`, `Tutorial`, `Training Mode`, `1`, `2`, `3`, `4`
3. **Player Settings** — configurația proiectului este deja stabilită:

   | Parametru | Valoare |
   |---|---|
   | Identificator aplicație | `com.KodaGames.SpaceshipDivine` |
   | Versiune minimă Android | API 22 (Android 5.1) |
   | Motor de scriptare | IL2CPP |
   | Arhitecturi | ARMv7 și ARM64 |
   | Nivel de compatibilitate | .NET Standard 2.1 |

4. **Build** → se generează un fișier `.apk`

> **Semnare.** Certificatul de semnare (`user.keystore`) **nu este inclus în repository**, din motive de securitate. Pentru o compilare de test nu este necesar: se dezactivează *Custom Keystore* din **Player Settings → Publishing Settings**, iar motorul va folosi certificatul implicit de depanare.

---

## Compilare pentru iOS

1. **File → Build Settings** → platforma **iOS** → **Switch Platform**
2. **Player Settings** → se completează identificatorul de pachet și echipa de dezvoltare Apple
3. **Build** → se generează un proiect Xcode
4. Se deschide proiectul generat în Xcode, se selectează dispozitivul și se compilează

> Achizițiile din aplicație nu au produse configurate pentru App Store. Dacă biblioteca de achiziții provoacă erori la compilare, componenta corespunzătoare poate fi dezactivată; ea nu afectează funcționarea jocului.

---

## Instalare și lansare

### Android

```bash
adb install -r SpaceshipDivine.apk
```

Alternativ, fișierul `.apk` se copiază pe dispozitiv și se deschide, după activarea instalării din surse necunoscute.

Lansarea se face din meniul de aplicații al dispozitivului.

### iOS

Instalarea se face direct din Xcode, pe un dispozitiv conectat și autorizat pentru dezvoltare.

---

## Componente terțe

Următoarele componente **nu** aparțin autorului și sunt utilizate ca dependențe:

| Componentă | Rol |
|---|---|
| Unity 2022.3 LTS și pachetele sale (URP, Input System, TextMesh Pro, Unity IAP, 2D Tilemap) | Motorul de joc |
| Joystick Pack | Maneta virtuală pentru ecran tactil |
| Graphy — Ultimate Stats Monitor | Monitorizarea performanței |
| Lightning Bolt Effect | Efect vizual de fulger |
| Sci-fi UI, SimplePixelUI | Elemente grafice de interfață |

O parte din materialul grafic a fost achiziționată sau comandată unor ilustratori externi; biblioteca de dale din care sunt compuse camerele a fost realizată la comandă. Coloana sonoră a fost compusă de un membru al comunității de testare.

**A\* Pathfinding Project** a fost importat și evaluat într-o etapă intermediară a dezvoltării, dar **nu a fost niciodată folosit în joc**, și a fost eliminat complet din versiunea finală. Navigația este implementată integral în `Assets/Scripts/`.

Toate celelalte sisteme — generarea procedurală, navigația, inteligența artificială a inamicilor, mecanicile de joc, progresia, interfața — sunt scrise integral de autor.

---

## Note și limitări cunoscute

- **Formularul de reacții** (`Assets/Scripts/Emailer.cs`) necesită un fișier de configurare `Assets/Resources/smtp_config.json`, care nu este inclus în repository. În absența lui, formularul se deschide și validează datele introduse, dar nu trimite mesajul. Un model se află în `smtp_config.example.json`.
- **Reclamele sunt dezactivate** în versiunea curentă a codului, ca urmare a modificărilor de interfață apărute la actualizarea bibliotecii.
- **Achizițiile din aplicație** sunt implementate, dar necesită produse configurate în consola magazinului pentru a funcționa.
- Aplicația **nu mai este disponibilă în Google Play**; a fost retrasă în aprilie 2024 pentru neactualizarea versiunii țintă a interfeței de programare Android.
- Jocul **nu are legături de tastatură** — vezi secțiunea *Comenzi*.

Limitările tehnice identificate în cod sunt discutate pe larg, cu propuneri de remediere, în secțiunile dedicate din lucrarea de diplomă.

---

## Contact

Marco Buga — bugamarco07@gmail.com
