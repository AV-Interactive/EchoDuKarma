# Combat — règles & scénarios de régression

Document de référence pour playtests manuels (P2.5 / P2.6).

---

## Règles de fin de combat

| Issue | XP | Butin | Retour map | État joueur |
|-------|----|-------|------------|-------------|
| **Victoire** | Oui (`GrantBattleExperience`) | Oui (`DistributeBattleLoot` → `InventoryManager.TryAddItem`) | `ReturnScenePath` | Snapshot appliqué au joueur |
| **Défaite** | Non | Non | `ReturnScenePath` | 1 PV, 0 MP sur snapshot |
| **Fuite réussie** | Non | Non | `ReturnScenePath` | Snapshot inchangé (PV/MP actuels) |

### Fuite — détails

- Probabilité actuelle : **50 %** (`GD.Randf() > 0.5f`).
- Échec de fuite : le tour se termine, l'ennemi peut attaquer au tour suivant.
- **Karma / quêtes** : les ennemis déjà tués avant la fuite ont déjà déclenché `NotifyKill` et `ApplyMonsterKillImpact` — ce n'est **pas** annulé.
- **MP dépensés** avant la fuite restent consommés.

---

## Scénarios de régression

### 1. Victoire basique (1× Rat)

1. Map Intro → dialogue combat → 1 Rat.
2. Attaque physique jusqu'à victoire.
3. **Attendu** : retour map, +2 XP, log « Butin : Peau de rat », objet en inventaire, pas de crash.

### 2. Victoire multi-ennemis (2× Rat)

1. Combat Marchand : 2 Rats.
2. Victoire.
3. **Attendu** : +4 XP, butin affiché (2× Peau de rat si inventaire le permet), ordre des tours cohérent.

### 3. Magie offensive

1. Lancer un sort offensif sur un ennemi.
2. **Attendu** : caméra `PlayerMagic`, animation spellcast, dégâts, retour plan neutre, pas de soft-lock si MP suffisants.

### 4. Magie / soin (Support)

1. Utiliser une compétence Support (ex. Soin).
2. **Attendu** : caméra `PlayerMagic` (pas Neutral), animation spellcast, soin appliqué (sauf karma neutralisant les soins).

### 5. MP insuffisants

1. Tenter un sort coûtant plus de MP que disponibles.
2. **Attendu** : message d'erreur, menu réaffiché, pas de soft-lock.

### 6. Fuite réussie

1. Combattre, puis Fuir jusqu'à réussite.
2. **Attendu** : retour map, **aucun** message XP, **aucun** butin, PV/MP = état au moment de la fuite.

### 7. Fuite après kills partiels

1. Tuer 1 Rat sur 2, puis fuir avec succès.
2. **Attendu** : pas de XP ni butin de victoire ; kill comptabilisé quête/karma pour le Rat mort.

### 8. Défaite

1. Laisser le joueur mourir.
3. **Attendu** : retour map, joueur à 1 PV / 0 MP, pas de XP ni butin.

### 9. Double fin de combat

1. Enchaîner deux combats depuis la map.
2. **Attendu** : un seul signal `BattleEnded` par combat, pas de double connexion signal.

### 10. IA ennemie

| Pattern | Comportement attendu |
|---------|---------------------|
| Aggressive | Attaque ; bonus Force si PV &lt; 30 % |
| Defensive | Attaque si PV &gt; 50 % ; sinon ~60 % posture défensive |
| Normal | Attaque physique chaque tour |

---

## Fichiers concernés

- `Scripts/Battle/BattleManager.cs` — boucle combat, loot, fuite
- `Global/GameManager.cs` — retour map, XP (`GrantBattleExperience`)
- `Global/Bestiary.cs` — `EnemyStats.ParseLoot`
- `Global/InventoryManager.cs` — `TryAddItem`
- `Scripts/Battle/CameraDirector.cs` — plans caméra
- `Scripts/Battle/BattleActor.cs` — animations joueur
