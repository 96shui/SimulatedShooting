# P1 training rifle asset source

- Runtime Prefab: `Weapon_training-rifle_Blockout` (legacy path/name retained so scene and test references remain stable)
- Visual asset: `QBZ-191 - Free`
- Author: Brahian SG (`@Brahian_s_g`)
- Source: [Sketchfab model 88786970cb164adaad127a91a29b3dd8](https://sketchfab.com/3d-models/qbz-191-free-88786970cb164adaad127a91a29b3dd8)
- License: Creative Commons Attribution (CC BY); credit to the author is required in distributed builds
- Download archive: `QBC-191/source/Final test.zip`
- Imported model: `QBC-191/source/QBZ-191.obj`
- Original content: one 37.9k-triangle OBJ with body and magazine material slots, plus 4K base-color, normal, metallic and roughness textures
- Project processing: runtime texture import is capped at 2K; metallic and inverse-roughness are packed into URP metallic/smoothness textures; the standalone magazine vertex group is moved 10 mm upward to remove the visible receiver gap
- Integration: `TrainingRiflePrefabBuilder` replaces only the visual layer and preserves the `training-rifle` service ID, XR grab component, recoil root, colliders, muzzle, aim line, grip, shoulder and test-ID bindings
