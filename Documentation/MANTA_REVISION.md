# Manta replacement

The rejected procedural manta has been replaced in Underwater_Realistic with Innerscene's Giant manta ray model (Mobula birostris), licensed CC0. Source: https://www.innerscene.com/tools/library/3d-parts/giant-manta-ray-006f87da . Original GLB retained at Assets/UnderwaterArt/Scans/GiantManta.glb. The source describes it as a generalized adult study model, not a scan.

Tools/convert_manta_source.py imports the source, preserves the source albedo/normal textures, converts orientation and reduces geometry to 16,655 triangles. Unity combines it into a body renderer and one detail renderer. This replaces the old angular body, floating oval mouth, sphere eyes, thick horn-like tubes and synthetic checker-pattern shader. The new asset has a rounded central body, integrated front lobes, small lateral eyes, textured back/belly and gill slits.

The current forward-swimming and obstacle avoidance are retained. Vertex deformation adds gradual wing strokes and glide intervals; no per-frame mesh regeneration. The collision sphere includes the deformed wings. Resting stingrays are unchanged.

Validation: inspected the source in Blender, rendered upper and lower views inside Unity, and simulated 30 seconds of swimming without reef overlap. Quest performance and appearance still need hardware testing. Preview: Renders/Manta_Revised.png and Manta_Underside.png.
