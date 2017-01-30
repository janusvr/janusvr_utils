Janus VR Unity Exporter v2.0 - Alpha 3

- To Install the Janus VR Exporter
Open your project in Unity then hit:
Assets -> Import Package -> Custom Package
And point to the "JanusVRExporter.unitypackage" file inside the Release folder.

- To open the Janus Exporter, hit the Edit -> Janus VR Exporter 2.0 menu. You can add the shown window to your Unity layout (the parameters will be saved).

Export Path
	The folder you want to export your scene to
	
Default Mesh Format
	The format to default every mesh to export to. Only FBX is supported right now, but OBJ support is coming.
	
Default Texture Format
	The format to default every texture to export to. PNG are lossless, so quality options are ignored. Supported formats are PNG and JPG.
	
Texture Filter When Scaling
	The filtering mode to use when scaling down textures.
	
Default Textures Quality
	The quality to initialize every texture that's using a lossy export format (like JPG).
	
Uniform Scale
	A uniform scale to transform the entire scene.
	
Export Lightmaps
	Only available if you have built lightmaps,.
	
Max Lightmap Resolution
	The maximum width/height the lightmaps can have.
	
Bake Material to Lightmaps
	As of Janus VR 50, this option is obligatory if you're exporting lightmaps. It will bake each objects lightmaps to a custom material. It's a memory hog.
	
Start Export
	Begin the Export process, listing up all textures/meshes and rendering needed lightmaps.
	
	Per Texture Options
		You can modify the Format, Resolution (will be changed to the closest power of 2) or quality if using a lossy format
		
	Per Model Options
		You can modify the Format the mesh is going to be exported.