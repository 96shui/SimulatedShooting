# Selected modular gloved arms

User-provided source: `D:/BaiduNetdiskDownload/MODULAR FPS ARMS PACK1.0.unitypackage`.
SHA256: `C09D9949ED9D580DD1CD3643A6B23C091B67CEEF0573012ED2D76797FF8D7E7F`.
User-supplied licensed content, no redistribution rights inferred.

Selected `SK_SOF_Arms_B`, `M_Gloves2CamoB`, `M_Rashguard1ColorB`, plain `M_Arms` skin.
Only forearm/hand triangles retained per side; watches, other variants, body/head, demos and PSDs excluded.
Left: 3217 vertices / 5496 triangles / 20 weighted bones. Right: 3252 vertices / 5566 triangles / 20 weighted bones.
Two independent skinned mesh assets, three shared URP/Lit materials, ten 1024px PNG textures.

Bone coordinates are rebased to wrist-forward +Z and +X finger curl to retain the existing hand/reload presentation interface. Finger names adapted to Proximal/Intermediate/Distal; endpoints added for pose discovery. Source rest-pose mesh UVs and weights retained on selected triangles. Bind poses rebuilt in the rebased coordinate frame; no runtime FBX importer or full-body animation dependency.

The original 8.26 GB `Assets/AAAModularFPSArmsPack` import was removed after confirming no external GUID references. Selected assets were exported, removed and reimported from the 15-asset `TempAssetResearch/ModularArmsBackup/SelectedGlovedArms.unitypackage` staging archive. The source archive remains outside the Unity project.

`ModularGlovedArmsAuthoring.Build` is a one-time extraction utility requiring the original selected source and prepared PNG staging files. Do not import the full package into the production project to rerun it; use a staging project. `PatchSceneHands` uses only the final hand prefabs and can update scene bindings without the original package.
