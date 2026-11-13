# Tower Defense: Κλασικό vs AR

**Η επίδραση της Επαυξημένης Πραγματικότητας στο mobile gaming: συγκριτική ανάλυση του UX μεταξύ AR και κλασικών παιχνιδιών για κινητά, με βάση ένα παιχνίδι στρατηγικής Tower Defense.**

Το repository περιέχει:

1. **Ένα παιχνίδι Tower Defense για Android σε δύο εκδόσεις** (Unity 6, URP, AR Foundation 6 / ARCore):
   - **Κλασική έκδοση:** το πεδίο της μάχης εμφανίζεται στην οθόνη και ο χρήστης ελέγχει μια εικονική κάμερα (σύρσιμο για μετακίνηση, τσίμπημα για ζουμ).
   - **Έκδοση AR:** το ίδιο ακριβώς πεδίο τοποθετείται πάνω σε πραγματική επιφάνεια (τραπέζι ή πάτωμα) και ο χρήστης κινεί το κινητό ή τον εαυτό του για να το δει.
2. **Εργαλεία έρευνας ενσωματωμένα στην εφαρμογή:** αυτόματη καταγραφή αντικειμενικών μετρικών, ερωτηματολόγια (SUS, iGEQ, ειδικές ερωτήσεις) μετά από κάθε συνεδρία, τελικό συγκριτικό ερωτηματολόγιο, αντιστάθμιση της σειράς (counterbalancing) και εξαγωγή σε CSV.
3. **Scripts στατιστικής ανάλυσης** (Python) που παράγουν πίνακες, ελέγχους υποθέσεων και γραφήματα για την εργασία ([`Analysis/`](Analysis/)).
4. **Τεκμηρίωση της μεθοδολογίας**: [`Docs/METHODOLOGY.md`](Docs/METHODOLOGY.md) και [`Docs/QUESTIONNAIRES.md`](Docs/QUESTIONNAIRES.md).

---

## Το παιχνίδι

| | |
|---|---|
| Είδος | Tower Defense (στρατηγική) |
| Στόχος | Να αντέξει η βάση **10 κύματα** εχθρών |
| Πόροι | 100 νομίσματα στην αρχή, +10 για κάθε εχθρό που νικιέται |
| Πύργος | 20 νομίσματα, εμβέλεια 8, στοχεύει τον εχθρό που βρίσκεται πιο κοντά στη βάση |
| Βάση | 10 πόντοι ζωής, −1 για κάθε εχθρό που φτάνει |
| Δυσκολία | Κάθε κύμα έχει +1 εχθρό, +20% ζωή και +3% ταχύτητα |
| Χειρισμός | Πάτημα σε ελεύθερο σημείο του χορταριού = χτίσιμο πύργου (προεπισκόπηση με πράσινο/κόκκινο «φάντασμα» και κύκλο εμβέλειας) |

Και οι δύο εκδόσεις χρησιμοποιούν **τον ίδιο κώδικα για την πίστα, τους εχθρούς, την οικονομία και το UI**
(`LevelBuilder`, `WaveManager`, `TowerPlacement`, `GameHUD`). Διαφέρει μόνο ο τρόπος που ο παίκτης
βλέπει και αγγίζει τον κόσμο. Έτσι η σύγκριση αφορά την επίδραση της AR και όχι διαφορές σχεδιασμού.

Στην AR έκδοση το πεδίο έχει πλάτος 0,9 m (`ARGameSetup.boardWidthMeters`). Για να είναι ίδιο το πεδίο σε
μονάδες του κόσμου, κλιμακώνεται το XR Origin και όχι το περιεχόμενο. Αυτή είναι η τεχνική που προτείνει το AR Foundation.

## Ροή της μελέτης μέσα στην εφαρμογή

1. **Κεντρικό μενού:** ο ερευνητής ορίζει τον κωδικό συμμετέχοντα (P001, P002, …). Η εφαρμογή προτείνει τη σειρά των εκδόσεων:
   οι μονοί κωδικοί ξεκινούν από την κλασική και οι ζυγοί από την AR.
2. **Συνεδρία παιχνιδιού:** οδηγίες, μετά «Έναρξη» και 10 κύματα. Στην AR προηγούνται η σάρωση του χώρου και η τοποθέτηση του πεδίου.
3. **Ερωτηματολόγιο μετά τη συνεδρία:** SUS (10 ερωτήσεις), iGEQ (14) και 5 ειδικές ερωτήσεις.
4. Τα ίδια για τη δεύτερη έκδοση.
5. **Τελικό ερωτηματολόγιο** από το μενού: σύγκριση/προτίμηση και δημογραφικά.

Η επιλογή «Ερωτηματολόγια μετά το παιχνίδι: ΟΧΙ» επιτρέπει ελεύθερο παιχνίδι, π.χ. για εκπαίδευση ή επίδειξη.
Στις συνεδρίες αυτές οι μετρικές καταγράφονται, αλλά με `questionnaire_completed = 0`.

## Δεδομένα που καταγράφονται

Τα αρχεία γράφονται στον φάκελο `Application.persistentDataPath/UXStudy/`. Στο Android αυτός είναι ο
`/sdcard/Android/data/<package>/files/UXStudy/`. Η πλήρης διαδρομή εμφανίζεται και στο κεντρικό μενού.

```bash
adb pull /sdcard/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/UXStudy ./Analysis/data
```

| Αρχείο | Περιεχόμενο |
|---|---|
| `sessions.csv` | Μία γραμμή ανά συνεδρία: όλες οι μετρικές, η βαθμολογία SUS, οι παράγοντες iGEQ και οι ειδικές ερωτήσεις |
| `answers.csv` | Κάθε απάντηση ερωτηματολογίου (long format) |
| `events.csv` | Χρονολόγιο γεγονότων (κύματα, τοποθετήσεις, απώλεια tracking, παύσεις, …) |
| `final.csv` | Τελικό συγκριτικό ερωτηματολόγιο και δημογραφικά |
| `session_<id>.csv` | Αντίγραφο ασφαλείας κάθε συνεδρίας |

Οι μετρικές περιγράφονται αναλυτικά στο [`Docs/METHODOLOGY.md`](Docs/METHODOLOGY.md#4-μετρήσεις).
Ενδεικτικά: χρόνος προετοιμασίας, ποσοστό λαθών τοποθέτησης, χρόνος έως τον πρώτο πύργο, αγγίγματα ανά λεπτό,
απώλειες tracking, φυσική μετακίνηση της συσκευής, FPS και κατανάλωση μπαταρίας.

## Ανάλυση

```bash
cd Analysis
pip install -r requirements.txt
python analyze_ux.py --data data --out results     # παράγει results/report.md, summary_stats.csv, figures/
```

Για να δοκιμάσετε τη διαδικασία πριν συλλέξετε πραγματικά δεδομένα, υπάρχει το
`python generate_synthetic_data.py --out synthetic_data`. Παράγει **συνθετικά** δεδομένα και η αναφορά που προκύπτει σημειώνεται ρητά ως τέτοια.

---

## Άνοιγμα και build

**Απαιτήσεις:** Unity **6000.3.10f1** με Android Build Support, και συσκευή Android με
[υποστήριξη ARCore](https://developers.google.com/ar/devices) (Android 10+) για την AR έκδοση.

1. Ανοίξτε το project στο Unity Hub.
2. Με το πρώτο άνοιγμα, το editor script `Assets/Editor/ProjectSetup.cs` προσθέτει αυτόματα:
   - τις σκηνές `MainMenu`, `Game_Classic`, `Game_AR` στο Build Profile,
   - το **AR Background Renderer Feature** στους URP renderers (χωρίς αυτό η κάμερα του κινητού δεν εμφανίζεται).
3. Εκτελέστε το **Tools → AR Tower Defense → Setup Project**. Ρυθμίζει για Android OpenGLES3, landscape, IL2CPP/ARM64, min SDK 29 και Application Entry Point = Activity.
4. Εκτελέστε (μία φορά) το **Tools → AR Tower Defense → Add AR Rig To Game_AR Scene**. Προσθέτει στη σκηνή `Game_AR` τα έτοιμα αντικείμενα του Unity **AR Session** και **XR Origin (Mobile AR)** και αποθηκεύει τη σκηνή. Κάντε commit το `Game_AR.unity`.
5. Ελέγξτε το **Project Settings → XR Plug-in Management → Android** (το ARCore είναι ήδη ενεργό) και το **Project Validation**.
6. Στο **File → Build Profiles → Android**: *Switch Platform* και μετά *Build And Run*.

### Δοκιμή στον editor

- Η **κλασική έκδοση** παίζει κανονικά με το ποντίκι: αριστερό κλικ για χτίσιμο, σύρσιμο ή δεξί κουμπί για μετακίνηση, ροδέλα για ζουμ.
- Για την **AR έκδοση** στον editor ενεργοποιήστε το **XR Simulation** στο *XR Plug-in Management → Windows/Mac/Linux*.
- Ξεκινήστε πάντα από τη σκηνή `MainMenu`. Μπορείτε όμως να ανοίξετε και απευθείας μια σκηνή παιχνιδιού.

## Αρχιτεκτονική κώδικα

Ο `SceneBootstrapper` στήνει από κώδικα την πίστα, το UI και τους managers όταν φορτώνει κάθε σκηνή.
Εξαίρεση είναι το AR rig (AR Session, XR Origin με κάμερα): αυτό είναι τα έτοιμα αντικείμενα του Unity στη σκηνή `Game_AR`.

```
Assets/Scripts/
├── Core/        StudySession (συμμετέχων, σειρά, αλλαγή σκηνών), SceneBootstrapper, GameBootstrap
├── Level/       LevelBuilder (κοινή πίστα), LevelLayout (grid, διαδρομή, επιτρεπτά κελιά), GameMaterials
├── Input/       PointerInput: ενιαία αγγίγματα/ποντίκι (tap, drag, pinch) για τις δύο εκδόσεις
├── Classic/     ClassicCameraController: pan/zoom εικονικής κάμερας
├── AR/          ARGameSetup: ανίχνευση επιπέδων, τοποθέτηση πεδίου, tracking (πάνω στο AR rig της σκηνής)
├── Tower/       TowerPlacement (χτίσιμο με έλεγχο εγκυρότητας), TowerAttack
├── Enemy/       EnemyMovement (waypoints), EnemyHealth, EnemyHealthBar
├── UI/          GameHUD, MainMenuUI, QuestionnaireUI, UIFactory, SafeAreaFitter
├── Research/    SessionTelemetry (μετρικές), Questionnaires (SUS, iGEQ, …), DataStore (CSV)
└── GameManager, GameEconomy, WaveManager, Bullet, Target
```

| Κλάση | Ρόλος |
|---|---|
| `GameManager` | Καταστάσεις (`WaitingToStart`, `Playing`, `Paused`, `Victory`, `Defeat`, `Aborted`), ζωή βάσης, παύση συστήματος (π.χ. απώλεια tracking) |
| `WaveManager` | 10 κύματα, κλιμάκωση δυσκολίας, λίστα ενεργών εχθρών, συνθήκη νίκης |
| `TowerPlacement` | Raycast μόνο στο collider του πεδίου, κούμπωμα στο grid, απαγόρευση χτισίματος στη διαδρομή, ορατή προεπισκόπηση |
| `ARGameSetup` | Χρησιμοποιεί το AR Session / XR Origin της σκηνής `Game_AR` και ρυθμίζει το XR Origin ώστε το σημείο που πάτησε ο χρήστης να γίνει το κέντρο του πεδίου, με το πεδίο στραμμένο προς αυτόν |
| `SessionTelemetry` | Καταγραφή μετρικών και γεγονότων, εγγραφή CSV |
