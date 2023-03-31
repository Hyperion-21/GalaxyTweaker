# Galaxy Tweaker
A galaxy configuerer that allows for modification and usage of custom galaxy definition files, allowing users to tweak the galaxy.

While this mod shouldn't affect old saves in any way, be careful with loading your old saves with it active.

**THIS MOD IS IN A VERY EARLY STATE! EXPECT BUGS AND CRASHES!**

## How to use
1. Download the mod like any other SpaceWarp mod (drag the BepInEx folder into the root KSP2 directory).
2. Launch the game to generate GalaxyDefinition_Default. DO NOT EDIT THIS FILE, IT GETS OVERWRITTEN WHEN THE GAME IS LAUNCHED!
3. Go into the mod folder after launching the game once, and go into the `galaxy_tweaker` folder. Inside should be `GalaxyDefinitions` and `CelestialBodyData`.

### Galaxy Definitions

A galaxy definition is a file that the game uses to determine *where* to place celestial bodies in their orbits.

1. Copy the `GalaxyDefinition_Default.json` file inside the `GalaxyDefinitions` folder, and rename it to whatever you want (the name becomes important later, so remember it). DO NOT EDIT THE DEFAULT FILE DIRECTLY, THE MOD USES IT AS A DEFAULT AND IT GETS RESET FREQUENTLY!
2. Open the new .json file, and edit it however you want. See wiki below for more detailed explanations of the values inside.

### Celestial Body Data

Each celestial body has its own celestial body data file. These determine the physical properties of a celestial body, like planetary radius and atmospheric parameters. Galaxy Tweaker organizes these files into packs.

1. Copy the `Default Pack` folder inside the `CelestialBodyData` folder, and rename it to whatever you want (the name becomes important later, so remember it). DO NOT EDIT THE DEFAULT PACK DIRECTLY, THE MOD USES IT AS A DEFAULT AND IT GETS RESET FREQUENTLY!
2. Open any of the .json files inside the new pack, and edit it however you want. See wiki below for more detailed explanations of the values inside.

### Launching
1. In KSP2, click the "Create New Campaign" button. A window should automatically appear.
2. On the list, click on the galaxy definition/celestial body data pack you created.
3. Create a new campaign. Your new galaxy should be loaded in there.

**WARNING: CELESTIAL BODY DATA PER SAVE IS NOT YET IMPLEMENTED! YOU MUST MANUALLY SELECT THE PLANET PACK A SAVE FILE WAS ORIGINALLY CREATED WITH, OR YOU MAY EXPERIENCE SAVE CORRUPTION! THIS ONLY APPLIES TO CELESTIAL BODY DATA, GALAXY DEFINITIONS REMEMBER SAVES!**

This mod remembers which save file is using which galaxy definition, so the target galaxy definition only works on new save files. Old save files should automatically be assigned to the default galaxy definition stored in the game files (not the `GalaxyDefinition_Default.json` that is automatically generated)

## Editing Galaxy Definitions
This section will act as a wiki for what everything in the galaxy definition file actually means.

### Overall Advice
- Do not have the SoI of any celestial body intercept with the SoI of any other body, unless one of the bodies is directly a moon of the other body. If an illegal SoI exists, it causes weird issues when a craft intercepts the intersection, including what seems to be erroneous deloading of celestial bodies and a breakdown of the encounter system.
- Stars — such as Kerbol — only need a `"GUID"`, `"PrefabKey"`, an empty `"OrbitProperties": {}`, and an empty `"OrbiterProperties": {}`. This places the star in the "center" of the solar system. (TO TEST: adding multiple celestial bodies without `OrbitProperties`)

### Galaxy Definition Parameters
```json
"Name": "Default",
"Version": "0.0.1",
"CelestialBodies": [
```
Some basic metadata for the galaxy definition. Don't touch this.

`"GUID": "Kerbin"`

This is how the game knows which planet you're giving orbital properties to. If this isn't set to an existing planet name, the game *will* crash.

`"PrefabKey": "Celestial.Kerbin.Simulation"`

Link to an internal group of files that are used to generate terrain and such. Format should be `"Celestial.[PLANET NAME].Simulation"`, but Kerbol uses `"Celestial.Kerbol.Scaled"`.

`"referenceBodyGuid":  "Kerbol"`

The celestial body to orbit around. 
- THE ORBITED CELESTIAL BODY MUST BE DEFINED BEFORE THE ORBITING CELESTIAL BODY! If moons are out of order, it will cause a crash!
- Yes, a moon can have a moon.
- Sphere of Influence for a celestial body is calculated only using itself and the mass of the body it is orbiting. This can result in weird SoIs in cases where submoons (moons of moons) are present.

`"inclination": "0"`

The inclination of the celestial body's orbit relative to its reference body's equator (affected by axial tilt), in degrees. Increase to make the orbit offset from the equator. [See here for more details.](https://en.wikipedia.org/wiki/Orbital_inclination)

`"eccentricity":  "0"`

The eccentricity of the celestial body's orbit around its reference body. When at 0, the orbit is a perfect circle. When at 1, the orbit is a straight line. [See here for more details.](https://en.wikipedia.org/wiki/Orbital_eccentricity)

`"semiMajorAxis": "13599840256"`

The semi-major axis of the celestial body's orbit around its reference body. When eccentricity is 0, this is identical to the orbit's radius. [See here for more details.](https://en.wikipedia.org/wiki/Semi-major_and_semi-minor_axes)

`"longitudeOfAscendingNode": "0"`
The longitude of the ascending node of the celestial body's orbit around its refernce body, in degrees, which is the angle between the ascending node and an arbitrary but universally constant "reference direction" vector (needs testing). Change this to rotate the orbit around the axis perpendicular to the equatorial plane of the reference body. [See here for more details.](https://en.wikipedia.org/wiki/Argument_of_periapsis)

`"argumentOfPeriapsis": "0"`

The argument of periapsis of the celestial body's orbit around its reference body, in degrees, which is the angle between the ascending node and periapsis. Change this to rotate the orbit around the axis perpendicular to the inclined orbital plane. [See here for more details.](https://en.wikipedia.org/wiki/Argument_of_periapsis)

`"meanAnomalyAtEpoch":  "3.14"`

The mean anomaly of the celestial body's orbit around its reference body at epoch (presumably UT 0:00:00), in radians (NOT DEGREES). This determines the position of the celestial body in its orbit at epoch. When this is set to 0, the planet will start the game at its periapsis; when set to π (game uses 3.14 in galaxy definitions), it is at its apoapsis. This is not to be confused with true anomaly. [See here for more details.](https://en.wikipedia.org/wiki/Mean_anomaly)

`"epoch": "0"`

(presumably, needs testing) The time offset from epoch, likely in seconds. Functionally, this should work the same as mean anomaly at epoch, but measured in time instead of an angle. All stock celestial bodies set this value to 0, so it's recommended not to mess with this value.

```json
"orbitColor": 
{
    "r": "0.30",
    "g": "0.31",
    "b": "0.35",
    "a": "0.2"
}
```
The color of the orbital line of the celestial body. `r` is red, `g` is green, `b` is blue, and `a` is alpha (transparency); all of these values range from 0-1.

```json
"nodeColor": 
{
    "r": "0.30",
    "g": "0.31",
    "b": "0.35",
    "a": "0.3"
}
```
The color of the dot/node on the orbital line of the celestial body. `r` is red, `g` is green, `b` is blue, and `a` is alpha (transparency); all of these values range from 0-1.

## Editing Celestial Body Data

This section will act as a wiki for what everything in celestial body data means.

FOREWARNING: We know considerably less about celestial body data than galaxy definitions. Much of this information is assumed. There's also a lot more here. For the time being, not all values are listed here.

`"bodyName": "Kerbin"`

The ID of the celestial body. Should match the GUID in the galaxy definition.

```json
"assetKeyScaled": "Celestial.Kerbin.Scaled.prefab",
"assetKeySimulation": "Celestial.Kerbin.Simulation.prefab"
```

Determines which bundles of information to read for terrain PQS and visuals. THEORETICALLY, if someone figures out how to create a planet prefab and creates an addressable for that, that address could be used here to effectively load in a new planet.

```json
"bodyDisplayName": "#autoLOC_910048",
"bodyDescription": "#autoLOC_900101"
```

Localization files. Determines some of the written text releated to the planet, like name and description.

`"isStar": false`

Self-explanatory, however it's ambiguous as to what this actually does.

`"isHomeWorld": true`

The starting planet. Highly recommended to not touch this, because Kerbin is very hard-coded to be the default planet, and the game will crash in a million ways if another planet is set as the home world.

```json
"navballSwitchAltitudeHigh": 36000.0,
"navballSwitchAltitudeLow": 33000.0
```

Going above the high value (in meters ASL) from the surface of the celestial body switches the navball to orbit. Going below the low value from space switches the navball to surface.

`"radius": 600000.0`

Planet radius, in meters from the center. CHANGING THIS WILL CAUSE GRAPHICAL GLITCHES AND TERRAIN ISSUES; THIS IS NORMAL. Trying to figure out how to stop these issues from happening is a primary issue that is being looked into.

`"gravityASL": 1.00034160493135`

Gravity in Gs, at sea level. This and radius are the *only* factors that influence a celestial body's gravity.

`"isRotating": true`

Self-explanatory.

`"isTidallyLocked": false`

Sets rotation speed such that one side of the celesttial body always faces its parent body.

`"initialRotation": 90.0`

Starting rotation, in degrees. Unknown what 0 degrees represents.

`"rotationPeriod": 21549.425`

Time in seconds it takes to complete a sidereal year.

```json
"axialTilt": {
    "x": 0.0,
    "y": 0.0,
    "z": 0.0
}
```

The angle, in degrees, that the celestial body's equator is offset from its parent body's equator. All stock planets only use the `x` value. An example would be `"x": 23.4` to set a celestial body's axial tilt to 23.4 degrees.

`"hasAtmosphere": true`

Toggles the *physical* atmosphere of a celestial body. If off, atmosphere graphics still exist, but gameplay-wise the celestial body's air pressure is 0.

`"atmosphereContainsOxygen": true`

Determines if jet engines work.

`"atmosphereDepth": 70000.0`

Value used to determine whether to apply atmospheric effects to a craft, based on altitude ASL in meters.

```json
"atmospherePressureCurve": {
    "fCurve": {
        "serializedVersion": "2",
        "m_Curve": [
            {
```

A float curve that determines atmosphere pressure at altitudes. Float curves are very complex, so expect issues if you try changing these too much.
