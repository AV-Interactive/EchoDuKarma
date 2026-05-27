# 📜 Synopsis, Protagonistes et Progression - Echo Du Karma

## 1. Synopsis et Contexte Narratif
[cite_start]L'aventure débute par une tradition ancestrale : le **Pèlerinage des Étoiles**[cite: 41, 86]. [cite_start]Pour valider son diplôme et devenir officiellement un Mage, le protagoniste doit traverser le continent pour faire bénir son bâton dans le temple de la Capitale[cite: 42, 86]. 

[cite_start]Ce qui ne devait être qu'une formalité administrative tourne au désastre lorsque le Karma mondial bascule brutalement, plongeant les régions traversées dans des états extrêmes[cite: 43, 87].

### Les Seuils Idéologiques du Monde
* [cite_start]**L'Utopie étouffante (+70 à +100)** : Un monde "parfait" où l'ordre est si absolu que l'ambition et l'action disparaissent[cite: 38].
* [cite_start]**Le Chaos total (-70 à -100)** : Une zone de survie pure où la violence domine et où les structures sociales s'effondrent[cite: 39].

---

## 2. Les Protagonistes 🛡️
* [cite_start]**Le Mage (Magus)** : Un érudit fragile dont la quête de diplôme devient une mission de sauvetage du monde[cite: 56]. [cite_start]Il commence le voyage avec des statistiques défensives faibles (4 de Défense)[cite: 89]. Affinité élémentaire de départ : **Feu** (`heroes.csv`) — privilégie **Flammeche** contre les **Rats** (Terre), moins efficace contre les **Gobis** (Eau) ; voir `GDD_systeme_elementaire.md`.
* [cite_start]**Le Tank (Paladin)** : Le protecteur indispensable, garant de la survie du Mage face à un bestiaire de plus en plus agressif[cite: 57].

---

## 3. Découpage des Zones et Arcs Initiaux 🗺️

### Zone 1 : Le Village du Silence Éternel (L'Utopie)
* [cite_start]**Contexte** : Une cité au Karma extrêmement haut (+90)[cite: 46, 80]. [cite_start]Pensant que le monde est déjà "parfait", les habitants ont cessé de parler et d'agir[cite: 47, 80].
* [cite_start]**Gameplay / Objectif** : Le Tank et le Mage doivent "choquer" ce système pour ramener l'équilibre[cite: 48, 81]. [cite_start]Cette action provoque involontairement l'apparition des premiers monstres de la zone : les **Rats**[cite: 48, 81].

### Zone 2 : La Route de la Capitale (L'Éveil)
* [cite_start]**Contexte** : Le tout premier segment en "monde ouvert"[cite: 49, 88]. 
* [cite_start]**Gameplay / Objectif** : C'est ici que les héros testent leur survie et font face à leur premier combat contre un **Rat** (test de survie pour les 4 de Défense du Mage)[cite: 50, 88, 89].
* [cite_start]**Quête Annexes (SOS PNJ)** : Un marchand est harcelé par deux Rats sur le bord de la route[cite: 50, 90]. [cite_start]Le joueur peut choisir d'intervenir : aider ce PNJ permet d'augmenter directement le Karma positif de la région[cite: 51, 91].

### Zone 3 : La Mine de l'Instabilité (Le Chaos)
* [cite_start]**Contexte** : Un village minier autrefois prospère grâce à l'extraction de cristaux de Karma[cite: 52, 82]. [cite_start]Une surexploitation et une extraction excessive ont fait basculer la zone dans l'Instabilité, avec un score de **-40**[cite: 53, 83].
* [cite_start]**Gameplay / Objectif** : Les galeries de la mine sont désormais envahies par des **Gobis**[cite: 54, 84]. [cite_start]Les héros y interviennent en tant que mercenaires pour éliminer la menace et stabiliser la jauge de Karma de la région[cite: 54, 84]. En combat : affinité **Eau** des Gobis — contre-nature pour un Magus **Feu** (double cycle défavorable si le joueur spamme Flammeche).

---

## 4. Zone Introduction — implémentée aujourd’hui

| Élément | Détail gameplay actuel |
| :--- | :--- |
| Map | `Maps/Intro/Map.tscn`, zone `Introduction` |
| Karma départ | +15 |
| PNJ | Marchand (SOS rats), Sage Karma, Livre, testeurs |
| Combat intro | `BATTLE:Rat:2` via quête marchand ; test `Rat\|Gobi` |
| Boutique | Débloquée après quête — `MARCHAND_INTRO` |
| Tutoriels | Dialogues karma + éléments (`PNJ_KARMA_01`–`11`) |

Les zones 1–3 ci-dessus restent la **vision narrative** ; seul le segment « Route / Éveil » est partiellement couvert par la map Intro.

---

## 5. Documentation systèmes

Index complet : [`GDD_INDEX.md`](GDD_INDEX.md)

* [`GDD_donnees_reference.md`](GDD_donnees_reference.md) — CSV
* [`GDD_combat.md`](GDD_combat.md) — combat
* [`GDD_dialogues_quetes.md`](GDD_dialogues_quetes.md) — Intro
* [`GDD_economie_progression.md`](GDD_economie_progression.md) — boutique & XP
* [`GDD_systeme_karma.md`](GDD_systeme_karma.md)
* [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md)
* [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md)
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)