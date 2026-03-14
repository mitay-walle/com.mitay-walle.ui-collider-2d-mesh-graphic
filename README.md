# UI Collider2D Based Graphic
Unity uGUI custom graphic component extracting geometry from any Collider2D at same GameObject, include CompositeCollider2D

# Proof of Concept
Project is not production ready
# Summary
- all Collider2D type supported
- tiled uv with ppu multiplier
- any Texture
- any Material
- maskable

# Known Issues
- CompositeCollider2D GeometryType.Outlines not supported (Collider2D.CreateMesh output is always polygons, not outlines)
- Collider2D.CreateMesh always create new Mesh. GC
- preferred width / height are calculated not
- 
