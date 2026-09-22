# Reprise — CopilotBuddy MoP

Colle ce fichier au début d'un nouveau chat. Lis ensuite `.github/agents/agent.agent.md` sur le disque (git-ignoré, donc absent du dépôt, mais il est là).

---

## 1. Ce qu'est le projet

Port **1:1 strict** de Honorbuddy 5.4.8 (WoW MoP, build 18414, x86) vers **.NET 10 / WPF / x86**.
Racine : `C:\Users\Texy6\Desktop\newhcb\Copilotbuddy MoP`, branche `master`.

Je suis **superviseur**. Je ne porte pas moi-même les gros fichiers : je délègue, puis je **contrôle chaque résultat** avant de le garder. J'ai attrapé une douzaine d'erreurs ainsi — code inventé, offsets sautés, justifications fausses, une récursion infinie. Le contrôle n'est pas optionnel, c'est la valeur ajoutée.

---

## 2. Comment Texy veut qu'on travaille

- **Réponses en français.** Code, journaux et commits en anglais.
- **Économiser les tokens.** Pas de dispersion, pas de vérification redondante, pas de relecture d'un fichier déjà lu. Grouper les commandes shell.
- **Un seul sous-agent à la fois.** J'en ai lancé deux en parallèle une fois, il m'a repris. Modèle : Sonnet.
- **Déléguer une partie du travail à ChatGPT** (voir §6). Il a le projet et les arbres décompilés, mais **il ne compile pas** — c'est moi qui vérifie et compile.
- **Ne jamais inventer.** Ni code, ni chaîne de ressource, ni valeur d'offset. Un trou déclaré vaut mieux qu'un comblement inventé.
- **Ne jamais corriger les bugs de HB.** Les reproduire et les signaler.
- **Jamais de `dotnet clean`, ne jamais supprimer `bin`** — il contient la charge utile runtime de HB, git-ignorée.
- **Ne pas toucher à `offsets mop.txt`** (git-ignoré, choix du joueur).
- Rien ne sort du disque : pas d'artefact, pas de gist, pas de partage.

### Commits

Une seule ligne, en anglais, format `Composant: ce qui change, pourquoi`.
Auteur **obligatoire** :

```
git -c user.name=Likon69 -c user.email=50536335+Likon69@users.noreply.github.com commit -q -m "..."
```

Jamais Claude comme auteur, **aucun trailer**, pas de `Co-Authored-By`. Un commit par étape vérifiée. **Ne jamais pousser sans demander.**
Les branches sont pour le dépôt `Navigation C++`, **pas** pour le port MoP — ici tout va sur `master`.

---

## 3. État au 22/09/2026

**Build : 0 erreur.** 167 commits, 899 fichiers `.cs` portés.

### Terminé
- **`Styx` — le noyau entier.** Mémoire, objets WoW, Lua, offsets, profils, comportements, frames, inventaire, coroutines.
- **Navigation complète** : `MeshNavigator`, `WowNavigator`, `Flightor` + navigation aérienne, évitement, `FlightPaths`, `WoWTaxi`.
- **UI** : `MainWindow`, `SettingsWindow` (avec bouton Discord), `PluginsWindow`, `RoutineSettingsWindow` — style MahApps Dark.Amber repris de CopilotBuddy WotLK, **logique de HB 5.4.8**.
- **Overlay** `Buddy.Overlay` porté de HB 6.2.3 (absent de MoP, ajout voulu).
- **Chargeurs** : `BotManager`, `PluginManager`, `RoutineManager`, `AssemblyLoader` → `CodeCompiler` sur **Roslyn**.
- **`LevelBot`** (botbase grind), 1950 lignes, corps `async` récupérés.

### Reste — 289 fichiers hors `nsNN`
| Zone | Manquants |
|---|---|
| `Bots.Professionbuddy*` | 77 |
| `Bots.DungeonBuddy*` | 65 |
| `Bots.Quest*` | 25 |
| `Bots.BGBuddy*` | 13 |
| `Honorbuddy` racine (`DevTools`, `Error`, `Login`, `Update`, `WebProfile`) | 16 |
| `SevenZip*` | 17 |
| Gatherbuddy, ArchaeologyBuddy, divers, bibliothèques | ~76 |

Plus **408 fichiers dans les namespaces obfusqués `nsNN`** (52 namespaces). Beaucoup sont des classes de fermeture qui disparaissent à la déobfuscation ; `Buddy.Auth` et `Buddy.Store` en font partie et sont **hors périmètre**.

**Prochain jalon suggéré : `Bots.Quest`** — objectif déclaré de Texy, et il hérite de `LevelBot` déjà en place.

Texy **ne veut pas tester en jeu avant 100 %**. Ne pas proposer de test partiel.

---

## 4. LA chose à savoir — l'arbre décompilé

**Autorité pour la logique 5.4.8 : `hb decompile\.hb 5.4.8 fresh\`** (disposition **plate**, par namespace).

L'ancien arbre `hb decompile\.hb 5.4.8\Honorbuddy\` vient d'un ILSpy périmé qui **supprimait silencieusement les machines à états** : 198 types `ClassNNN`/`StructNNN` y étaient référencés sans jamais être déclarés, sur 62 fichiers. Tous les corps `async` et les itérateurs en sortaient vides. J'ai cru pendant des heures que la logique était irrécupérable.

Régénéré avec `ilspycmd` 11 (`scratchpad/tools/ilspycmd.exe`) :

```
ilspycmd.exe -p -o "<dossier>" "C:\Users\Texy6\Downloads\Honorbuddy-MoP-5.4.8-18414-main\Honorbuddy.exe"
ilspycmd.exe -t <Namespace.TypeName> "<...>\Honorbuddy.exe"   # un seul type
```

→ **0 type orphelin**, itérateurs en `yield return`, corps `async` complets, **`.resx` extraits** (plus besoin de lire les blobs compilés).

Deux fichiers échouent à la décompilation — utiliser l'**ancien** arbre pour eux seulement :
`Bots.Professionbuddy.BehaviorTree\Action.cs`, `CommonBehaviors.Actions\ActionRunCoroutine.cs`.

### Hiérarchie des références
| Arbre | Rôle |
|---|---|
| `.hb 5.4.8 fresh` | **autorité logique, membres, valeurs** — gagne toujours |
| `.hb 5.3` | **seul non obfusqué** — autorité noms, commentaires, style. Namespace `Zeta.*` renommé `Styx.*` en 5.4.8 |
| `.hb 6.2.3` (WoD), `.hb legion` | même obfuscateur, ne récupèrent **jamais** un nom privé. Consulter pour comprendre, jamais pour copier de la logique de jeu |
| `.hb 3.3.5a` | préserve les **vrais noms de paramètres** |

### Binaire original
`C:\Users\Texy6\Downloads\Honorbuddy-MoP-5.4.8-18414-main\`
- `Honorbuddy.exe` — l'IL réel
- `auth\Offsets\HB2_18414.dat` — blob d'offsets, 2756 uints
- charge utile runtime : `Bots/`, `Plugins/`, `Quest Behaviors/`, `Routines/`

**J'ai deux fois conclu à tort que ces fichiers étaient absents du disque**, sur des recherches filtrées (`-maxdepth 4`, exclusions `grep -v`). Ne jamais conclure à l'absence depuis une recherche restreinte.

---

## 5. Offsets — méthode vérifiée

Blob `HB2_18414.dat`, 2756 uints, XOR octet roulant `b = (i+1) & 0xFF` → `v ^ (b<<24|b<<16|b<<8|b)`. Relatif = `v - 0x400000`.
Blocs : **205 fonctions** (début index 0), 206 divers (2579), **207 statiques** (2633), 208 vtable (2742).

**Contrôles de méthode** (à refaire à chaque fois) : fonction idx 1165 → `0x5D508F`, statique idx 1 → `0xB568A8`.
Les indices **glissent entre versions** : la caméra est 916 en 5.3, 931 en 5.4.8. Ne jamais mapper un nom par l'index d'une autre version — vérifier la fonction dans **IDA** (`mcp__ida-pro-mcp__decompile`), puis prendre le nom dans la 5.3.

Insérer dans **`Rebase(IntPtr newBase)`** de `OffsetManager.cs`, **pas** dans `Clear()` — erreur déjà commise.

---

## 6. Déléguer à ChatGPT

Texy fait l'aller-retour. ChatGPT a le projet et les arbres, mais **ne compile pas**. Il travaille bien : il refuse plutôt que d'inventer (il a bloqué trois fois, sur des offsets non décodés et sur `XmlEngine` manquant) et il a trouvé une vraie incompatibilité .NET 10 (`BeginInvoke`).

**Lui donner** : des fichiers autonomes dont toutes les dépendances existent déjà. Le vérifier avant d'envoyer — sinon il perd son quota sur un mur.
**Garder pour moi** : ce qui demande IDA, l'IL, ou une déobfuscation à recouper dans le corpus.

Gabarit de mission (à adapter) :

> Porte ces N fichiers vers le projet CopilotBuddy MoP, mêmes chemins et namespaces.
> **Source : `hb decompile\.hb 5.4.8 fresh\`** (disposition plate par namespace). Noms et style depuis `hb decompile\.hb 5.3\`. La 5.4.8 fait foi sur la logique, la 5.3 sur les noms.
>
> | Source | Destination |
> |---|---|
> | … | … |
>
> Règles : ne change aucune logique ; n'invente rien, signale plutôt ; ne corrige aucun bug apparent ; conserve exactement littéraux, constantes et `{0}` ; aucun identifiant `method_0`/`smethod_0`/`object_0`/`Class123` ne doit subsister (vrai nom déduit de l'usage, les noms **publics** ne changent pas) ; une classe `nsNN` sort du namespace obfusqué vers son vrai foyer ; pour toute structure lue en mémoire, conserve l'ordre, les types et le `StructLayout`/`Pack` ; n'invente jamais une valeur ni un nom d'offset ; `ImplicitUsings` désactivé, tous les `using` explicites, `Nullable` activé ; ne modernise pas ; signale toute API .NET Framework absente de .NET 10 ; **ne compile pas**.
>
> Rends les fichiers complets, un par un, avec leur chemin.

Il committe lui-même. **Auditer systématiquement** sa livraison (§7) avant de passer à la suite.

---

## 7. Outil d'audit

`scratchpad/one.py <source> <porté>` — compare les **valeurs** des chaînes et les constantes numériques.

```bash
python one.py "<...>/.hb 5.4.8 fresh/Styx.Xxx/Yyy.cs" "<...>/Copilotbuddy MoP/Styx/Xxx/Yyy.cs"
```

Artefacts restants :
```bash
grep -ohP '\b(smethod_\d+|method_\d+|[a-z][A-Za-z0-9]*_\d+|Class\d+|Struct\d+)\b' <fichiers> | sort -u
```

**Faux positifs connus :**
- ilspycmd 11 replie `string.Format("…{0}", x)` en `$"…{x}"` — même valeur, littéral différent.
- Il **inline les constantes nommées** : `PaddingSqr` devient `0.25f`. Un port qui rétablit la constante nommée est *plus* fidèle, pas moins.
- Artefacts ILSpy normaux : `new string[0]` omis sur `params`, `0U`/`1L`/`4f`, `+= 1U` pour `++`, valeurs d'enum implicites explicitées, gros `switch` sur chaîne compilé en `Dictionary<string,int>`, `@"…"` contre `"\r\n…"`.
- **Comparer par namespace + nom de type, jamais par nom de fichier** : il existe deux `LfgDungeons.cs`, deux `BlackspotManager.cs`, deux `Helpers.cs`.

Piège de build : une erreur **CS0246** fait avorter la pré-passe WPF et masque **toutes** les autres (`CS0103`/`CS0117` non). Pour voir la vérité, déplacer temporairement le fichier fautif, compiler, puis **le remettre et vérifier qu'il est remis**.

---

## 8. Déviations assumées et documentées

1. **CodeDom → Roslyn** — le *moteur* CodeDom est mort en .NET 10, les *types* vivent. API publique de HB inchangée.
2. **`CryptoStream.Read`** — boucle de lecture partielle.
3. **`Tripper.RecastManaged.dll` abandonné** — assembly **C++/CLI en mode mixte** (`COR flags 0x18`, `ILONLY=false`), **inchargeable en .NET 10**. Remplacé par `Navigation.dll` (natif, Recast/Detour + gestionnaire de tuiles MaNGOS). La *forme* de l'API de HB est conservée (`NavMeshQuery`, `NavMesh`, `QueryFilter`, `Status`, `PolygonReference`), seuls les corps sont rebranchés — 489 lignes de déviation confinée.
4. **`PolygonReference.Id` en `ulong`** (HB : `uint`) — `DT_POLYREF64` est défini dans `Navigation.vcxproj`, les polyrefs sont réellement 64 bits.
5. **Conversion de coordonnées dans le shim** — HB parle Detour (`−Y, Z, −X`), `Navigation.dll` attend du **WoW brut** (elle convertit elle-même en `{Y, Z, X}`). `NavMeshQuery` convertit aux frontières ; **`WowNavigator.FindPath` ne convertit pas** (entrées et sorties déjà en WoW). Vérifié méthode par méthode.
6. **`SetFactionQueryFilter` par coût de zone** — l'extractor écrit `0x0001` sur tout polygone, aucun bit de faction. La donnée est dans l'`AreaType` (`NAV_HORDE=15`, `NAV_ALLIANCE=16`).
7. **`ThreadPool.QueueUserWorkItem`** remplace `delegate.BeginInvoke` (`PlatformNotSupportedException` en .NET 10).
8. **`Buddy.Overlay` porté de 6.2.3** — absent de MoP, ajout voulu par Texy.
9. **`ScriptManager.cs` non porté** — c'est du source **Demonbuddy** (Diablo 3) resté par accident : il importe `Zeta.CommonBot`, `Zeta.Internals.SNO`, et son script est `Me = ZetaDia.Me`. Zéro namespace `Zeta.*` dans HB, personne ne l'appelle, IronPython jamais livré.
10. **`GreyMagic` → `GreenMagic`** — convention du projet, 31 fichiers.
11. **`MeshProvider`/`IMeshProvider` non portés** — seule implémentation `ns7.Class711`, qui importe `Buddy.Auth`. Les tuiles sont chargées nativement.
12. **Une modification de la DLL**, branche `mop` du dépôt `Navigation C++`, compilée par Texy : `SetPathFindProgressCallback_C` ajouté pour restaurer l'annulation de recherche à 4 s (`OnPathFindProgress`), que HB avait gardée jusqu'en 6.2.3.

**Critère pour emprunter à WoD/Legion** (décidé avec Texy) : n'emprunter que ce qui **ne touche pas au jeu** — géométrie, mathématiques, plomberie. Jamais ce qui touche mémoire, frames ou offsets. Finir le 1:1 d'abord ; les améliorations viendront après, composant par composant, chacune documentée.

---

## 9. Bugs de HB conservés (ne pas « corriger »)

- `Unstick()` : le démontage peut être tenté **deux fois** (`else if (Mounted)` non protégé par le drapeau).
- `FindSafeLandingSpot` : `2π × (8/i)` au lieu de `i/8 × 2π` — les caps de balayage se chevauchent.
- `WoWTaxi.GetKnownNodeBitQword(index)` ignore son paramètre — seuls les 64 premiers nœuds de vol sont lus. La 5.3 a le code correct **en commentaire**.
- `SetFlightPathUsage` (5.4.8) a perdu la protection `null` de la 5.3 — peut lever une `NullReferenceException`.
- `Navigator.CanNavigateFully` appelé avec 2 arguments dans l'évitement (5.3 passait `1` et `2`) → `maxHops` par défaut à 8192.
- `OverlayManager` passe `WINDOWPOS.hwnd` là où `SetWindowPos` attend `hwndInsertAfter`.
- `QueryPolygons` du shim : la DLL rend `0` aussi bien pour « aucun résultat » que « échec » — traduit en `Failure`.

---

## 10. Reliquats connus

- `Infralution/Localization/Wpf/` : **174 identifiants obfusqués** laissés par ChatGPT avant que je lui donne la consigne de déobfuscation. À reprendre.
- `CoroutineCompositeExtensions` : emprunté à 6.2.3 quand la 5.4.8 était illisible. L'arbre frais la rend lisible (108 l.) — **à rebasculer sur la 5.4.8**.
- `{loc:Resx …}` ne marchait pas au moment du port de l'UI (pas de `ResxExtension`) : les libellés sont affectés depuis le code-behind. `Infralution` est porté depuis, donc **le balisage XAML est redevenu possible**.
- Fenêtres absentes : `DevToolsWindow` (le bouton journalise « not available »), `ErrorWindow` (remplacé par `MessageBox`), icône de barre système (pas de ressource), sélecteur de langue (dépendait d'`Infralution`, désormais porté).
- `WoWMissile` : la structure native 5.4.8 a **un champ de plus** que celle de la 5.3. Champs non identifiés laissés en `Unknown01..74`, ordre et types préservés. Une passe IDA les nommerait.
- Le redistribuable **VC++ x86** est requis par `Navigation.dll` (`MSVCP140`, `VCRUNTIME140`). Texy le sait et le dit déjà aux utilisateurs.
