# Echo du Karma — Documentation gameplay (GDD)

**Moteur** : Godot 4.6 · C# · GL Compatibility · 1920×1080  
**Dernière sync code** : mai 2026 — reflète l’état du dépôt (CSV, combat, karma, boutique, quêtes Intro).

Ce dossier `_GDD/` est la **source de vérité design** alignée sur l’implémentation. Pour l’audit d’avancement et la dette technique, voir aussi [`AUDIT.md`](../AUDIT.md) et [`docs/COMBAT_REGRESSION.md`](../docs/COMBAT_REGRESSION.md).

---

## Index des documents

| Document | Contenu |
| :--- | :--- |
| [`GDD_INDEX.md`](GDD_INDEX.md) | Ce fichier — vue d’ensemble et liens |
| [`GDD_donnees_reference.md`](GDD_donnees_reference.md) | **Toutes les tables CSV** et séparateurs |
| [`GDD_combat.md`](GDD_combat.md) | Combat : états, rounds, dégâts, butin, fin de combat |
| [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md) | Initiative, phases choix / exécution, panneau HUD |
| [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md) | **IA ennemis** : sorts, mêlée, défense, profils Aggressive / Defensive / Normal |
| [`GDD_systeme_karma.md`](GDD_systeme_karma.md) | Jauge karma, stats, dégâts subis, soins |
| [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md) | Affinités, cycle, formules élémentaires |
| [`GDD_economie_progression.md`](GDD_economie_progression.md) | Or, inventaire, boutique, XP, compétences, équipement |
| [`GDD_dialogues_quetes.md`](GDD_dialogues_quetes.md) | Dialogues, actions, conditions, quêtes zone Intro |
| [`GDD_histoire_zone.md`](GDD_histoire_zone.md) | Synopsis, zones prévues, lien narratif ↔ gameplay |

---

## Périmètre jouable actuel

| Élément | Valeur |
| :--- | :--- |
| Zone implémentée | **Introduction** (`Maps/Intro/Map.tscn`) |
| Scène combat | `Maps/Battles/Basic.tscn` |
| Héros jouable | **Magus** (Feu), `heroes.csv` id 1 |
| Ennemis | **Rat** (niv. 1–3 en Intro), **Gobi** — métadonnées `bestiary.csv`, stats `rat.csv` / `gobi.csv` |
| Compétences (CSV) | 4 ; déblocage par **niveau + classe** (joueur) ; ennemis via colonne **Skills** |
| Quêtes CSV | 2 (`QUEST_INTRO`, `QUEST_MARCHANDER_01`) |
| Boutique | `MARCHAND_INTRO` (2 équipements) |
| Karma initial (Intro) | **+15** (`KarmaManager`) |
| Sauvegarde | **Non** (état perdu au redémarrage) |

---

## Autoloads (managers globaux)

| Manager | Fichier | Rôle |
| :--- | :--- | :--- |
| `GameManager` | `Global/GameManager.cs` | Combat, retour map, actions dialogue, récompenses quête |
| `DialogueSystem` | `Global/DialogueSystem.cs` | CSV dialogues par zone, choix, signaux |
| `Bestiary` | `Global/Bestiary.cs` | Métadonnées bestiaire + progression par niveau |
| `QuestManager` | `Global/QuestManager.cs` | Quêtes, kills, journal, `ALL_STEPS` |
| `KarmaManager` | `Global/KarmaManager.cs` | Jauge −100…+100 par zone |
| `InventoryManager` | `Global/InventoryManager.cs` | Or, équipement, ressources, achat/vente |

---

## Boucle de gameplay (vertical slice Intro)

```text
Exploration (3D, caméra, herbe procédurale, LPC)
    → Interaction PNJ (dialogue CSV + conditions karma/quête)
        → Choix / Combat (BATTLE:nom:quantité) / Boutique (SHOP:id) / Karma (KARMA:±delta)
            → Combat : rounds (choix → exécution par initiative) → XP + loot + karma −0,15 / kill
            → Retour map → quête / marchand / menus (inv, stats, skills, journal)
```

**Quête marchand (jouable)** : `INTRO_SOS_01` → aider → `BATTLE:Rat:2` → victoire → `MARCHAND_DONE` → `SHOP:MARCHAND_INTRO`.

---

## Stats de base (convention projet)

`PV`, `PM`, `Force`, `Esprit`, `Agi`, `Def` — progression par classe via `Datas/Persos/<Classe>/progression-*.csv`.

---

## Systèmes croisés (résumé)

| Système | Interaction |
| :--- | :--- |
| Karma | Modifie stats combat, dégâts subis, soins, prix boutique |
| Éléments | Multiplie dégâts/soins (magie + mêlée) selon affinité et cycle |
| Initiative | Round : choix (aperçu HUD) puis exécution triée par initiative |
| IA ennemis | Planifie sort / mêlée / défense selon `IA` + `Skills` + PV/PM |
| Équipement | Bonus stats → joueur et `PlayerBattleSnapshot` en combat |
| Quêtes | `KILL:`, `DIALOGUE:`, récompenses XP/or/objet/karma |

---

## Non implémenté (GDD vs code)

* Sauvegarde / chargement
* Paladin jouable, multi-zones (`TELEPORT`, `CHANGE_SCENE` = logs seulement)
* Effets karma sur exploration (spawns, auberges, apathie PNJ hors boutique)
* Loot probabiliste, consommables, craft
* Affinité affichée sur HUD ennemi
* Sorts exclusifs ennemis (tous partagent `skills.csv` joueur)
* Spawn aléatoire monde (`Spawn Rate` dans `enemies.csv`)

---

*Maintenir ce index à jour lors de tout ajout CSV ou changement de formule combat.*
