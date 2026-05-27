# Données de référence (CSV) — Echo Du Karma

Tables sous `Datas/`. **Séparateur** : vérifier chaque fichier (`;` ou `,`).

---

## `Datas/Persos/heroes.csv` (`;`)

| ID | Name | Classe | Affinity |
| :--- | :--- | :--- | :--- |
| 1 | Player | Magus | Fire |

Chargé par `HeroManager` : affinité combat, filtre des compétences par classe.

---

## `Datas/Persos/skills.csv` (`;`)

| Nom | Type | Coût PM | Puissance | Élément | Vitesse | Classes | Level requis |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Flammeche | Attack | 4 | 2 | Fire | 10 | Magus | 1 |
| Soin | Support | 6 | 3 | Water | 10 | Magus, Paladin | 7 |
| Stalagtite | Attack | 8 | 3 | Earth | 6 | Magus | 3 |
| Renforcement | Support | 8 | 5 | Earth | 5 | Magus | 10 |

Colonnes complètes : `Nom;Type;Description;Coût PM;Puissance;Élément;Vitesse;Effet special;Type de cible;Classes;Level requis`

**Joueur** : `SkillManager.GetUnlockedForClass(classe, niveau)`.

**Ennemis** : `SkillManager.GetByName` / `ResolveByKeys` — **sans** filtre classe/niveau ; clés listées dans `bestiary.csv` → `Skills`.

---

## `Datas/Persos/equipments.csv` (`;`)

| Nom | Slot | Force | Agi | Esprit | Defense | Classes | Prix |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Bâton | Main | 2 | 5 | 2 | 0 | Magus\|Paladin | 10 |
| Bâton usé | Main | 0 | 5 | 5 | 0 | Magus | 15 |
| Bâton Commun | Main | 0 | 5 | 7 | 0 | Magus | 100 |
| Gourdin | Main | 3 | 7 | 0 | 0 | Paladin | 20 |
| Bâton de DEV | Main | 100 | 100 | 100 | 100 | Magus\|Paladin | 0 |

Bonus fusionnés dans `Player.Dexterity` etc. via `InventoryManager.GetEquipmentBonuses()`.

---

## `Datas/Persos/Magus/progression-mage.csv` (`,`)

Colonnes : `Seuil XP, Niveau, Multiplicateur, PV, PM, Force, Esprit, Agi, Def`

| Niveau | Seuil XP | PV | PM | Force | Esprit | Agi | Def |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | 10 | 30 | 30 | 4 | 14 | 7 | 4 |
| 2 | 40 | 32 | 31 | 4 | 15 | 7 | 4 |
| 3 | 90 | 35 | 33 | 4 | 16 | 8 | 4 |
| 4 | 160 | 38 | 34 | 4 | 17 | 8 | 4 |
| 5 | 250 | 42 | 36 | 5 | 18 | 9 | 4 |

*(Niveaux 6–100 : voir fichier complet.)*

---

## `Datas/Bestiary/bestiary.csv` (`;`)

Métadonnées communes à tous les niveaux d'un ennemi. Les stats par niveau sont dans un fichier dédié (`rat.csv`, `gobi.csv`, …).

| Nom | XP | IA | LOOT | Affinity | Skills |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Rat | 2 | Aggressive | Peau de rat | Earth | Stalagtite\|Renforcement |
| Gobi | 5 | Defensive | Gelée, Fleur de gobi | Water | Soin |

### Colonnes

| Colonne | Description |
| :--- | :--- |
| `Nom` | Identifiant ennemi (nom du CSV progression = `{nom}.csv` en minuscules) |
| `XP` | Récompense fixe par espèce (indépendante du niveau de l'instance) |
| `IA` | `Normal`, `Aggressive`, `Defensive` → voir [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md) |
| `LOOT` | Objets séparés par **virgule** (distribution victoire) |
| `Affinity` | `Fire`, `Water`, `Earth`, `Air` |
| `Skills` | Sorts planifiables : `Nom1\|Nom2` (noms = `skills.csv`) |

Chargé par `Bestiary` au démarrage ; fusionné avec la progression via `GetEnemyAtLevel(nom, niveau)`.

---

## `Datas/Bestiary/{ennemi}.csv` (`;`)

Progression par niveau (1 fichier par espèce, ex. `rat.csv`, `gobi.csv`).

Colonnes : `Niveau;Multiplicateur;PV;PM;Force;Esprit;Agi;Def`

| Niveau | Multiplicateur | PV | PM | Force | Esprit | Agi | Def |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | 1 | 15 | 30 | 5 | 14 | 12 | 2 |
| 2 | 1,040 | 16 | 31 | 5 | 15 | 12 | 2 |
| 3 | 1,113 | 17 | 33 | 5 | 16 | 13 | 2 |

*(Niveaux 4–100 : voir fichiers complets.)*

* `Dexterity` en combat = colonne `Agi`
* Si le niveau demandé est absent, fallback sur le niveau le plus proche inférieur

---

## `Datas/Progress/{Zone}/enemies.csv` (`;`)

Table de spawn par zone (niveaux des ennemis rencontrés). Colonne **Spawn Rate** présente dans le CSV mais **non utilisée** pour l'instant.

| Name | Levels | Spawn Rate |
| :--- | :--- | :--- |
| Rat | 1-3 | 60 |

### Colonnes

| Colonne | Description |
| :--- | :--- |
| `Name` | Nom bestiaire (`bestiary.csv`) |
| `Levels` | Plage inclusive (`1-3`) ou niveau fixe (`2`) |
| `Spawn Rate` | *(réservé — non branché)* |

Chargé par `ZoneEnemyCatalog.LoadZone(zone)` à l'entrée de map (`MapLoader` → `GameManager.SetMapContext`).

Lors d'un combat dialogue (`BATTLE:Rat:2`), chaque instance tire un niveau aléatoire dans la plage de la zone courante. Ennemi absent de la table → niveau **1**.

### Initiative ennemi (rappel)

| Action planifiée | Initiative |
| :--- | :--- |
| Sort `X` | Agi + `Vitesse` du sort |
| Mêlée | Agi seule |
| Défense | Agi seule |

Ex. Rat niv. 1 + Stalagtite : **18** (12+6). Gobi niv. 2 + Soin : **18** (8+10). Gobi niv. 2 mêlée : **8**.

**Loot** : une entrée par type d'ennemi dans la liste de combat (pas de tirage %).

⚠️ Casing : bestiaire `Peau de rat` vs ressource `Peau de Rat` — harmoniser pour le loot.

---

## `Datas/Persos/resources.csv` (`;`)

| Nom | Type |
| :--- | :--- |
| Peau de Rat | Matériau |
| Gelée | Matériau |
| Fleur de gobi | Matériau |

---

## `Datas/Progress/quests.csv` (`;`)

| ID | Type | Zone | Complétion | Récompenses |
| :--- | :--- | :--- | :--- | :--- |
| QUEST_INTRO | PRINCIPAL | Introduction | ALL_STEPS (3 étapes) | 100 XP, 20 or, +10 karma |
| QUEST_MARCHANDER_01 | ANNEXE | Introduction | KILL:Rat:2 | 10 XP, 20 or, Peau de Rat, +5 karma |

**QUEST_INTRO — étapes** : Aider le marchand · Parler Sage Karma (`PNJ_KARMA_01`) · Lire livre (`PNJ_LIVRE_01`).

**QUEST_MARCHANDER_01** : `DIALOGUE:MARCHAND_AIDE` ; kills via `QuestManager.NotifyKill`.

---

## `Datas/Progress/shops.csv` (`;`)

| ShopId | Équipements |
| :--- | :--- |
| MARCHAND_INTRO | Bâton usé, Bâton Commun |

Hook dialogue : `SHOP:MARCHAND_INTRO`.

---

## `Datas/Progress/Introduction/dialogues.csv` (`;`)

Colonnes : `ID;TYPE DIALOGUE;PNJ;TEXTE;CONDITION ACCES;ACTION POST DIALOGUE;LIEN SUIVANT`

**Types** : `TEXT`, `CHOICE`

**Actions** (`GameManager`) :

| Token | Format | Exemple |
| :--- | :--- | :--- |
| BATTLE | `BATTLE:ennemis:quantités` | `BATTLE:Rat:2` · `BATTLE:Rat\|Gobi:2\|1` |
| SHOP | `SHOP:ShopId` | `SHOP:MARCHAND_INTRO` |
| KARMA | `KARMA:delta` ou `KARMA:zone:delta` | `KARMA:+10` |
| GOLD | `GOLD:montant` | `GOLD:50` |
| ITEM | `ITEM:nom` | `ITEM:Peau de Rat` |
| LEVEL_UP | `LEVEL_UP:n` | `LEVEL_UP:1` |
| TELEPORT | stub | log uniquement |
| CHANGE_SCENE | stub | log uniquement |

**Tutoriels** : `PNJ_KARMA_01`–`11`, `PNJ_LIVRE_01`–`04`, SOS marchand.

---

## Fichiers non branchés

| Fichier | Note |
| :--- | :--- |
| `Datas/Bestiary/EdK.csv` | Ancien format monolithique, non référencé |

---

## Voir aussi

* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)
* [`GDD_combat.md`](GDD_combat.md)
* [`GDD_economie_progression.md`](GDD_economie_progression.md)
