# Taipei government building backdrop assets

Source: Taipei City Government Department of Urban Development, `LOD1T_2017` I3S building service.

- Service: https://www.historygis.udd.gov.taipei/arcgis/rest/services/Hosted/LOD1T_2017/SceneServer/layers/0
- Official integration notice: https://www.gov.taipei/News_Content.aspx?n=EEC70A4186D4C828&s=06836C73D332F112
- License: Taiwan Open Government Data License 1.0, compatible with CC BY 4.0. Attribution is required.
- Attribution: `臺北市政府都市發展局 2017 臺北市三維建物模型 LOD1T_2017`.

The six folders contain small, selected building nodes from Wanhua, Dadaocheng, Zhongshan, Songshan, Datong, and Nanjichang. Geometry was converted from the published I3S vertex buffers to Unity-readable OBJ. Coordinates were changed from local longitude/latitude offsets to local metres, the Z axis was converted to Unity Y-up, and atlas UV regions were baked into the OBJ UVs. Three complete low-rise nodes are used with uniform scale adaptation as non-colliding perimeter scenery; the authored enterable buildings remain responsible for interior traversal and collision.
