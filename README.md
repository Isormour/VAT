# Vertex Animation Texture System for Unity

Way of optimalization for skinned mesh rendering by baking animation in to texture

Contains VAT Generation based on Animator with transitions included.
By default using URP

Three way of rendering.
Mesh Render + ShaderGraph
Mesh Renderer + Shader
Graphics.RenderMeshIndirect

Supports URP and HDRP

Benchmarking for 1000 units.
![alt text](https://imgur.com/a/6adGGo7)

TODO:
Add shadows for indirect rendering.
Cleanup packages for URP and HDRP shaders with examples.
