# Combat — Echo Du Karma

Scène : `Maps/Battles/Basic.tscn` · Orchestration : `BattleManager` · HUD : `BattleHud`.

---

## 1. Machine à états

```
Setup → Selection → Action → Evaluation → (boucle) → Victory | Defeat
```

| État | Rôle |
| :--- | :--- |
| Setup | Spawn joueur (`BattleActor` LPC) + ennemis, karma combat, **round 1 — phase de choix** |
| Selection | **Joueur** : menu actions ; ennemis déjà planifiés (IA) |
| Action | Exécution animation + résolution (dégâts, soins, fuite…) |
| Evaluation | PV ≤ 0, nettoyage morts, tour suivant ou nouveau round |
| Victory | XP, loot, retour map |
| Defeat | Message, retour map à **1 PV / 0 PM** |

---

## 2. Rounds et initiative

Voir [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md) et [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md).

Résumé d’un **round** :

1. **Planification** — Chaque ennemi : sort, mêlée ou défense (`EnemyTurnPlanner` + `Skills` / `IA` du bestiaire).
2. **Choix joueur** — Menu ; panneau gauche trié en temps réel (initiative ennemis figée, joueur selon action survolée).
3. **Exécution** — Après validation, tri par initiative → chaque combattant joue son tour.
4. **Round suivant** jusqu’à victoire, défaite ou fuite réussie.

**UI** : `BattleInitiativeTrack` (portraits, initiative, ▶ tour actif, tours passés atténués).

---

## 3. Actions joueur

| Menu | Effet | Initiative |
| :--- | :--- | :--- |
| Attaque | Mêlée sur cible | Agi effective |
| Magie | Liste sorts débloqués (niveau + classe) | Agi + Vitesse sort |
| Défense | Réduit dégâts reçus /2 (tour) | Agi effective |
| Fuite | 50 % succès → fin combat sans XP/loot | Agi + 5 |

**Magie** : coût PM vérifié au commit ; Support = cible soi ; Attack = sélection cible ennemi.

**Caméras** (`CameraDirector`) : Neutral, PlayerAttack, PlayerMagic, EnemyAttack + fondu.

---

## 4. Actions ennemies

| Type | Source | Initiative |
| :--- | :--- | :--- |
| Sort Attack | `Skills` → `skills.csv` | Agi + Vitesse |
| Sort Support (soin) | idem | Agi + Vitesse |
| Mêlée | Secours (IA ou PM insuffisants) | Agi |
| Défense | Profil Defensive, PV bas | Agi |

Détails IA : [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md).

---

## 5. Formules de dégâts

Variance aléatoire : **×0,9 – ×1,1** sur les formules ci-dessous.

### Mêlée (joueur ou ennemi)

```
base = (ForceEffective / 2) − (DefCible / 4)
dégâts = max(1, round(base × variance × MultElement))
```

Joueur : `ForceEffective` inclut karma (`KarmaCombatModifiers`).  
Dégâts **reçus** par le joueur : `ApplyDamageTaken` selon karma zone.

Posture **défense** (joueur ou ennemi) : dégâts finaux `/ 2` (min 1).

**Aggressive** (exécution mêlée) : Force × **1,2** si PV ennemi &lt; 30 % max.

### Magie (attaque)

```
base = (Puissance × EspritAttaquant / 5) − (EspritCible / 4)
dégâts = max(1, round(base × variance × MultElement))
```

Ennemi → joueur : même formule, karma sur dégâts subis.

### Soins

**Joueur** :

```
base = Puissance + (EspritJoueur × 1,5)
soin = ApplyHealAmount(round(base × variance × synergieAffinité), karma)
```

**Ennemi** (Support planifié, cible soi) :

```
soin = max(1, Puissance + EspritEnnemi / 2)
```

`ApplyHealAmount` : ×0 si karma ≤ −69 (Chaos) — joueur uniquement.

### Éléments

Voir [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md) — `MultElement` sur magie et mêlée.

---

## 6. Fin de combat

### Victoire

* XP = somme `XpValue` de `_enemyStatsSource` (colonne `XP` de `bestiary.csv`, fixe par espèce)
* `GrantBattleExperience` → level up possible (`LevelUpPopup` en map)
* **Loot** : chaque entrée LOOT du bestiaire → `TryAddItem` (1× par nom, pas de drop rate)
* Karma : **−0,15** par monstre tué (`KarmaManager.KarmaLossPerMonsterKill`)
* `QuestManager.NotifyKill` par ennemi
* Retour `ReturnScenePath` avec snapshot appliqué au joueur

### Défaite

* Snapshot : **1 PV**, **0 MP**
* Retour map, pas d’XP ni loot

### Fuite

* 50 % réussite (`GD.Randf() > 0.5`)
* Réussite : pas XP/loot
* Échec : le round continue (exécution des tours restants)
* Karma / kills déjà appliqués **conservés**

---

## 7. Snapshot combat

`GameManager.PersistPlayerForBattle()` → `PlayerBattleSnapshot` :
* Stats + PM/PV courants + `LearnedSkills` filtrés par niveau
* Affinité (`heroes.csv` ou snapshot joueur)
* Bonus équipement inclus dans stats exposées

**Ennemis** : niveau tiré via `ZoneEnemyCatalog` (plage zone) + stats fusionnées `Bestiary.GetEnemyAtLevel`.  
`CurrentMp = max(Mp progression, Esprit×2, 8)` au spawn (`Enemy.InitializeFromBattleStats`).

---

## 8. Fichiers clés

| Fichier | Rôle |
| :--- | :--- |
| `Scripts/Battle/BattleManager.cs` | États, rounds, formules, victoire |
| `Scripts/Data/EnemyTurnPlanner.cs` | Planification IA ennemis |
| `Scripts/Data/CombatInitiative.cs` | Initiative |
| `Scripts/Data/ElementCombat.cs` | Éléments |
| `Scripts/Data/SkillManager.cs` | Catalogue sorts (joueur + ennemis) |
| `Global/KarmaCombatModifiers.cs` | Karma combat |
| `Global/Bestiary.cs` | Métadonnées bestiaire + progression par niveau |
| `Scripts/Data/ZoneEnemyCatalog.cs` | Plages de niveaux par zone |
| `Scripts/UI/BattleHud.cs` | Menu, logs, initiative, dégâts flottants |
| `Scripts/Battle/BattleActor.cs` | Animations LPC joueur |

---

## Voir aussi

* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)
* [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md)
* [`GDD_donnees_reference.md`](GDD_donnees_reference.md)
* [`docs/COMBAT_REGRESSION.md`](../docs/COMBAT_REGRESSION.md)
