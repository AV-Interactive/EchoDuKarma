# ⚡ Initiative et tours de combat - Echo Du Karma

## 1. Principe

Chaque **round** se déroule en deux phases :

1. **Phase de choix** — Les ennemis ont une action et une initiative **figées** (planifiées par l’IA). Le joueur choisit son action ; l’initiative affichée à gauche se met à jour **en temps réel** au survol des boutons (Agi + vitesse sort, fuite +5, etc.). Le panneau trie toutes les unités par initiative décroissante (aperçu).
2. **Phase d’exécution** — Une fois l’action validée, la file complète (ennemis + joueur) est triée par initiative et chaque tour s’exécute dans cet ordre.
3. Retour à la phase 1 (nouveau round).

Ex. Rat planifie Stalagtite (**13** = 7 Agi + 6 Vitesse), Gobi planifie Soin (**18**) ou mêlée (**8**) ; le joueur avec Flammeche (**17** à Agi 7) peut passer entre les deux selon le tri final.

---

## 2. Formules d'initiative

**Agilité effective** : Agi du combattant ; pour le joueur, modificateur **Karma** (comme les autres stats de combat).

| Action | Initiative |
| :--- | :--- |
| **Sort** (attaque, soin, buff) | `AgiEffective + Vitesse` (colonne `Vitesse` de `skills.csv`) |
| **Attaque physique** (menu Attaque) | `AgiEffective` (bonus Agi d'arme déjà inclus dans `Dexterity`) |
| **Défense** | `AgiEffective` |
| **Fuite** | `AgiEffective + 5` |
| **Ennemi — sort** (planifié) | `Agi + Vitesse` du sort (`skills.csv`, clé dans `Skills` du bestiaire) |
| **Ennemi — mêlée** (secours) | `Agi` seule |
| **Ennemi — défense** | `Agi` |

### Égalité

Si deux initiatives sont égales : priorité au **joueur**, puis ordre alphabétique des noms d'ennemis.

---

## 3. Déroulement d'un round

1. Planification ennemis (sort / mêlée / défense — voir [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)).
2. Menu joueur + HUD trié en direct selon l’action survolée.
3. Validation → tri final → exécution séquentielle.
4. Fin du round → nouveau round ou victoire / défaite.

---

## 4. Sources CSV

| Fichier | Colonne | Usage |
| :--- | :--- | :--- |
| `Datas/Persos/skills.csv` | `Vitesse` | Bonus initiative des sorts |
| `Datas/Bestiary/bestiary.csv` | `Skills` | Liste de sorts (`Nom1\|Nom2`) — initiative et action via `skills.csv` |
| `Datas/Bestiary/{ennemi}.csv` | `Agi` | Agilité effective au niveau de l'instance |
| `Datas/Persos/equipments.csv` | `Agi` | Déjà fusionné dans l'Agi du joueur (pas de double comptage) |

---

## 5. Implémentation

| Fichier | Rôle |
| :--- | :--- |
| `Scripts/Data/CombatInitiative.cs` | Formules |
| `Scripts/Battle/WaveActionEntry.cs` | Type d'action + initiative |
| `Scripts/Battle/BattleManager.cs` | `BeginRound`, `CommitAndStartRoundExecution`, `AdvanceRoundExecution` |

---

## 6. Interface combat

Panneau **Initiative** à gauche (`BattleInitiativeTrack`) :

* **Choix** : portraits + initiative ; tri temps réel (ennemis figés, joueur selon survol).
* **Exécution** : même ordre que la file triée ; **▶** sur le tour en cours ; tours passés atténués.

---

## 7. Voir aussi

* [`GDD_INDEX.md`](GDD_INDEX.md)
* [`GDD_combat.md`](GDD_combat.md)
* [`GDD_systeme_karma.md`](GDD_systeme_karma.md)
* [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md)
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)
