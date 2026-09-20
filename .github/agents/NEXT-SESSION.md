# Prompt de reprise — CopilotBuddy MoP

Colle ce message tel quel au début du nouveau chat.

---

Tu reprends le portage de **CopilotBuddy MoP** : portage 1:1 de Honorbuddy 5.4.8 (WoW MoP, build 18414, x86) vers .NET 10.

**Projet** : `C:\Users\Texy6\Desktop\newhcb\Copilotbuddy MoP`
**Règles du projet** : lis `.github\agents\agent.agent.md` en premier (il est git-ignoré mais présent sur le disque). Tout y est : architecture, style ApocDev, règle 18 (RemoteASM différé), règle 19 (offsets), règle 20 (ne jamais nettoyer `bin`).

## Références (lecture seule, ne jamais modifier)

| Quoi | Chemin |
|---|---|
| Décompilé 5.4.8 — **autorité pour la logique et la structure** (obfusqué : `nsNN`, `smethod_N`, `structNNN_0.intptr_N`) | `..\hb decompile\.hb 5.4.8\Honorbuddy\` |
| Source réelle 5.3 — **autorité pour les vrais noms** | `..\hb decompile\.hb 5.3\` |
| Décompilé 6.2.3 — indices secondaires | `..\hb decompile\.hb 6.2.3\` |
| Release HB MoP (binaire + table d'offsets) | `C:\Users\Texy6\Downloads\Honorbuddy-MoP-5.4.8-18414-main\` |
| Table d'offsets chiffrée | `...\auth\Offsets\HB2_18414.dat` |
| IDA (client WoW 5.4.8) | serveur MCP `ida-pro-mcp` déjà connecté |

## Règles absolues

1. **Ne jamais rien inventer.** Chaque ligne doit être traçable : logique = décompilé 5.4.8, noms = source réelle 5.3 (ou 6.2.3 si 5.3 n'a pas l'équivalent). Si ce n'est pas résolvable → on s'arrête et on signale, on ne devine pas.
2. **Fichiers complets.** Pas de stub, pas de TODO, pas de version « simplifiée ». Un fichier porté l'est en entier.
3. **Ne jamais « corriger » les bugs de HB.** Plusieurs bizarreries sont conservées volontairement (division entière dans `WoWItem.DurabilityPercent` et `LocalPlayer.DurabilityPercent`, `WoWPlayer.LevelFraction`, boucle vide dans `WoWPlayer.ArenaTeams`, `GetPhysicalDamageIndex` qui renvoie 0). Les signaler, jamais les réparer.
4. **Ne jamais recopier un artefact du décompilateur qui ne compile pas** (`goto IL_xxxx` sans étiquette, `(bool) ?? x`). Restaurer la forme source réelle depuis 5.3, sinon signaler.
5. Code et commentaires **en anglais**, conversation **en français**.
6. **Économiser les tokens.** Pas de dispersion, une étape à la fois, pas d'initiative non demandée, pas de build non demandé.

## Méthode de travail (imposée par l'utilisateur)

- Tu **supervises**. Les workers sont des sous-agents **Sonnet** (`Agent` avec `model: "sonnet"`), **un seul à la fois**.
- Tu **relis systématiquement** chaque retour de worker avant de le garder. Les workers se trompent : plusieurs erreurs réelles ont été interceptées de cette façon.
- Dans chaque briefing de worker, mettre les **conditions d'arrêt dur** : offset inconnu → laisser l'accesseur brut `StyxWoW.Offsets.structNNN_0.intptr_N` et signaler le bloc + l'index ; corps d'itérateur absent du décompilé → signaler `UNRECOVERABLE`, **ne pas** le reconstruire depuis une autre version de HB.

### Vérifications qui marchent (à refaire, elles sont rapides et décisives)

- **Diff de membres et de littéraux** entre le décompilé et le fichier porté (petit script Python : extraire les déclarations `public/internal/private … Nom(` et compter tous les littéraux numériques). Un écart de littéral non expliqué = un bug.
- **Diff ordonné des accès aux descripteurs** : comparer la suite des `method_1<T>(N)` du décompilé aux `GetStorageField<T>(WoWXxxFields.Y)` du portage, valeurs résolues via l'enum. Doit correspondre 1:1 dans le même ordre.
- **Offsets** : décoder depuis `HB2_18414.dat` (XOR par octet roulant : `valeur ^ (b<<24|b<<16|b<<8|b)` avec `b = (index+1) & 0xFF`), blocs `205` = fonctions (0…2578), `206` = misc (2579+), `207` = statics (2633+), `208` = vtable (2742+). Relatif = `valeur - 0x400000`. Puis **vérifier dans IDA** (segment `.data` pour un statique, vrai début de fonction en `.text` pour une fonction, et décompiler pour confirmer la sémantique). Nom pris dans les enums 5.3 au même site d'appel.
- **Corps manquant dans le décompilé** (ILSpy laisse tomber certaines machines à états d'itérateur) : lire l'**IL du vrai binaire** `Honorbuddy.exe` avec un petit outil .NET utilisant `System.Reflection.Metadata` (`PEReader` + `GetMethodBody` + décodage des opcodes). Déjà fait deux fois avec succès (`LocalPlayer.BankItemGuids`, `QuestLog.GetQuestsCompleted`).

## Ce qui est déjà fait

- **Jalon 1 validé en jeu** : attache au processus, rebasage ASLR, hook EndScene/Present (D3D9 + D3D11), injection FASM, gestionnaire d'objets, appel Lua. Testé sur le client TwinStar du joueur.
- **GreenMagic** complet (mémoire, exécuteur, FASM, DirectX).
- **Modèle objet complet** : `WoWObject`, `WoWUnit`, `WoWPlayer`, `LocalPlayer`, `WoWItem`, `WoWContainer`, `WoWBag`, `WoWGameObject` (+ sous-objets), `WoWCorpse`, `WoWDynamicObject`, `WoWAreaTrigger`, `WoWPartyMember`, `ObjectManager` complet.
- **Couche sorts** : `WoWSpell`, `SpellManager` + `SpellHelpers`, `WoWAura`, `WoWAuraCollection`, `WoWSkill`.
- **Caches et données** : `WoWCache` (17 types imbriqués), `ClientDb` (357 tables), 20 lecteurs DBC, `Quest`, `QuestLog`, `PlayerQuest`, `WoWGroupInfo`, `WoWCurrency`, `WoWTotemInfo`, `ItemInfo`, `ItemStats`.
- **Divers** : `WoWMovement`, `WoWMovementInfo`, `Blacklist`, `Battlegrounds`, `Landmarks` + les 9 sous-classes, `LuaEvents`, `FileCache`, SDK **LZMA** (copie de la source réelle 5.3), `Styx.Helpers.Extensions`, `WoWMathHelper`, types math Tripper.
- **181 offsets** nommés et vérifiés dans IDA (`Styx\Offsets\`). **Plus aucun identifiant obfusqué dans tout le projet** (`smethod_`, `method_N`, `ClassN`, `StructN`, `structNNN_0.intptr_N` : zéro occurrence).
- ~320 fichiers, ~41 000 lignes. 37 commits sur `master`, arbre propre.

## Ce qui reste

**Point de blocage du build** : `AreaPoiLandmark.Location` appelle `Styx.Pathing.Navigator.FindHeight`. `Navigator` est une **façade** (413 lignes déléguant à des interfaces), mais sa propriété `FlightorLandSystem` tire `Flightor` (1293 lignes) → toute la pile de navigation.

**Attention** : le build affiche **1 seule erreur**, mais c'est trompeur — la passe WPF (`MarkupCompilePass1`) compile un sous-ensemble et s'arrête avant le compilateur principal (26 avertissements contre 596 au build complet). `BotEvents` et `TreeRoot` n'existent toujours pas et sont référencés par `SpellManager`, `LuaEvents` et `Battlegrounds` : ces erreurs sont masquées, pas résolues.

**Ordre proposé pour la suite** (à valider avec l'utilisateur avant de lancer quoi que ce soit) :

1. **Ouverture du jalon 3 — cœur du bot** : `Styx\TreeSharp\*` (RunStatus, Composite, PrioritySelector…), `Styx\CommonBot\TreeRoot.cs`, `BotBase.cs`, `BotEvents.cs` (1314 lignes). C'est ce qui débloque `SpellManager`, `LuaEvents`, `Battlegrounds`.
2. **Façade de navigation** : `Styx\Pathing\Navigator.cs` + les contrats (`INavigationProvider`, `ITerrainHeightProvider`, `IPlayerMover`, `IStuckHandler`, `MoveResult`, `NavigationProvider`, `NavigationProviderChangedEventArgs`) et ce que `FlightorLandSystem`/`Flightor` exigent.
3. **Premier build vert**, puis test en jeu (le joueur lance WoW et teste à la demande).
4. Ensuite : moteur de profils XML, compilation runtime (Roslyn), bots, Singular.
5. **La navigation réelle (meshes, `Navigation.dll` de CopilotBuddy, tuiles) reste la toute dernière étape.**
6. Restaurer RemoteASM (règle 18) : trois endroits l'attendent — `WoWObject.Location`, `WoWUnit.GetTraceLinePos`, `SpellManager.GetKnownSpellIndex`/`GetSpellOverrideId`.

## Commits

Une seule ligne, format `Composant: ce qui change, pourquoi`, en anglais, sur `master`, auteur `Likon69 <50536335+Likon69@users.noreply.github.com>`, **jamais Claude**, pas de trailer, **jamais de push sans demander**. Commiter à chaque étape relue et validée (ne pas attendre le build vert : on a failli perdre 16 000 lignes une fois).

## À ne pas oublier

- Ne **jamais** exécuter `dotnet clean` ni supprimer `bin` : la charge utile de HB (`Spells.bin`, `Data.bin`, profils, Quest Behaviors…) y est copiée à côté de l'exe.
- Ne pas toucher à `offsets mop.txt` (git-ignoré, choix du joueur).
- Ne jamais partager la conversation ni le code à l'extérieur (pas d'artefact, pas de gist) : tout reste sur le disque local.
- `System.Data.SQLite.dll` de HB est inutilisable en .NET 10 (mixed-mode) : quand `Styx\CommonBot\Database` sera porté, prendre le paquet NuGet `System.Data.SQLite.Core`.
