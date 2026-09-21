# Prompt de reprise — CopilotBuddy MoP

Colle ce message tel quel au début du nouveau chat.

---

Tu reprends le portage de **CopilotBuddy MoP** : portage 1:1 de Honorbuddy 5.4.8 (WoW MoP, build 18414, x86) vers .NET 10.

**Projet** : `C:\Users\Texy6\Desktop\newhcb\Copilotbuddy MoP`
**Règles du projet** : lis `.github\agents\agent.agent.md` en premier (git-ignoré mais présent sur le disque). Tout y est : architecture, style ApocDev, règle 18 (RemoteASM différé), règle 19 (offsets), règle 20 (ne jamais nettoyer `bin`).

Ces deux fichiers suffisent. Tout le reste est sur le disque.

## Références (lecture seule, ne jamais modifier)

`..\hb decompile\` contient **six** versions de HB. Leur autorité respective a été mesurée, ne la redécouvre pas :

| Version | Chemin | Rôle |
|---|---|---|
| 5.3 | `.hb 5.3\` | **Seule source non obfusquée.** Autorité pour les noms, commentaires XML, `#region`, style. |
| 5.4.8 | `.hb 5.4.8\Honorbuddy\`, `.hb 5.4.8\GreyMagic\` | **Cible.** Autorité pour la logique, les membres, l'ordre, les valeurs. **Gagne toujours** en cas d'écart. |
| 6.2.3 (WoD) | `.hb 6.2.3\Honorbuddy\` | Obfusqué. Seulement si un membre est dans 5.4.8 mais pas en 5.3. **Ne jamais reprendre une forme 6.2.3 que la 5.4.8 n'a pas** (pièges rencontrés : `Else`/`IXmlObject`, `DelayCompiledExpression<T>` générique, `HealTargeting` par HashSet de GUIDs). |
| 4.3.4, 3.3.5a, Legion 3.0.904 | `.hb 4.3.4\`, `.hb 3.3.5a\`, `.hb legion 3.0.904 for wow 7.3.2\` | Même obfuscateur que la 5.4.8 : **ne récupèrent jamais un nom privé**. Seule exception utile : **`.hb 3.3.5a` conserve les vrais noms de paramètres**. |

Autres : release HB MoP `C:\Users\Texy6\Downloads\Honorbuddy-MoP-5.4.8-18414-main\` (binaire + `auth\Offsets\HB2_18414.dat`), IDA via le serveur MCP `ida-pro-mcp` (base `Wow mop.exe`), navigation C++ `..\Navigation C++\`, extracteur de tuiles `..\Navigation View3D\Extractor_projects-master\extractor-csharp\`.

## Règles absolues

1. **Ne jamais rien inventer.** Logique = décompilé 5.4.8, noms = source réelle 5.3. Non résolvable → on s'arrête et on signale.
2. **Fichiers complets.** Pas de stub, pas de TODO, pas de version simplifiée.
3. **Ne jamais « corriger » un bug de HB.** Les signaler, jamais les réparer.
4. **Ne jamais recopier un artefact du décompilateur.**
5. Code et commentaires **en anglais**, conversation **en français**.
6. **Économiser les tokens.** Une étape à la fois, pas de build non demandé.

## Méthode de travail (imposée par l'utilisateur)

- Tu **supervises**. Les workers sont des sous-agents **Sonnet**, **un seul à la fois**.
- Tu **relis systématiquement** chaque retour. **Plusieurs erreurs réelles ont été interceptées ainsi** — voir « Erreurs déjà commises par des workers » plus bas.
- **Commiter chaque étape relue**, sans attendre le build vert (on a failli perdre 16 000 lignes une fois).

### Le patron de briefing qui marche (réutilise-le)

Donner au worker : le **mapping A→B déjà résolu** fichier par fichier ; les **pièges d'homonymes** nommés ; la liste de ce qui est **déjà porté** ; la liste des types **pas encore portés** présentée comme « normal et attendu, ne crée aucun stub, le build reste rouge, c'est assumé par l'utilisateur » ; et l'exigence d'un rapport listant **chaque écart 5.4.8 vs 5.3**.

Arrêts durs à répéter dans chaque brief :
- `UNRECOVERABLE: <fichier> <méthode>` — **jamais** de `throw new NotImplementedException()`.
- `CODEDOM:` / `API-MORTE:` pour une API .NET Framework absente de .NET 10 — porter fidèlement, ne jamais adapter de sa propre initiative.
- **Branche Buddy Store / `nsNN`** : la retirer, porter le reste, la signaler ligne par ligne.
- **Helper obfusqué** (`ClassNNN.smethod_N()`) : **jamais deviner**, lire le corps réel dans `..\hb decompile\.hb 5.4.8\Honorbuddy\nsNN\ClassNNN.cs`.
- **Offset brut** : ne pas l'inventer, signaler bloc + index + nom réel 5.3, ne pas toucher `Styx\Offsets\*`.
- Demander **la liste des balises/attributs XML** ou **les valeurs d'enum** quand le fichier en contient — c'est le contrôle le plus rentable.

## Les trois outils construits (à reconstruire dans le scratchpad, ils valent leur poids)

1. **Décodeur d'offsets** — `HB2_18414.dat`, 2756 `uint`, XOR par octet roulant `b=(i+1)&0xFF` → `v ^ (b<<24|b<<16|b<<8|b)`. Blocs : 205 fonctions (0), 206 misc (2579), **207 statiques (2633)**, 208 vtable (2742). Relatif = `v - 0x400000`. **Puis vérifier dans IDA** : `xrefs_to` prouve que c'est une statique référencée ; des tableaux frères référencés par la **même fonction** corroborent la sémantique. Ajoutés ainsi cette session : `GossipActiveQuestsArray` (79, 0xDD9B28), `GossipAvaibleQuestsArray` (80, 0xDDEEE8), `GossipQuestsForObjectsArray` (82, 0xDE9F48), `CurrentShownQuest` (81, 0xDE7B9C), `LootFrame` (68, 0xDD3280), `C_PetBattles__IsInBattle` (3, 0xB61B20).
2. **Extracteur de ressources** — `System.Resources.ResourceReader` sur `.hb 5.4.8\Honorbuddy\Styx\Resources\StyxResources.resources` → **232 vraies chaînes de HB**. Un script compare les `StyxResources.X` utilisés dans le code porté au `.resx` du projet et ajoute les `<data>` manquants **plus** les propriétés typées dans `StyxResources.cs`. **~50 chaînes restaurées ainsi — n'invente jamais une chaîne de ressource, elle est dans ce blob.**
3. **Lecteur d'IL** — `PEReader` + `GetMethodBody` + décodage d'opcodes sur `Honorbuddy.exe`, en résolvant les tokens. **C'est ainsi qu'on récupère les corps d'itérateurs qu'ILSpy abandonne.** Utilisé avec succès sur `BlackspotManager.GetBlackspotsForFlags` et `IfNode.GetNodes` (les classes d'état `Class698`/`Class788` sont absentes du décompilé mais présentes dans le binaire).

Piège d'environnement : `dotnet run -- "$arg"` mange les arguments sous Git-Bash. Compiler avec `-o out` et lancer l'exe directement.

## Les vérifications qui tranchent

- **Diff des littéraux de chaînes, fichier par fichier**, entre décompilé et portage. C'est le contrôle le plus décisif : sur un parseur XML, des ensembles identiques prouvent que **chaque nom de balise et d'attribut est exact**. Résultats obtenus : `Profile` 119 chaînes identiques, les 8 nœuds d'action 102, `Vendor` 39, `Chat` toutes.
- **Diff des littéraux numériques.** Attrape les IDs de sorts/mobs/quêtes faux. Écarts normaux et explicables : `new string[0]` omis sur un `params`, `0U`/`1L`/`4f` contre `0`/`1`/`4.0`, `+= 1U` contre `++`, valeurs d'enum implicites rendues explicites, `new Composite[3]` de ILSpy devant un appel `params`, et **un grand `switch` de chaînes compilé en `Dictionary<string,int>` + `case N:`** (d'où des entiers 1..49 en double).
- **Diff ordonné des `RunStatus`** par fichier pour l'arbre de comportement.
- **Comptage des champs de struct** quand c'est une disposition mémoire lue par `Memory.Read<T>` — un champ de travers décale tout.
- **Compilation isolée** dans le scratchpad (csproj `EnableDefaultCompileItems=false` + les fichiers voulus + des bouchons) : valide la syntaxe sans toucher au projet ni à `bin`.

## Erreurs déjà commises par des workers (à intercepter)

- Un worker a remplacé un appel obfusqué `state.smethod_0()` par un `Enum.IsDefined` **supposé** équivalent ; le corps réel était un contrôle d'intervalle.
- Un worker a supposé que ILSpy répartit `Foo`/`Foo<T>` de la même façon dans `X.cs` et `X.2.cs` — **ce n'est pas symétrique d'une exception à l'autre**, il faut vérifier laquelle est où.
- Un worker a laissé `throw new NotImplementedException()` sur un itérateur. Récupéré par lecture de l'IL.
- Un worker a justifié une omission par un raisonnement **faux** (« la propriété existe en 5.3 ») alors qu'elle n'existait nulle part. L'action était bonne, la raison inventée : **vérifier l'affirmation, pas seulement le code**.
- Deux fois, **mes propres listes « déjà porté » étaient fausses** ; les workers l'ont signalé au lieu de contourner. Bon réflexe à encourager.

## Ce qui est fait

**Jalon 1 validé en jeu** (attache, ASLR, hook EndScene/Present D3D9+D3D11, injection FASM, gestionnaire d'objets, appel Lua). **GreenMagic** complet. **Modèle objet complet**, **couche sorts**, **caches et données** (`WoWCache`, `ClientDb` 357 tables, 20 lecteurs DBC), `WoWMovement`, `Blacklist`, `Battlegrounds`, `Landmarks`, `LuaEvents`, SDK LZMA. **181 offsets** nommés et vérifiés dans IDA, **plus aucun identifiant obfusqué** dans le projet.

**Cette session : 34 commits**, de `e8d17aa` à `fd7ebd4`, ~14 000 lignes.

- **Cœur du bot** : tout `Styx\TreeSharp` (24 fichiers), système de hooks, `BotEvents`, `TreeRoot`, `BotBase`, `BotManager`, `BotPoi`, `Pulsator`, façade `Navigator`.
- **Routines** : `CombatRoutine`, `RoutineManager`, `ICombatRoutine`, `CapabilityFlags/State`, `AssemblyLoader`, `TypeOnlyLoader`, `CodeCompiler`, `Arguments`, `CommandLine`.
- **Settings** : `Settings` + ses 3 attributs, `GlobalSettings`, `CharacterSettings`.
- **Ciblage** : `Targeting`, `HealTargeting`, `LootTargeting`, `LootPredictor`, les 4 delegates.
- **Divers CommonBot** : `Chat` (30 événements Lua), `Mount`, `GameStats`, `PluginManager`, `CharacterManager`, `GrindArea`, `Area`, `Hotspot`, `AreaManager`, `BlackspotManager`, `Blackspot`, `GossipFrame`, `QuestFrame`, `WoWPetBattle`.
- **Système de profils** : `Profile` (2242 L de décompilé), `ProfileManager`, les 8 exceptions, `ProfileHelper`, `ProfileExceptionHelper`, `HotspotCollection`, `Vendor`/`VendorManager`, `Mailbox`/`MailboxManager`, `XmlUtils`, tout `Quest\*`.
- **`Styx\CommonBot\Profiles\Quest\Order` : 41 fichiers sur 41**, le moteur d'interprétation des profils de quêtes.

### Deux déviations de plateforme assumées (validées par l'utilisateur, prouvées par test)

1. **CodeDom → Roslyn.** `CodeCompiler` (commit `6519861`) et `ExpressionSet` (commit `45f18f6`). Fait crucial : **seul le moteur est mort, les types CodeDom sont vivants** en .NET 10 — `CompilerParameters`, `CompilerResults`, `CompilerError` se construisent normalement et `CompilerResults.CompiledAssembly` a un setter. L'API publique de HB est donc **inchangée** et aucun appelant ne bouge. Paquets ajoutés : `Microsoft.CodeAnalysis.CSharp` 4.14.0, `System.CodeDom` 9.0.0. Correspondance : `/d:A;B` → `preprocessorSymbols`, `/unsafe` → `WithAllowUnsafe`, `ReferencedAssemblies` → `MetadataReference.CreateFromFile`, diagnostics → `CompilerError` (lignes 1-basées des deux côtés). **Piège trouvé par le test : `CSharpSyntaxTree.ParseText(string)` crée un texte sans encodage et l'émission du pdb échoue en CS8055 — passer par `SourceText.From(text, Encoding.UTF8)`.**
2. **`EncryptedAttribute`** (commit `e332da9`) : `CryptoStream.Read` rend la main dès qu'il a des données sur .NET, donc la lecture unique de HB **tronquait** au-delà d'un bloc, silencieusement (son `catch` renvoie du base64). Corrigé par une boucle de lecture, commentaire laissé dans le fichier pour que personne ne « restaure » le code d'origine.

Règle dégagée : on ne corrige jamais un bug de HB, mais **quand .NET a changé de comportement sous le code, on restaure la sémantique d'origine et on le signale**.

### Quatre branches Buddy Store retirées (règle 13)

`BotManager.LoadBots`, `RoutineManager`, `PluginManager`, `ProfileManager.LoadNew` (protocole `store://`). Chaque fois vérifié par diff de chaînes que **seule** la branche Store disparaissait.

## Ce qui reste

**Le build est rouge par choix** — l'utilisateur a explicitement choisi « le cycle d'abord, build rouge assumé ». Ne cherche pas à le verdir en inventant des stubs.

Dans l'ordre suggéré :

1. **Managers de profil restants** : `HotspotManager` (315 L), `ProtectedItemsManager` (204 L), `ForceMailManager` (184 L). Chemins identiques en 5.3.
2. **Sous-système base de données d'objets**, révélé par `VendorManager` : `Styx.CommonBot.Vendors`, `Database.NpcCacher`, `Database.NpcResult`, `ObjectDatabase.Query`, `VendorTypeExtensions`. Dossiers `Database\` et `ObjectDatabase\` vides dans le projet. **Rappel `agent.agent.md` : `System.Data.SQLite.dll` de HB est inutilisable en .NET 10, prendre le paquet NuGet `System.Data.SQLite.Core`.**
3. **`CustomForcedBehavior`** (1133 L) + `CustomBehaviorFileNameAttribute`.
4. **Plugins** : `PluginContainer`, `PluginWrapper`, `HBPlugin`.
5. **Gestion de personnage** : `ClassProfile`, `ClassProfileLoadException`, `ClassProfileLocalization`, `AutoEquipper`, `TalentSelector`. Attention : quatre méthodes d'`AutoEquipper`/`TalentSelector` ont été nommées `Pulse`/`Initialize`/`Shutdown` **par déduction** depuis `CharacterManager` — **à revalider** quand ces fichiers seront portés.
6. **Terminer les portages partiels de jalon 1** : `StyxWoW` (`AreaManager`, `Sleep`, `GetLog`, `LastRedErrorMessage`, `SleepForLagDuration`), `Utilities.GetObjectString`, `Styx.Localization.Globalization` (clés `NoClassProfileLoaded`, `WrongClassProfileLoaded`), `RoutineSelectionForm` (WinForms — le csproj a bien `UseWindowsForms=true`).
7. **Premier build vert**, puis test en jeu (le joueur lance WoW et teste à la demande).
8. **Pile de navigation, en dernier** : `Navigator` est déjà porté en façade mais `Flightor`, `MeshNavigator`, `FlightorLandSystem`, `ITerrainHeightProvider`, `IPlayerMover`, `ClickToMoveMover`, `DefaultHeightProvider`, `NavigationProviderChangedEventArgs` manquent, ainsi que tout `Tripper.*`. **La navigation réelle (meshes, `Navigation.dll`, tuiles) reste la toute dernière étape.**
9. **Restaurer RemoteASM** (règle 18) : `WoWObject.Location`, `WoWUnit.GetTraceLinePos`, `SpellManager.GetKnownSpellIndex`/`GetSpellOverrideId`.

## Décisions déjà prises — ne pas les reposer

- **Il n'y aura jamais d'authentification.** `Buddy.Auth` et `Buddy.Store` ne seront jamais portés.
- Les **noms de types gardent leur orthographe d'origine** : seul le *namespace* racine `Honorbuddy` devient `CopilotBuddy`. `HonorbuddyUnableToStartException` reste tel quel.
- **Découpage des fichiers = celui de la 5.4.8**, même quand la 5.3 regroupe plusieurs types dans un fichier.
- Le portage est **complet par fichier**, dans l'ordre des dépendances.

## Commits

Une seule ligne, format `Composant: ce qui change, pourquoi`, en anglais, sur `master`, auteur `Likon69 <50536335+Likon69@users.noreply.github.com>`, **jamais Claude**, pas de trailer, **jamais de push sans demander**.

## À ne pas oublier

- Ne **jamais** exécuter `dotnet clean` ni supprimer `bin` : la charge utile de HB y est copiée à côté de l'exe.
- Ne pas toucher à `offsets mop.txt` (git-ignoré, choix du joueur).
- Ne jamais partager la conversation ni le code à l'extérieur : tout reste sur le disque local.
