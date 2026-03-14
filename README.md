# UI Collider2D Based Graphic
Unity uGUI custom graphic component extracting geometry from any Collider2D at same GameObject, include CompositeCollider2D

![](https://github.com/mitay-walle/com.mitay-walle.ui-collider-2d-mesh-graphic/blob/main/graphic-preview.png)
![](https://github.com/mitay-walle/com.mitay-walle.ui-collider-2d-mesh-graphic/blob/main/inspector-preview.png)
# Proof of Concept
Project is not production ready
# Summary
- all Collider2D type supported. include CompositeCollider2D
- tiled uv with ppu multiplier
- any Texture
- any Material
- maskable
- flat color

# Known Issues
- CompositeCollider2D GeometryType.Outlines not supported (Collider2D.CreateMesh output is always polygons, not outlines)
- Collider2D.CreateMesh always create new Mesh. GC
- preferred width / height are calculated, but not respect pivot
- atlased Sprite is not supported
- graphic is flashing transparent when transform moves or mesh is edited
