# Changelog

## [1.3.5] - 2026-02-09

### Waiter! Waiter! More compatibility, please

#### Fixes

- Fixed compatibility (again) with **EVIL** [WeatherRegistry](https://thunderstore.io/c/lethal-company/p/mrov/EXPERIMENTALWeatherRegistry) and [WeatherTweaks](https://thunderstore.io/c/lethal-company/p/mrov/EXPERIMENTALWeatherTweaks), now on the main branch!
  - Added gradient colors for all the different weathers, if using `v0.8.0` and above.
  - Should be backwards-compatible with `v0.7.5`, too.

## [1.3.4] - 2026-01-26

### Another small compatibility fix

#### Fixes

- Fixed compatibility with **EVIL** (Experimental) [WeatherRegistry](https://thunderstore.io/c/lethal-company/p/mrov/EXPERIMENTALWeatherRegistry) and [WeatherTweaks](https://thunderstore.io/c/lethal-company/p/mrov/EXPERIMENTALWeatherTweaks).
  - Both stable and experimental branches should work with this version.
  - Doesn't use any of the fancy new gradient stuff just yet, but it won't error out on launch.
- Fixed compatibility with [Diversity](https://thunderstore.io/c/lethal-company/p/IntegrityChaos/Diversity), if `SnowAffectsEnemies` is enabled.

## [1.3.3] - 2026-01-16

### Small compatibility fix

#### Fixes

- Fixed `Snowfall` and `Blizzard` compatibility with [DawnLib](https://thunderstore.io/c/lethal-company/p/TeamXiaolan/DawnLib)'s custom footsteps.

## [1.3.2] - 2025-11-26

### Snow lighting fix, Heatwave changes

#### Fixes

- Fixed snow not being as bright as it's supposed to be due to dynamic sky volume profile override not applying immediately.
- Fixed `Heatwave` cooldown values no longer being updated after entering the facility once.

#### Tweaks

- Tweaked `Heatwave` cooldown mechanic a bit:
  - Adjusted `Heatwave` cooldown animation curve to be more impactful once it starts getting dark.
  - Switched to using the current moon's initial lighting intensity as its max luminosity, instead of an arbitrary value of `10`.
    - Will probably not work perfectly on all moons, but on _most_ moons it should be noticeably less intense at night.
  - Lowered minimum heat vfx parameter from `0.25` to `0.1`, when affected by no light.
- Several other miscellaneous tweaks for `Heatwave`:
  - Changed monitor color to a slightly different shade.
  - Added a bunch of volcanic/desert/beach/canyon moons to the default level whitelist filter.
  - Rebalanced default scrap multipliers.
  - Made some pretty minor optimizations.

## [1.3.1] - 2025-11-21

### Oily snow is no more

#### General

- Added a tag check to prevent water from being frozen during `Snowfall` and `Blizzard`.
  - Moon makers can now stop their water from becoming ice by adding a `voxxDontFreezeMyWater` tag to their moon.

#### Fixes

- Fixed dark or 'oily' snow during `Snowfall` and `Blizzard`.
  - From what I can tell, it was due to indirect lighting not being applied to the snow, despite the material being set up to receive it.
  - Seems to be fixed by switching `skyAmbientMode` to `SkyAmbientMode.Dynamic` on a `VisualEnvironment` volume profile override, but it's more of a workaround.
- Fixed `Heatwave` stamina hindering applying multiple times, once per player in the lobby.

#### Tweaks

- Some small tweaks to `Snowfall` and `Blizzard`:
  - Changed monitor colors to a slightly different shade.
  - Added a bunch of volcanic/desert/beach moons to the default level blacklist filter.
  - Rebalanced default scrap multiplier values a bit.
  - **NOTE:** `Snowfall` and `Blizzard` entries in the [WeatherRegistry](https://thunderstore.io/c/lethal-company/p/mrov/WeatherRegistry) config file will need to be manually reset for some of these changes to take effect.
- Added a few more default combined and progressing weathers.
  - Existing combined and progressing weathers are now registered through [Combined Weathers Toolkit](https://thunderstore.io/c/lethal-company/p/Zigzag/Combined_Weathers_Toolkit), if installed.
  - Added `Hallsnowed`, `Forsaken Cold`, and `Toxic Hurricane` as available combined weathers (requires [Wesley's Weathers](https://thunderstore.io/c/lethal-company/p/Magic_Wesley/Wesleys_Weathers)).

## [1.3.θ] - 2025-1θ-17

### Theta (θ) release

#### General

- Recompiled with updated dependencies for LC `v73`.
  - **NOTE:** This release is _not_ backwards-compatible with `v72` and below.
  - **NOTE #2:** [LEBWeatherFix](https://thunderstore.io/c/lethal-company/p/Sniper1_1/LEBWeatherFix) is no longer needed for `Toxic Smog` to work properly.
- Switched over to the new `Rpc` attribute, and removed `Netcode` patching step.

#### Fixes

- Fixed [OpenBodyCams](https://thunderstore.io/c/lethal-company/p/Zaggy1024/OpenBodyCams) soft compatibility throwing errors when _not_ present.
- Fixed `enemySpawnBlacklist` config setting not applying at the appropriate time.
  - Enemy blacklist is now applied when the snow becomes ready instead of immediately at the start of the round, so `LevelManipulator.IsSnowReady()` is not always false.

#### Tweaks

- Added custom colors to the supported combined and progressing weathers (when using [WeatherTweaks](https://thunderstore.io/c/lethal-company/p/mrov/WeatherTweaks)), and renamed `Snowfall + Solar Flare` to `Aurora Borealis`.
- Made some small (internal) tweaks to the config.
  - Orphaned nodes are now cleared automatically, if any are present.
  - Configuration file no longer saves after every individual `Bind()` call.
- Tweaked transpiler patches a bit.
  - Rewrote the ones that weren't using `CodeMatcher`, and did small formatting tweaks for the ones that were.
  - IL code end result is exactly the same, but the transpilers are now about 5% more readable!
- Did some minor refactoring all around.
  - Removed usages of null propagation with `Unity` objects.
  - Added some missing null checks.
  - Made some small optimizations.

## [1.2.81b] - 2025-03-03

### Performance improvements

- Much better VFX collision detection
- Snow doesn't appear underwater anymore
- Fixed some bugs with ice on moons with moving water level
- Added options in the config to limit FPS on cameras used for collision detection
- Fixed bugs with VSM depth map baking
- Made async processing truly parallel, should speed up mesh algorithms significantly
- Fixed a very rare bug with black screen appearing due to errors with TerraMesh

## [1.2.80b] - 2025-19-02

### Solar Flare Revamp + Various Improvements (Beta release)

#### General

- Updated to the new WeatherRegistry's config system. Now every weather has predefined weather to weather weights, special level weights and tag based filters!

#### Heatwave

- Changed the way particles spawn to a coroutine so it wouldn't prevent the game from running normally while it's being done

#### Toxic Fog

- Optimized toxic smoke visual to reduce performance overhead, added proper translucency
- Fixed possible desync of interior fumes positions

#### Snow & Blizzard

- Completely changed the way depth buffer is captured which led to better compatibility with other mods and reduction of visual snow leaking artifacts (snow appearing in places where it shouldn't)
- Fixed a bug on some moons like Titan where snow was colliding with non existing objects
- Added new configuration options for snow LOD distances
- Significantly improved performance of all VFX effects, now Lit versions (affected by lightning) are usable on lower end hardware
- Lit versions now use a diffusion profile for correct lighting
- For blizzard added an option to use a volumetric fog instead/in addition to the particle one. It has significantly better performance than VFX, but since the latter is optimized too now, both can be enabled at the same time for the best visual quality.
Volumetric only is the default option now! Fog density adjustable in the config.
- Fixed problems with ice freezing on large ocean moons, you can safely reenable it now. It will just skip huge objects now.

#### Solar Flare

- Rebalanced all of the types of solar flare, made walkie talkie less unusable (you can actually hear people now, but in a funni way, on most types of flare)
- Made default walkie noise setting slightly lower
- Overhauled sun corona visuals + now they will work on custom moons that use Unity's Physically Based Sky
- Aurora adjusts its position based on the player's position
- Made door malfunctions slightly more common (also fixed a possible desync if clients framerates were too low)
- There's now a small popup that indicates the type of solar flare
- Improved performance and robustness of the distortion filter and removed unnecessary allocations.
- OpenBodyCams compatibility - now its cameras will be distorted as well!
- Added 4 types of electric malfunctions that can occur during the flare

<details><summary>Spoiler</summary>

  ```text
  1. Turret malfunction - they can occasionally go berserk
  2 + 3. Old Birds - they can be reactivated prematurely during the day or stunned randomly, giving you some time to escape
  4. Landmine - they have a very low chance of spontaneously triggering

  Each malfunction is indicated by an electric sound and static charge particles
  ```

</details>

#### WeatherTweaks compatibility

In case WeatherTweaks is detected, adds several new weather combinations

- Eclipsed Flare (wow, pretty!)
- Snowfall > Rainy (spring is right around the corner!)
- Solar Flare > Heatwave (toasty!)
- Snowfall + Solar Flare (polar circle vibes!)

## [1.2.74b] - 2025-11-02

### Snow & Toxic update (Beta release)

- Fixed incorrect order of tag color application
- Added an event for other developers that gets invoked when snow preparation is fully complete (LevelManipulator.OnSnowReady)

## [1.2.73b] - 2025-11-02

### Snow & Toxic update (Beta release)

- Revamped the snow tracker system to remove unnecessary operations
- Fixed overly bright visuals in some cases
- Removed Fray from snow/blizzard blacklist

## [1.2.72b] - 2025-10-02

### Snow & Toxic update (Beta release)

- Changed the way weather is detected -> reduced function updates
- Fixed bug with increasing foot trackers on repeated lobby reload
- Linearized snow accumulation. Now it doesn't speed up drastically towards the end of the day, but increases by a constant value, this will cause earlier parts of the day appear snowier than before.
- Slightly increased deepness of paths left by players
- Rebalanced some configs regarding snow height as a result + blacklisted volcanic moons by default (reset WR config for snow and blizzard for that)
- Temporarily disabled ice by default
- Fixed NRE if snow is disabled, but blizzard isn't

## [1.2.71b] - 2025-24-01

### Snow & Toxic update (Beta release)

- Changed the way collision for blizzard particles is calculated to improve performance

## [1.2.70b] - 2025-24-01

### Snow & Toxic update (Beta release)

- Reverted baking changes

## [1.2.69b] - 2025-24-01

### Snow & Toxic update (Beta release)

- Fixed extremely long navmesh rebake on some moons
- Improved memory allocation for snow detection

## [1.2.68b] - 2025-20-01

### Snow & Toxic update (Beta release)

- Some performance improvements for snow and toxic fumes vfx
- New option to make snow affected by scene lighting (resource intensive!)
- Mesh processing and terrain meshification is now async to reduce stutter during level load
- Ability for moon makers to change snow and fumes colors via tags: "voxxSnowColor", "voxxSnowOverlayColor", "voxxBlizzardFogColor", "voxxBlizzardCrystalsColor", "voxxToxicFumesColor", "voxxToxicFogColor"
- Snow is more white now
- Fixed bugs when snow appeared over blizzard fog
- Fixed strange transparency glitches for snow when you could see through it at certain angles
- Fixed incorrect water freezing on some moons

## [1.2.67b] - 2025-11-01

### Snow & Toxic update (Beta release)

- Switch to coroutines for light flickering and door opening during Solar Flare to eliminate any possibilities of hangs up on huge interiors
- Added support for snow weather transition mid-round and fixed associated bugs
- Fix for frostbite accumulating faster than supposed during blizzard
- Moon processing blacklist is now a whitelist, you can add moons with MESH terrain there, that have glitchy interaction with the fluffy snow shader (not guaranteed to be a solution for every custom moon)
- Balancing adjustments for default config of Toxic Smog (you need to reset the file manually)

## [1.2.66b] - 2025-06-01

### Snow & Toxic update (Beta release)

- Small hotfix to support new TerraMesh version
- More moons are blacklisted for mesh postprocess since they are already optimal and made with TerraMesh in Editor

## [1.2.65b] - 2025-06-01

### Snow & Toxic update (Beta release)

- Deduplicated some shared objects
- Fixed bug with effects not disappearing upon death?
- Changed sounds for blizzard to avoid copyright problems (also quieter)
- Bees are now blocked from spawning during snowy weathers (configurable)
- Much better under snow detection
- Toxic fog poisoning no longer gives you an actual TZP effect, now it's just visual
- Split off some settings for blizzard from snowfall
- Moved TerraMesh stuff to a separate library
- More precise snow collision detection

## [1.2.60b] - 2024-26-12

### Snow & Toxic update (Beta release)

- Fixed NavMesh rebake lag
- Fixed solar flare reenabling itself on unsupported moons
- poisoning is now a status effect (not local to each fume instance)
- Fixed SRP batching with the snow vertex shader
- New settings for number of particles, vfx collision and enemies
- Simplified blizzard visual effects

## [1.2.50b] - 2024-25-12

### Snow & Toxic update (Beta release)

- Fixed false positives during freezing of water
- Enemies can now walk on ice
- Improved visuals for snow overlay (more textured)
- Fixed solar flare visuals missing after exiting an interior
- Fixed aurora positioning
- Added Toxic Smog weather

## [1.2.40b] - 2024-24-12

### Snow update (Beta release)

- Fixed blacklist for postprocessing working incorrectly
- Fixed buffering not clearing on moons with no valid ground
- More robust water freezing logic
- Now snow properly interacts with players (no monsters for now) that stand on non-ground objects with snow on top
- Added option to calculate more accurate shadows for fluffy snow

## [1.2.30b] - 2024-22-12

### Snow update (Beta release)

- Fixed water drowning not being disabled when it freezes over

## [1.2.20b] - 2024-22-12

### Snow update (Beta release)

- Fixed problems on experimentation
- Added config to blacklist planets with a mesh terrain from postprocessing it
- Worms no longer create trails when borrowed
- Fixed dropping from ladders in wind, now only happens when yoo're too cold to hold the metal bars
- Made chilling wave vfx dynamically resize depending on the level size
- Slightly recolored ice particles in the wave
- Changed under snow filter to be less intrusive (may change again)
- Improved snow collision detection
- Fixed incompatibility with Diversity
- Fixed a random infinite loop bug while triangulating
- Moons without proper ground will now work with the blizzard weather (without the fluffy snow part)
- Fixed freezing water in interiors

## [1.2.11b] - 2024-22-12

### Snow update (Beta release)

- Fixed ghost levels appearing in some cases

## [1.2.1b] - 2024-22-12

### Snow update (Beta release)

- Fixed NRE when patching modded assemblies

## [1.2.0b] - 2024-22-12

### Snow update (Beta release)

- Added Snowfall weather
- Added Blizzard weather
- Added more configurations for filter strengths
- Corona VFX during solar flare weather is bigger now
- Heatwave reduces intensity as the sun's brightness decreases
- Added a festive easter egg

## [1.1.8] - 2024-09-09

### Bugfix

- Hopefully fixed more incompatibilities with Lethal Performance
- Added config for the static noise from walkies, reduced default volume
- Fixed small bug with randomization picking that was forcing it to hardcoded defaults if min=max

## [1.1.7] - 2024-08-23

### Bugfix

- Hopefully fixed an incompatibility with Lethal Performance

## [1.1.6] - 2024-08-21

### Bugfix

- Fixed possible NRE under certain conditions that broke the radar map
- Improved compatibility of the teleporter distortion with other mods and fixed problems with dead bodies desyncing

## [1.1.5] - 2024-08-17

### Bugfix

- Fixed missing player voices after rejoin

## [1.1.4] - 2024-08-13

### Hotfix

- Fixed an error when full distortion of the walkie talkie sounds is enabled

## [1.1.3] - 2024-08-13

### Solar Flare Update

- New config options
- Nerfed default battery drain speed (now 60 - 200%)
- Doors can malfunction slightly more frequently now

## [1.1.2] - 2024-08-12

### Solar Flare Update

- Increased default luminosity threshold for aurora (should appear earlier now, reset config for that)
- Fixed aurora disappearing at certain angles
- Fixed missing VFX on planets without a sun
- Auroras now reappear instantly when leaving the facility

## [1.1.1] - 2024-08-12

### Solar Flare Update

- Solar flare no longer affects item batteries when you are in a facility
- On the surface discharge is even faster now (50 - 500 % faster depending on the strength of a flare)

## [1.1.0] - 2024-08-11

### Solar Flare Update

- Added new weather - Solar Flare!
- Water now cools you down in Heatwave weather
- Added auditory distortion when you have a heatstroke
- New config options

## [1.0.0] - 2024-07-24

### Release

- Initial release.
