# D3 enemy asset

- Source: user-supplied `Detailed Characters人物、枪械/FBX_Characters/D3_MIXAMO/D3_MIXAMO.fbx`.
- Selected character: D3, the upper-right gas-mask soldier in the user's circled reference. The source FBX includes a separate back-cylinder mesh (`D3_Baloons`), which is hidden on the enemy Prefab at the user's request.
- Textures: selected color and normal maps from the supplied `4k_textures` folder, resized to at most 1024 pixels for the VR prototype. The suit uses the olive green `D3_set2_dirt_color` variant. Original source files remain untouched.
- The external flamethrower prop has been removed from the enemy Prefab. The original enemy weapon's muzzle and combat references remain bound, with its mesh hidden.
- Installer: `Tools > Simulated Shooting > Scene 3 > Install D3 Enemy` rebuilds the enemy visual and animation controller while preserving `CombatActorView` bindings.
