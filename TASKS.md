# Echo du Karma — Backlog & tâches

Document de suivi actionnable. Synthèse complète : **[AUDIT.md](AUDIT.md)**.

**État global : ~73 % du gameplay RPG** (vertical slice Intro ~80 %). Voir **[AUDIT.md](AUDIT.md)** (26 mai 2026).

---

## Légende statuts

| Statut | Signification |
|--------|---------------|
| ✅ | Fait (à valider en playtest si besoin) |
| 🔶 | Partiel |
| ⬜ | À faire |

---

## P0 — Boucle combat (TERMINÉ)

| ID | Tâche | Statut |
|----|-------|--------|
| P0.1 | Brancher `BattleEnded` → `GameManager.OnBattleEnded` | ✅ |
| P0.2 | Retour map + zone (`ReturnScenePath`, `MapLoader`, snapshot) | ✅ |
| P0.3 | Soft-lock MP insuffisants (`CancelPlayerActionAndShowMenu`) | ✅ |
| P0.4 | XP victoire + level up (`GrantBattleExperience`) | ✅ |
| P0.5 | Playtest boucle Marchand (checklist manuelle) | ⬜ |

### P0.5 — Checklist playtest

| Étape | Action attendue |
|-------|-----------------|
| 1 | Map Intro → Marchand → « Aider » (karma ≥ 10) |
| 2 | Combat 2× Rat → victoire |
| 3 | Retour map, dialogues OK, PNJ « merci » si quête done |
| 4 | XP / or / objet quête / karma cohérents |
| 5 | Re-combat sans double signal / crash |
| 6 | MP insuffisant → pas de blocage |
| 7 | Défaite → map, 1 PV |
| 8 | Fuite → retour map |

---

## P1 — Fiabilisation combat (TERMINÉ)

| ID | Tâche | Statut |
|----|-------|--------|
| P1.1 | `ReturnScenePath` / `ReturnZoneName` mémorisés | ✅ |
| P1.2 | Défaite : retour map, 1 PV / 0 MP sur snapshot | ✅ |
| P1.3 | Doublon `PlayerDamage` sur `BattleHud` | ✅ |
| P1.4 | `SpawnPlayer` : erreur si instantiate null | ✅ |
| P1.5 | PV ennemis copiés depuis `_enemyStatsSource` | ✅ |
| P1.6 | Mort ennemi 3D (`Enemy.PlayDefeatAnimation`) | ✅ |

---

## P2 — Enrichissement combat

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P2.1 | Caméra magie / soin (`ExecuteMagicAction`) | Basse | ✅ |
| P2.2 | Attaque joueur : await `BattleActor.PlayAttackAnimation` | Basse | ✅ |
| P2.3 | IA ennemie (`Aggressive` / `Defensive`) | — | ✅ |
| P2.4 | **Loot post-combat** depuis `bestiary.csv` colonne LOOT | **Haute** | ✅ |
| P2.5 | Règles fuite (pas XP) documentées / testées | Basse | ✅ |
| P2.6 | Scénarios de régression combat documentés | Basse | ✅ |
| P2.7 | Remplacer `FindChild("BattleManager")` par `[Export]` ou groupes | Moyenne | ✅ |

### P2.4 — Loot post-combat (détail)

**Problème** : `EnemyStats.Loot` chargé mais jamais distribué après victoire.

**À faire**

1. Parser le loot (ex. `"Gelée, Fleur de gobi"` ou nom unique).
2. À la victoire : roll ou distribution garantie par ennemi vaincu.
3. Appeler `InventoryManager.TryAddItem` + feedback HUD / toast.

**Fichiers** : `Global/Bestiary.cs`, `Scripts/Battle/BattleManager.cs` (`HandleVictory`).

---

## P3 — Progression & personnage

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P3.1 | Filtrer skills par `LevelRequired` + niveau actuel | **Haute** | ⬜ |
| P3.2 | Classe joueur dynamique (plus `"Magus"` en dur) | **Haute** | ⬜ |
| P3.3 | Snapshot combat : inclure bonus équipement dans stats | Moyenne | ✅ |
| P3.4 | Déblocage skill à la montée de niveau (signal / log) | Basse | ⬜ |
| P3.5 | Paladin jouable (progression CSV + classe) | Basse | ⬜ |

**Référence** : `Scripts/Entities/Player/Player.cs`, `Global/PlayerBattleSnapshot.cs`.

---

## P4 — Économie & inventaire

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P4.1 | **Boutique marchand** (acheter / prix CSV) | **Haute** | ✅ |
| P4.2 | Modificateur prix selon karma zone (GDD) | Moyenne | ✅ |
| P4.3 | Vendre des objets | Basse | ✅ |
| P4.4 | Consommables utilisables (map ou combat) | Basse | ⬜ |
| P4.5 | Stack ressources identiques (si design le prévoit) | Basse | ⬜ |

**Référence** : `Global/InventoryManager.cs`, `Datas/Persos/equipments.csv`, `_GDD/GDD_systeme_karma.md`.

---

## P5 — Karma (monde)

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P5.1 | Karma combat + dialogues + quêtes + kills | — | ✅ |
| P5.2 | UI `KarmaBanner` | — | ✅ |
| P5.3 | **Effets monde GDD** : marchands apathiques (+70) | Moyenne | ⬜ |
| P5.4 | Auberges / soins hors combat selon karma | Moyenne | ⬜ |
| P5.5 | Exploitation cristaux → baisse karma | Basse | ⬜ |
| P5.6 | Spawns / types ennemis selon seuils karma | Basse | ⬜ |

**Référence** : `_GDD/GDD_systeme_karma.md`, `Global/KarmaManager.cs`.

---

## P6 — Quêtes & narration

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P6.1 | `QuestManager` + journal UI + kills | — | ✅ |
| P6.2 | `DialogueConditions` (QUEST, KARMA) | — | ✅ |
| P6.3 | PNJ dialogues conditionnels (`ConditionalStartIds`) | — | ✅ |
| P6.4 | Compléter quête Intro (`QUEST_INTRO` 3 étapes) en playtest | Moyenne | ⬜ |
| P6.5 | Exploiter `DIAL_LINK` quêtes → dialogues post-quête | Basse | ⬜ |
| P6.6 | Quêtes secondaires supplémentaires (CSV) | Moyenne | ⬜ |
| P6.7 | Échec / abandon de quête | Basse | ⬜ |

**Référence** : `Datas/Progress/quests.csv`, `Scripts/UI/QuestJournalPage.cs`.

---

## P7 — Monde & contenu

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P7.1 | Implémenter `TELEPORT` / `CHANGE_SCENE` dans `GameManager` | Moyenne | ⬜ |
| P7.2 | Deuxième zone + CSV dialogues / quêtes | Moyenne | ⬜ |
| P7.3 | Ennemis bestiaire supplémentaires + loot | Moyenne | ⬜ |
| P7.4 | Contenu GDD zone Intro (`_GDD/GDD_histoire_zone.md`) | Moyenne | ⬜ |

---

## P8 — Persistance (CRITIQUE)

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P8.1 | **SaveManager** autoload (save/load slot) | **Critique** | ⬜ |
| P8.2 | Persister : stats, XP, inventaire, or, équipement | **Critique** | ⬜ |
| P8.3 | Persister : karma par zone, états quêtes | **Critique** | ⬜ |
| P8.4 | Persister : position joueur + scène courante | Haute | ⬜ |
| P8.5 | Auto-save à changement de zone / fin combat | Moyenne | ⬜ |

**Impact** : sans P8, progression perdue à chaque fermeture → bloque expérience RPG réelle.

---

## P9 — Qualité & architecture

| ID | Tâche | Priorité | Statut |
|----|-------|----------|--------|
| P9.1 | Supprimer / isoler code mort (`CSVLoader`, `LevelStats`, `Camera.cs` 2D) | Basse | ⬜ |
| P9.2 | Aligner formule dégâts sur GDD ou documenter l’écart | Basse | ⬜ |
| P9.3 | Migrer données critiques vers Resources (.tres) — optionnel | Basse | ⬜ |
| P9.4 | `.gitignore` pour `.DS_Store` | Basse | ⬜ |

---

## Ordre recommandé (2026)

```text
P0.5  Playtest Intro (validation)
  ↓
P3.1–P3.2  Skills par niveau + classe dynamique
  ↓
P8.1–P8.3  Sauvegarde (stats, inventaire, karma, quêtes)
  ↓
P7.x  Contenu (zone 2, ennemis, quêtes)
  ↓
P5.3+  Karma monde (GDD)
  ↓
P9.x  Dette technique (.gitignore, code mort, anchors combat)
```

**Objectif court terme (~78 % gameplay)** : P0.5 validé + save minimale + skills par niveau.

**Objectif moyen terme (~82 %)** : 2e zone + effets karma monde légers + Paladin.

---

## Fichiers centraux (référence rapide)

| Système | Fichiers |
|---------|----------|
| Orchestration | `Global/GameManager.cs` |
| Combat | `Scripts/Battle/BattleManager.cs`, `Scripts/UI/BattleHud.cs` |
| Joueur / stats | `Scripts/Entities/Player/Player.cs`, `Scripts/Data/StatHandler.cs` |
| Snapshot combat | `Global/PlayerBattleSnapshot.cs` |
| Dialogues | `Global/DialogueSystem.cs`, `Global/DialogueConditions.cs` |
| Quêtes | `Global/QuestManager.cs`, `Datas/Progress/quests.csv` |
| Karma | `Global/KarmaManager.cs`, `Global/KarmaCombatModifiers.cs` |
| Inventaire | `Global/InventoryManager.cs`, `Scripts/UI/InventoryPage.cs` |
| Données | `Datas/Bestiary/`, `Datas/Persos/`, `Datas/Progress/` |

---

*Dernière mise à jour : 26 mai 2026 — aligné sur [AUDIT.md](AUDIT.md).*
