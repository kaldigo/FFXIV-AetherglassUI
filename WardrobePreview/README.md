# Wardrobe preview

This Windows preview renders Aetherloom’s WardrobeLayout with AetherglassUI controls. Clone FFXIVAetherloom alongside this repository under the folder name Aetherloom, and configure DALAMUD_HOME or use the default XIVLauncher development installation. To capture a scene from the repository root, run

    dotnet run --project WardrobePreview -c Release -- dye dye.png

Scenes are gear, accessories, appearance, items, dye, hair, paint, and assignments. Omit the output path to explore the window. The layout, popups and shell are real ImGui controls. The desktop fixture supplies sample inventory and static character and item art from the approved mockups. The game supplies character rendering, game icons, item availability and catalog data.

The renders folder contains captures at 1250 × 760. Original design references live under docs/wardrobe in Aetherloom.
Set WARDROBE_PREVIEW_WIDTH to change the capture width. The gear-wide capture uses 1500 pixels for comparison with the default 1250 pixels.
