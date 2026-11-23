# Culling, anchors, fish-eye and texture review

## Applied repairs

- Padded fish mesh bounds for the full shader tail/bend envelope, including distance meshes. Kept distance detail switching. Lowered only the 30 added fish LOD cutoffs so they do not disappear at a visibly close distance; did not disable scene-wide culling.
- Padded the single water-surface renderer bounds to cover the wave displacement and deep viewing positions. Its background shader now uses far-plane depth, avoiding the 60 m camera cutoff while ordinary scene geometry retains that cutoff. Background depth writes keep the skybox from overwriting the surface. Surface brightness still falls with habitat depth.
- Enlarged both supplied anchors by 20%, laid their exported plane flat, and grounded their transformed vertices against the actual saved seabed colliders. Closest contact is embedded 1.5 cm to avoid a floating gap. The ship pose was not changed.

Unity 6000.4.5f1 checks: 96 upward view directions across eight habitats at a 60 m far plane; shader-deformed fish vertices contained in their bounds; scene scripts/materials/24 teleport anchors valid. Shallow and trench surface renders inspected. Previous grab/release, jellyfish collision and shark animation checks also rerun. These changes have not yet been rebuilt or verified in the headset.

## Appearance findings

21 material/model variants were rendered in neutral inspection lighting from both sides. These are visual comparisons, not scientific species identifications for unnamed generic assets. Source images were researched as reference only, not downloaded into project textures. No fish texture/model replacements were made in this pass.

| Model / variant | Eye check | Texture and anatomy assessment |
|---|---|---|
| Imported flame angelfish | Dark pupil and warm iris visible on both sides | One of the closer matches: orange/red, dark vertical bars and blue fin edges. Still a flat painted skin with limited fine detail. |
| Imported clownfish | Both eyes present; clean dark pupil | Orange body, three white bars, black edging reasonably match references. Best clownfish of the two variants. |
| Older clownfish | Eyes present but tiny, with dark speckling around the head | Bar motif recognizable; generic bass-like fins/body and redder pigment are less convincing than the imported model. |
| Imported butterflyfish | Real front eye plus a separate dorsal false eyespot; these are not duplicate eyes | Major mismatch: strong blue/purple cast and yellow bars. Copperband reference is silver/white with copper-orange bands, long snout and a dorsal false eyespot. |
| Older copperband butterflyfish | Small pupil present; head has inherited dark speckling | Orange/white bands are closer in color, but the bass-like body lacks the defining long snout and tall disc profile. |
| Yellow tang | Small eyes present; inherited dark forehead speckling | Yellow is recognizable, but the generic bass shape, mouth and fins do not match a tang's flattened oval body and small extended snout. |
| Blue tang | Small eyes present; forehead speckling | Blue/yellow palette recognizable; body shape and broad black stripe do not reproduce the characteristic palette-shaped black marking faithfully. |
| Royal gramma | Eyes present but simplified | Purple head and yellow rear follow the reference color direction. Body proportions and pattern transition are simplified. |
| Lyretail anthias | Eyes present but small/flat | Orange is plausible for females; generic fins/tail and lack of sex-specific variants limit realism. |
| Sergeant major | Eyes present but small | Vertical-bar motif present; pale body and yellow upper-body sheen are simplified. Generic fin anatomy remains. |
| Seabass | Eyes and silver scale texture present on both sides | Plausible generic silver seabass. Exact species is not established by the asset name, so no species-accurate claim is made. |
| Grouper | Eyes present but generic | Mottled pattern looks plausible at a distance; shared base mesh and procedural spots are not a verified species-specific grouper skin/anatomy. |
| Lionfish | Small eyes visible | Stripe motif is recognizable, but the rigid sparse spine/fan geometry is simplified. Several fin-ray renderers use a reef-sand material, which is an appearance mismatch. |
| Anglerfish | Eyes present | A stylized bass-like fish with lure rather than a faithful deep-sea angler head, mouth and skin. |
| Lumen companion | Eyes present; body emission was removed in the prior correction | Uses a generic silver-fish atlas, not a verified species-specific anglerfish texture. |
| Moray | Small head eyes visible on both sides | Yellow-green skin is plausible for some morays. The glossy circular feature behind the head appears to be the gill opening, not a third eye; its shiny rim is visually misleading. |
| Shark | Small dark head eyes visible | Gray dorsal/white underside and anatomy look like a white shark. The encounter is labeled reef shark, so the label and species appearance disagree. |
| Stingray | Dorsal eye structures present; side view is limited for checking both sockets | Procedural mottling and smooth skin are stylized; no exact ray species is verified. |
| Manta | Eye/head detail present, but a side view is limited for dorsal/ventral pattern checks | Countershaded model is more convincing than the generic reef fish. Exact species markings not established. |
| Turtle (also included) | Dedicated eye material/geometry present | Shell/body atlas present; species-specific daylight color fidelity is not certified by this fish-focused review. |
| Dolphin (also included) | Dark eye details present | Natural gray body/countershading reads plausibly. Material filename contains UV_GRID, but the rendered animal does not show a checkerboard; filename alone is not evidence of a missing texture. |

## References used

- [Copperband / beaked coralfish, Australian Museum](https://australian.museum/learn/animals/fishes/beaked-coralfish-chelmon-rostratus/)
- [Clown anemonefish physical characteristics, Georgia Aquarium](https://www.georgiaaquarium.org/animal/clown-anemonefish/)
- [Blue tang, Monterey Bay Aquarium](https://www.montereybayaquarium.org/animals-the-ocean/animals-a-to-z/blue-tang)
- [Yellow tang, Waikiki Aquarium](https://www.waikikiaquarium.org/experience/animal-guide/fishes/surgeonfishes/yellow-tang/)
- [Sergeant major, Florida Museum](https://www.floridamuseum.ufl.edu/discover-fish/species-profiles/sergeant-major/)
- [Royal gramma / fairy basslet, University of the West Indies](https://sta.uwi.edu/fst/lifesciences/sites/default/files/lifesciences/documents/ogatt/Gramma_loreto%20-%20Fairy%20Basslet.pdf)
- [Lyretail anthias, Oregon Coast Aquarium](https://aquarium.org/animals/lyretail-anthias/)
- [Flame angelfish description and photographs](https://en.wikipedia.org/wiki/Flame_angelfish)
- [Lionfish identification, NOAA](https://media.fisheries.noaa.gov/dam-migration/lionfish_factsheet.pdf)
- [Moray gill-opening anatomy, Waikiki Aquarium](https://www.waikikiaquarium.org/experience/animal-guide/fishes/eels/undulated-moray/)
- [White shark, NOAA](https://www.fisheries.noaa.gov/species/white-shark)

Rendered evidence: Renders/CullingAnchorReview/FishOverview.png, numbered Fish/* views, Anchor2.png, Anchor7.png and SurfaceZone*.png. FishInventory.txt maps the stable alphabetical numbers to project models/materials.
