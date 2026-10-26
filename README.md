# 2.5D Dungeon Adventure

First-Person-Dungeon-Crawler mit Sprite-Gegnern (2.5D), gebaut mit **Unity 6000.4.3f1**.

Hinweis:
**Neue Szene?** In *File → Build Profiles* (Scene List) eintragen, sonst lädt `SceneLoader.Load()` sie nicht.

---

## Ordnerstruktur

| Ordner | Inhalt |
|---|---|
| `Assets/Scripts/` | Code, ein Unterordner pro System (siehe unten) |
| `Assets/Scenes/` | `Levels/`, `MenuScreens/`, `CutScenes/` |
| `Assets/Prefabs/` | Player, Gegner, NPCs, Türen, Truhen, Items, UI … |
| `Assets/InGameText/` | Dialoge, Quests, Tutorials (ScriptableObjects) |
| `Assets/ItemObjects/` | Items (`ItemsSOs/`) und Erfolge (`Achievements/`) |
| `Assets/Resources/` | `SaveRegistry` und `AchievementRegistry` – **nicht umbenennen/verschieben** |

---

## Grundregeln

- **Szenen wechseln:** immer `SceneLoader.Load("Szenenname")`, nie `SceneManager.LoadScene` (sonst kein Ladebildschirm).
- **Level-Namen:** `Level 1.2` gehört zu `Level 1` (alles vor dem Punkt). Zeit, Statistik und Erfolge laufen über alle Teil-Szenen eines Levels.
- **Schaden:** über `DifficultySettings.DamageByPlayer/DamageByEnemy` skalieren, damit der Schwierigkeitsgrad wirkt.
- **Nach jeder Level-Änderung:** `Tools → Save System → 1. Setup Open Scene` ausführen und Szene speichern (siehe Savesystem).
- **Eingabe:** über `GameInput.Instance`, Aktionen stehen in `Assets/InputSystem_Actions.inputactions`.

---

## Savesystem (`Scripts/SaveSystem/`)

**Wie es funktioniert**
- `SaveManager` erzeugt sich selbst beim Spielstart – nicht in eine Szene legen.
- 10 manuelle Slots + 4 Autosave-Slots. Autosave alle 3 Minuten (nicht im Kampf).
- Dateien: `%USERPROFILE%/AppData/LocalLow/<GameJam>/<Spielname>/Saves/slot_X.json` (`Tools → Save System → Open Save Folder`).
- **Speichern** sammelt: alle Objekte mit `SaveableEntity` (aktiv/inaktiv + Komponentenzustand), zerstörte Objekte und zur Laufzeit gedroppte Items.
- **Laden** lädt die Szene neu, wartet bis alle `Start()` gelaufen sind und setzt dann die Zustände.
- Im Hardcore-Modus kann nicht gespeichert werden.

**Ein eigenes Script speichern lassen**

```csharp
public class Lever : MonoBehaviour, ISaveable
{
    private bool _pulled;

    [Serializable]
    private struct SaveState { public bool pulled; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { pulled = _pulled });

    public void RestoreState(string state) => _pulled = JsonUtility.FromJson<SaveState>(state).pulled;
}
```

Danach `Tools → Save System → 1. Setup Open Scene` → das Objekt bekommt automatisch eine `SaveableEntity` mit ID.

**Nur aktiv/zerstört speichern** (z.B. Tor, das per `SetActive` verschwindet): `[SaveActiveState]` an das `GameObject`-Feld oder an die Klasse schreiben, dann Setup ausführen.

**Logik nach dem Laden**, die von anderen Objekten abhängt: `ISaveLoadListener.OnGameLoaded()` implementieren.

**Wichtig**
- Die `SaveableEntity`-ID **nie von Hand ändern** – alte Spielstände finden das Objekt sonst nicht mehr.
- Objekte mit Strg+D kopiert? Setup erneut ausführen, es repariert doppelte IDs.
- Neues Item oder neue Quest angelegt? Setup ausführen, damit sie in die `SaveRegistry` kommen (sonst fehlen sie nach dem Laden).
- Änderst du das Format (`SaveGameData`), neue Felder so wählen, dass ein fehlender Wert in alten Saves sinnvoll ist, und `CurrentVersion` erhöhen.

---

## Quests (`Scripts/Questsystem/`)

**Neue Quest erstellen**
1. Rechtsklick in `Assets/InGameText/QuestsText/` → *Create → Quests → Quest*.
2. Felder ausfüllen:
   - **Title / Goal Text** – Anzeige im Quest-UI.
   - **Type** – `ReachZone`, `Collect`, `Talk`, `Kill`, `Destroy`.
   - **Target Id** – frei wählbarer Text, muss **exakt** mit dem meldenden Objekt übereinstimmen.
   - **Required Amount** – wie oft gemeldet werden muss (z.B. 5 Nerven).
   - **Next Quest** – startet automatisch nach Abschluss (Questketten).
   - **Can Be Skipped** – wird übersprungen, wenn eine spätere Quest der Kette schon erfüllt wird.
   - **Start Tutorial** – optional, wird beim Start angezeigt.
3. `Tools → Save System → 1. Setup Open Scene` (Quest in die Registry).

**Quest starten**
- Am Level-Start: `QuestStarter` in die Szene, Quest reinziehen.
- Nach einem Gespräch: `QuestTalkTarget` → *Quest To Give*.
- Per Code: `QuestManager.Instance.StartQuest(quest)`.

Die Szene braucht einen `QuestManager` und das `QuestUI`-Prefab.

**Wer meldet Fortschritt?**

| Type | Komponente | Target Id kommt aus |
|---|---|---|
| `ReachZone` | `QuestTriggerZone` (Collider als Trigger) | Feld *Target Id* |
| `Collect` | `QuestCollectible` | Feld *Target Id* |
| `Talk` | `QuestTalkTarget` am NPC (neben `DialogueInteractable`) | Feld *Target Id* |
| `Kill` | `DefeatReward` am Gegner, Boss | Feld *Quest Target Id* |
| `Destroy` | `Nerve`, `HeartObjective` | Feld *Quest Target Id* |

Eigenes Objekt: `QuestManager.Report(QuestType.Kill, "MeineId");`

**Tor öffnen bei Quest-Fortschritt:** `QuestGate` – Quest + Fortschritt eintragen, Tor-Objekt zuweisen.

---

## Dialoge (`Scripts/DialogueSystem/`)

1. *Create → Dialogue → Entry* in `Assets/InGameText/DialogueText/`: Sprecher, Textzeilen, Sprites, optional *Despawn At End* (NPC verschwindet danach).
2. NPC-Prefab aus `Prefabs/NPCs/` nehmen oder `DialogueInteractable` an ein Objekt hängen und den Dialog zuweisen.
3. Szene braucht das `Dialogue`-UI-Prefab.

Ende eines Gesprächs per Code: Event `DialogueInteractable.DialogueFinished`.

## Tutorials (`Scripts/TutorialSection/`)

*Create → Tutorial → Tutorial* in `Assets/InGameText/TutorialText/` (Titel + Seiten). Anzeigen über `TutorialTriggerZone`-Prefab oder als *Start Tutorial* einer Quest. Szene braucht das `TutorialUI`-Prefab.

## Cutscenes (`Scripts/CutsceneSystem/`)

*Create → New Cutscene → Cutscene* mit den Dialogzeilen. Eigene Cutscene-Szene: `CutsceneInitiator` + `CutsceneManager` in die Szene, *Next Scene To Load* eintragen.

---

## Items & Waffen (`Scripts/ItemSystem/`)

**Neues Item**
1. *Create → New Item → Item* (oder Weapon / Ammo) in `Assets/ItemObjects/ItemsSOs/`.
2. Prefab in `Prefabs/Items/` anlegen mit `ItemBase`, das SO in *Item Data* ziehen.
3. `Tools → Save System → 1. Setup Open Scene` (Item + Prefab in die Registry).

**Schlüssel/Tür:** Bei `Door` *Is Locked* an und bei *Needed Unlock Item* den **`ItemName`** des Schlüssels eintragen. Optional *Scene To Load On Unlock* für Level-Ausgänge.

**Loot:** `Chest` → *Item Prefab*. Gegner-Drop: `DefeatReward` → *Loot Prefab*.

---

## Gegner (`Scripts/Enemy/`)

Fertige Prefabs in `Prefabs/Enemies/` nehmen. Gegner laufen per NavMesh – nach Änderungen an der Level-Geometrie das NavMesh neu backen. Patrouillen über `EnemyPatrol` + `Prefabs/PatrolPath/`. Unverschlossene Türen können Gegner selbst öffnen.

---

## Erfolge (`Scripts/Achievements/`)

1. *Create → Achievements → Achievement* in `Assets/ItemObjects/Achievements/`. **Die ID ist der Asset-Name.**
2. In `Assets/Resources/AchievementRegistry` in die Liste ziehen (Reihenfolge = Anzeige).
3. Trigger wählen:
   - **LevelEnd** – *Level* (z.B. `Level 1`) setzen und Bedingungen anhaken (kein Schaden, alle Items, max. Minuten …). Wird auf dem Level-Abschluss-Screen geprüft.
   - **Code** – im Spiel freischalten: `AchievementManager.Unlock("AssetName")`. Zähler-Erfolg: *Progress Target* setzen und `AchievementManager.AddProgress("AssetName")`.

Erfolge liegen getrennt von den Spielständen in `achievements.json` und bleiben beim Löschen von Saves erhalten.

---

## Schwierigkeit

`Easy`, `Normal`, `Hard`, `Hardcore` (wie Hard, ein Leben, kein Speichern – freigeschaltet nach Level 1 auf Schwer). Wirkt nur über Schadens-Multiplikatoren, kann also jederzeit gewechselt werden. Für Erfolge zählt die leichteste im Level benutzte Stufe.

---

## Neues Level – Checkliste

- [ ] Szene in `Assets/Scenes/Levels/` als `Level X` (Teil-Szenen `Level X.Y`).
- [ ] Player-Prefab, `QuestManager`, UI-Prefabs (`QuestUI`, `Dialogue`, `TutorialUI`, `PauseMenu`, …) rein. Am einfachsten ein bestehendes Level kopieren.
- [ ] `QuestStarter` mit der ersten Quest.
- [ ] NavMesh backen.
- [ ] `Tools → Save System → 1. Setup Open Scene`, dann Strg+S.
- [ ] Szene in die Build-Liste eintragen.
- [ ] Ausgang: `Door` mit *Scene To Load On Unlock* oder `SceneLoader.Load(...)`.

---

## Editor-Tools (Menü `Tools`)

| Menü | Zweck |
|---|---|
| Save System → 1. Setup Open Scene | IDs vergeben, Registry aktualisieren |
| Save System → 2. Create Save Menu In Scene | Speichern/Laden-Menü bauen |
| Save System → 3. Add Menu Buttons | Speichern/Laden-Buttons neben einen ausgewählten Button setzen |
| Save System → Open Save Folder / Delete All Saves | Saves ansehen / löschen |
| Pause Menu → 1. / 2. | Pause-Menü-Prefab erstellen / in Szene ersetzen |
| Difficulty, Level Select, Achievements, Level Complete | Jeweiliges Menü bzw. Szene generieren |

---

## Häufige Probleme

| Problem | Lösung |
|---|---|
| Objekt nach dem Laden falsch / wieder da | Setup Open Scene ausführen, Szene speichern |
| Item fehlt nach dem Laden | Item nicht in der SaveRegistry → Setup ausführen |
| Quest geht nicht weiter | *Target Id* in Quest und Objekt vergleichen (Groß-/Kleinschreibung!) |
| Szene lädt nicht | Nicht in der Build-Liste |
| Gegner bewegen sich nicht | NavMesh fehlt / nicht neu gebacken |
| Alte Saves kaputt | `Tools → Save System → Delete All Saves` |
