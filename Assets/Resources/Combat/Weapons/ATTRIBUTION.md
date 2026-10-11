# QBZ191 removable magazine

`QBZ191_Reload.obj` derives from the repository's existing
`Assets/SimulatedShooting/Prefabs/Weapons/QBC-191/source/QBZ-191.obj`.
The original positions, UVs, normals and material assignments are preserved.
The connected magazine component (3,006 original faces) is separated from the
rifle body (34,301 faces) so it can be animated independently. No new third-party
model or texture is introduced; the original asset's attribution/license applies.

The shared rifle prefab binds the original QBZ191 materials to both pieces.
P1/P2 use the same seated geometry; only P3 installs the reload presentation.
