# Dialogues et quêtes — Echo Du Karma

Zone documentée en données : **Introduction** (`Datas/Progress/Introduction/dialogues.csv`).

---

## 1. Chargement

* `MapLoader.ZoneName` → `DialogueSystem.LoadZoneDialogues(zone)` au `_Ready` de la map
* Chaînage : colonne `LIEN SUIVANT` (TEXT linéaire, CHOICE multi-branches)
* UI : `Scripts/UI/Dialogue.cs` · BBCode supporté dans `TEXTE`

---

## 2. Conditions d’accès

Évaluées par `DialogueConditions` (libellés dans CSV, séparateur `|` pour AND implicite selon implémentation).

| Exemple | Sens |
| :--- | :--- |
| `interaction joueur` | Le joueur a déclenché le PNJ |
| `proximité joueur` | Zone de proximité |
| `KARMA:>=10` | Karma zone courant ≥ 10 |
| `QUEST_ACTIVE:QUEST_MARCHANDER_01` | Quête en cours |
| `QUEST_DONE:QUEST_MARCHANDER_01` | Quête terminée |

Échec : message optionnel côté PNJ (`ConditionalStartIds` sur prefab NPC — ex. marchand en cours / terminé / boutique).

---

## 3. Actions post-dialogue

Parser : `GameManager.OnActionTriggered` — format `CLE:arg1:arg2…`

| Clé | Statut | Détail |
| :--- | :--- | :--- |
| `BATTLE` | ✅ | `BATTLE:Rat:2` ou multi `BATTLE:Rat\|Gobi:2\|1` |
| `SHOP` | ✅ | `SHOP:MARCHAND_INTRO` |
| `KARMA` | ✅ | `KARMA:+10` ou `KARMA:Introduction:-5` |
| `GOLD` | ✅ | `GOLD:50` |
| `ITEM` | ✅ | `ITEM:nom_ressource` |
| `LEVEL_UP` | ✅ | `LEVEL_UP:1` → signal `PlayerLevelUp` |
| `TELEPORT` | ❌ stub | Log console |
| `CHANGE_SCENE` | ❌ stub | Log console |

---

## 4. Quêtes (`Datas/Progress/quests.csv`)

**`QuestManager`** :
* Étapes `STEPS` séparées par `|`, sous-étapes `~`
* Complétion `ALL_STEPS` ou condition type `KILL:Rat:2`
* Triggers : `DIALOGUE:id`, kills via `NotifyKill`
* Journal UI : liste + détail (`QuestJournalPage`)

### QUEST_INTRO (principale)

| Étape | Condition |
| :--- | :--- |
| Aider le marchand | `QUEST_DONE:QUEST_MARCHANDER_01` |
| Se renseigner sur le Karma | `DIALOGUE:PNJ_KARMA_01` |
| Lire le livre sacré | `DIALOGUE:PNJ_LIVRE_01` |

Récompense : 100 XP, 20 or, +10 karma.

### QUEST_MARCHANDER_01 (annexe)

* Trigger : dialogue `MARCHAND_AIDE` (après choix « Aider »)
* Complétion : tuer **2 Rats** (`KILL:Rat:2`)
* Récompense : 10 XP, 20 or, `Peau de Rat`, +5 karma
* Lien dialogue suite : `INTRO_SOS_01` / marchand reconnaissant / boutique

---

## 5. Flux marchand (Intro)

```text
INTRO_SOS_01 → INTRO_SOS_02 (CHOIX : Aider / Partir)
  → MARCHAND_AIDE → BATTLE:Rat:2
  → (combat) → MARCHAND_DONE_01 → MARCHAND_DONE_02 (CHOIX boutique)
      → MARCHAND_SHOP_OPEN → SHOP:MARCHAND_INTRO
```

Choix « Partir » : branche `MARCHAND_FUITE` (karma non augmenté via quête).

---

## 6. PNJ tutoriels

| Chaîne | IDs | Sujet |
| :--- | :--- | :--- |
| Sage du Karma | `PNJ_KARMA_01` – `11` | Jauge, stats, combat karma, **éléments** (double cycle) |
| Livre sacré | `PNJ_LIVRE_01` – `04` | Lois karma (narratif) |
| Test | `PNJ_TEST*`, `TEST_BATTLE*` | Debug combat / karma / level |

---

## 7. Karma dialogue ↔ gameplay

* Choix marchand peut exiger `KARMA:>=10` (branche alternative)
* Actions `KARMA:±n` dans dialogues de test
* Quêtes : colonne `KARMA_IMPACT` appliquée à la complétion

Karma initial zone Introduction : **+15** (voir `KarmaManager.EnsureZoneInitialized`).

---

## Voir aussi

* [`GDD_INDEX.md`](GDD_INDEX.md)
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md) — comportement Rat / Gobi en combat

---

## Voir aussi

* [`GDD_donnees_reference.md`](GDD_donnees_reference.md) — tables CSV brutes
* [`GDD_histoire_zone.md`](GDD_histoire_zone.md) — arcs narratifs prévus
* [`GDD_INDEX.md`](GDD_INDEX.md) — boucle globale
