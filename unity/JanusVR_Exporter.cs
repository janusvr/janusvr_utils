#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

/*=============================================================================
|||||||	unity to janusVR exporter v.1.0 by karan singh, adapted from the obj exporter of aaro4130 with complete credit (see original comment below) !! jan 2016

 | Project:  Unity3D Scene OBJ Exporter
 |
 |		  Notes: Only works with meshes + meshRenderers. No terrain yet
 |
 |       Author:  aaro4130
 |
 |     DO NOT USE PARTS OF THIS CODE, OR THIS CODE AS A WHOLE AND CLAIM IT
 |     AS YOUR OWN WORK. USE OF CODE IS ALLOWED IF I (aaro4130) AM CREDITED
 |     FOR THE USED PARTS OF THE CODE.
 |
 *===========================================================================*/

// unity janusVR export, parts of code adapted from aaro4130's obj exporter v2.0 with due credit as requested above
 
public class janusVR_Exporter : ScriptableWizard
{
    public bool onlySelectedObjects = false;
	public bool outputFBX=false;
    public bool unifiedOBJ = false;			// break up multiple obj files
    public bool addIdNumOBJ = false;
    public bool freezeTransformOBJ = false;	
    public bool outputColliderOBJ = true;	

    public bool generateMaterials = true;
    public bool exportTextures = true;
    public bool autoMarkTexReadable = false;
	
    private string lastExportFolder;

    bool StaticBatchingEnabled()
    {
        PlayerSettings[] playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
        if (playerSettings == null)
        {
            return false;
        }
        SerializedObject playerSettingsSerializedObject = new SerializedObject(playerSettings);
        SerializedProperty batchingSettings = playerSettingsSerializedObject.FindProperty("m_BuildTargetBatching");
        for (int i = 0; i < batchingSettings.arraySize; i++)
        {
            SerializedProperty batchingArrayValue = batchingSettings.GetArrayElementAtIndex(i);
            if (batchingArrayValue == null)
            {
                continue;
            }
            IEnumerator batchingEnumerator = batchingArrayValue.GetEnumerator();
            if (batchingEnumerator == null)
            {
                continue;
            }
            while (batchingEnumerator.MoveNext())
            {
                SerializedProperty property = (SerializedProperty)batchingEnumerator.Current;
                if (property != null && property.name == "m_StaticBatching")
                {
                    return property.boolValue;
                }
            }
        }
        return false;
    }

    void OnWizardUpdate()
    {
        helpString = "Unity to JanusVR export version 1.01 (adapted from aaro4130's OBJ Exporter v2.0)";
    }

    Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }
    Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

	void writeMeshasOBJ(Mesh msh,StringBuilder sb,bool freeze,bool reflect, bool faceOrder, Matrix4x4 xform)
	{

		//export vector data
		foreach (Vector3 vx in msh.vertices)
		{
			Vector3 v = vx;
			if (freeze)
				v= xform.MultiplyPoint(v);
			if (reflect)
				v.x *= -1;
			sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);
		}
		foreach (Vector3 vx in msh.normals)
		{
			Vector3 v = vx;
			if (freeze)
				v= xform.MultiplyVector(v);	// may not work for scaling KS
			if (reflect)
				v.x *= -1;	// reflecting mesh normal in x. why? KS
			sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);
		}
		foreach (Vector2 v in msh.uv)
		{
			sb.AppendLine("vt " + v.x + " " + v.y);
		}

		for (int j=0; j < msh.subMeshCount; j++)
		{
			int[] tris = msh.GetTriangles(j);			// only tri objects? what about quads etc? KS
			for(int t = 0; t < tris.Length; t+= 3)
			{
				int idx2 = tris[t] + 1;
				int idx1 = tris[t + 1] + 1;
				int idx0 = tris[t + 2] + 1;
				if(faceOrder)
				{
					sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
				}
				else
				{
					sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
				}				
			}
		}
	}
	
    void OnWizardCreate()
    {
        if(StaticBatchingEnabled() && Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "Static batching is enabled. This will cause the export file to look like a mess, as well as be a large filesize. Disable this option, and restart the player, before continuing.", "OK");
            goto end;
        }
        if (autoMarkTexReadable)
        {
            int yes = EditorUtility.DisplayDialogComplex("Warning", "This will convert all textures to Advanced type with the read/write option set. This is not reversible and will permanently affect your project. Continue?", "Yes", "No", "Cancel");
            if(yes > 0)
            {
                goto end;
            }
        }
        string lastPath = EditorPrefs.GetString("janusVR_Export_lastPath", "");
        string lastFileName = EditorPrefs.GetString("janusVR_Export_lastFile", "unityexport.html");
        string expFile = EditorUtility.SaveFilePanel("JanusVR Export", lastPath, lastFileName, "html");
        if (expFile.Length > 0)
        {
            var fi = new System.IO.FileInfo(expFile);
            EditorPrefs.SetString("janusVR_Export_lastFile", fi.Name);
            EditorPrefs.SetString("janusVR_Export_lastPath", fi.Directory.FullName);
            Export(expFile);
        }
        end:;
    }

	///////////////////// the workhorse function ///////////////////
	
    void Export(string exportPath)
    {
        //init stuff
        var exportFileInfo = new System.IO.FileInfo(exportPath);
        lastExportFolder = exportFileInfo.Directory.FullName;
        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(exportPath);
        EditorUtility.DisplayProgressBar("JanusVR Export", "Exporting static scene...", 0);

        Dictionary<string, bool> materialCache = new Dictionary<string, bool>();		
		Dictionary<string, bool> meshCache = new Dictionary<string, bool>();
		
        //get list of required export objects
        MeshFilter[] sceneMeshes;
        if (onlySelectedObjects)
        {
            List<MeshFilter> tempMFList = new List<MeshFilter>();
            foreach (GameObject g in Selection.gameObjects)
            {
                MeshFilter f = g.GetComponent<MeshFilter>();
                if (f != null)
                    tempMFList.Add(f);
            }
            sceneMeshes = tempMFList.ToArray();
        }
        else
        {
            sceneMeshes = FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[];
        }

        if (Application.isPlaying)	//static batching stuff KS
        {
            foreach (MeshFilter mf in sceneMeshes)
            {
                MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (mr.isPartOfStaticBatch)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Error", "Static batched object detected. Static batching is not compatible with this exporter. Please disable it before starting the player.", "OK");
                        return;
                    }
                }
            }
        }
        
        //export code begins
		
        StringBuilder sb = new StringBuilder();				// will comprise content of one or many obj files 
        StringBuilder sbMaterials = new StringBuilder();	// will comprise content of one or many mtl files
        StringBuilder sbJanusVR = new StringBuilder();		// will comprise content of a single jml file

        if (unifiedOBJ && generateMaterials)	//add mtllib header into monolithic obj file
		{
			sb.AppendLine("# adapted from aaro4130 obj exporter for JanusVR export of " + Application.loadedLevelName);
			sb.AppendLine("mtllib " + baseFileName + ".mtl");
		}
		
		// JML header information
		
		sbJanusVR.AppendLine("<html>\n<head>\n<title>"+Application.loadedLevelName+"</title>");
		sbJanusVR.AppendLine("</head>\n<body>\n<FireBoxRoom>\n<Assets>");

        float maxExportProgress = (float)(sceneMeshes.Length + 1);
        int lastIndex = 0;
		int assetcnt=0;
		int boxcollidercnt=0;
		int meshcollidercnt=0;
		
        for(int i = 0; i < sceneMeshes.Length; i++)	// loop through to add assets
        {
            string gameobjName = sceneMeshes[i].gameObject.name;
            float progress = (float)(i + 1) / maxExportProgress;
            EditorUtility.DisplayProgressBar("Exporting Assets... (" + Mathf.Round(progress * 100) + "%)", "Exporting object: " + gameobjName, progress);

            MeshFilter mf = sceneMeshes[i];
            MeshRenderer mr = sceneMeshes[i].gameObject.GetComponent<MeshRenderer>();
			
			//export the mesh only if the shared mesh is not null (seems to happen sometimes). 
			Mesh msh = mf.sharedMesh;
			if (msh==null)
			{
				Debug.Log("ERROR (skipping null mesh asset): "+gameobjName);
				continue;
			}
			string meshName=msh.name;

			// only output unique assets unless outputting a monolithic OBJ			
			if (!meshCache.ContainsKey(meshName) || (unifiedOBJ && !outputFBX) ) 
            {
				meshCache[meshName] = true;
				assetcnt++;
				if (outputFBX)	// output already present fbx asset into janus file
				{
					string modelPath = AssetDatabase.GetAssetPath(msh);	
					if (modelPath!="")
						sbJanusVR.AppendLine("<AssetObject id=\""+meshName+ "\" src=\""+modelPath+"\"/>");
					//Debug.Log("names "+meshName+" path:: "+modelPath+"*");//+prefabPath+"!");
				}
				else	// only for obj export
				{
					if (i==0)	// do this once, set up JanusOBJ dir if it doesnt exist
					{
						if (!System.IO.Directory.Exists(exportFileInfo.Directory.FullName + "\\Assets\\JanusOBJ"))
							System.IO.Directory.CreateDirectory(exportFileInfo.Directory.FullName + "\\Assets\\JanusOBJ");
					}
					
					// faceorder seems dodgy based on negative scale values KS
					int faceOrder = (int)Mathf.Clamp((mf.gameObject.transform.lossyScale.x * mf.gameObject.transform.lossyScale.z), -1, 1);					

					if (unifiedOBJ)
					{
						string exportName = meshName;
						if (addIdNumOBJ)
						{
							exportName += "_" + i;
						}
						sb.AppendLine("g " + exportName);
						
						if (i==0) // do this just once
							sbJanusVR.AppendLine("<AssetObject id=\""+meshName+ "\" src=\""+ baseFileName + ".obj" +"\" mtl=\""+ baseFileName + ".mtl" +"\"/>");
					}
					else
					{
						sb.Length=0;//Clear();
						string objPath = "Assets/JanusOBJ/"+meshName+".obj";
						sbJanusVR.AppendLine("<AssetObject id=\""+meshName+ "\" src=\""+objPath+"\" mtl=\"Assets/JanusOBJ/"+meshName+ ".mtl" +"\"/>");
					}
				
					if(mr != null && generateMaterials)	//generate mtl for object
					{
						if (!unifiedOBJ)
						{
							materialCache.Clear();
							sbMaterials.Length=0;
						}	
						Material[] mats = mr.sharedMaterials;
						for(int j=0; j < mats.Length; j++)
						{
							Material m = mats[j];
							if (m!=null && !materialCache.ContainsKey(m.name))
							{
								materialCache[m.name] = true;
								sbMaterials.Append(MaterialToString(m));
								sbMaterials.AppendLine();
							}
						}
						if (!unifiedOBJ)	// write the mtl asset file out
						{
							System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\Assets\\JanusOBJ\\"+meshName+".mtl", sbMaterials.ToString());
						}
					}
					// added to mtllib or written out individual mtl file
										
					//export vector data
					foreach (Vector3 vx in msh.vertices)
					{
						Vector3 v = vx;
						//freezing xforms mostly makes sense for a unified obj unless there is only obj instance of every asset KS
						if (unifiedOBJ || freezeTransformOBJ)
								v= mf.gameObject.transform.localToWorldMatrix.MultiplyPoint(v);
						if (unifiedOBJ)
							v.x *= -1;	// why are we reflecting the x position? KS
						sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);
					}
					foreach (Vector3 vx in msh.normals)
					{
						Vector3 v = vx;
						if (unifiedOBJ || freezeTransformOBJ)
						{
								v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale.normalized);
								v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
						}
						if (unifiedOBJ)
							v.x *= -1;	// reflecting mesh normal in x. why? KS
						sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);

					}
					foreach (Vector2 v in msh.uv)
					{
						sb.AppendLine("vt " + v.x + " " + v.y);
					}

					for (int j=0; j < msh.subMeshCount; j++)
					{
						if(mr != null && j < mr.sharedMaterials.Length && mr.sharedMaterials[j]!=null )
						{
							string matName = mr.sharedMaterials[j].name;
							sb.AppendLine("usemtl " + matName);
						}
						else
						{
							sb.AppendLine("usemtl " + meshName + "_sm" + j);
						}

						int[] tris = msh.GetTriangles(j);			// only tri objects? what about quads etc? KS
						for(int t = 0; t < tris.Length; t+= 3)
						{
							int idx2 = tris[t] + 1 + lastIndex;
							int idx1 = tris[t + 1] + 1 + lastIndex;
							int idx0 = tris[t + 2] + 1 + lastIndex;
							if(!unifiedOBJ || faceOrder < 0)
							{
								sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
							}
							else
							{
								sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
							}
							
						}
					}
					if (unifiedOBJ)
						lastIndex += msh.vertices.Length;

					if (!unifiedOBJ)	// write the obj file
					{
						System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\Assets\\JanusOBJ\\"+meshName+".obj", sb.ToString());
					}
					else if (i%200==199) // for monolithic obj flush sb buffer to file every 200 objects.
					{
						System.IO.File.AppendAllText(exportPath, sb.ToString());
						sb.Length=0;
					}				
				}	// obj export done
				
				//now check if mesh has a collider. if a mesh collider that is different from the current mesh
				// export it as an asset as well so it can be referenced later.
				Collider mc=mf.gameObject.GetComponent<Collider>();			
				if (mc!=null && !unifiedOBJ)						// currently only add colliders for OBJ
				{
					if(mc.GetType() == typeof(MeshCollider))
					{	
						MeshCollider mc2=mf.gameObject.GetComponent<MeshCollider>();
						if (mc2!=null)
						{
							Mesh mshb=mc2.sharedMesh;								
							if (mshb!=null && mshb.name!=meshName && !meshCache.ContainsKey(mshb.name))
							{
								//Debug.Log(mshb.name+" is a DIFF mesh collider for "+meshName+"---------gameobj=(("+gameobjName+"))"+meshcollidercnt);
								meshCache[mshb.name] = true;
								// add this mesh as an asset
								Matrix4x4 junk= Matrix4x4.identity;
								sb.Length=0;
								writeMeshasOBJ(mshb,sb,false,false, true,junk);
								System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\Assets\\JanusOBJ\\"+mshb.name+".obj", sb.ToString());
								string objPath = "Assets/JanusOBJ/"+mshb.name+".obj";
								sbJanusVR.AppendLine("<AssetObject id=\""+mshb.name+ "\" src=\""+objPath+"\"/>");
								meshcollidercnt++;
							}
							else
							{
								Debug.Log(mc2.name+" is a mesh collider with a null shared mesh for mesh "+meshName+" of gameobj "+gameobjName);
							}
						}
						else
						{
							Debug.Log(mc.name+" is a mesh collider with a null mesh collider WEIRD, for "+meshName+" of gameobj "+gameobjName);						
						}
					}
				}
				else if (mc.GetType() == typeof(BoxCollider)) 
				{
					BoxCollider mc2=mf.gameObject.GetComponent<BoxCollider>();
					if (mc2!=null)
					{
						//Debug.Log("box "+mc2.name+" "+mc2.center+" "+mc2.size);
						boxcollidercnt++;
					}
				}/*else if (mc.GetType() == typeof(SphereCollider))
				{
					SphereCollider mc2=mf.gameObject.GetComponent<SphereCollider>();
					if (mc2!=null)
					{
						Debug.Log("sphere "+mc2.name+" "+mc2.center+" "+mc2.radius);
					}
				}
				else if (mc.GetType() == typeof(CapsuleCollider))
				{
					CapsuleCollider mc2=mf.gameObject.GetComponent<CapsuleCollider>();
					if (mc2!=null)
					{
						//Debug.Log("capsule "+mc2.name+" "+mc2.center+" "+mc2.radius+" "+mc2.height);
					}
				}*/		
			}	// added a unique asset or all if a unified OBJ
        }

        //write to disk
		if (!outputFBX && unifiedOBJ && (sceneMeshes.Length-1)%200!=199)
		{
			System.IO.File.AppendAllText(exportPath, sb.ToString());
		}
		if (!outputFBX && unifiedOBJ && generateMaterials)
        {
            System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\" + baseFileName + ".mtl", sbMaterials.ToString());
        }

		// get camera settings for portal position and orientation
		Vector3 camerapos=new Vector3(0,0,0);
		Vector3 camerafwd=new Vector3(0,0,1); 
		
		try {
			camerapos=Camera.main.transform.position; 
			camerafwd=Camera.main.transform.forward; 
		}
		catch {
			Debug.Log("No main camera transform defined");
		};
		
		
		// output skybox to janusVR	
		Material sm=RenderSettings.skybox;//skybox.material;
		if (sm!=null)
		{
			Texture st;
			st=sm.GetTexture("_FrontTex");
			if (st==null)
				Debug.Log("no front tex "+sm.name);
			else
			{
				string stf=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_front\" src=\""+stf+"\"/>");		
			}
			st=sm.GetTexture("_BackTex");
			if (st==null)
				Debug.Log("no sky tex "+sm.name);
			else
			{
				string stb=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_back\" src=\""+stb+"\"/>");		
			}
			
			st=sm.GetTexture("_LeftTex");
			if (st==null)
				Debug.Log("no sky tex "+sm.name);
			else
			{
				string stl=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_left\" src=\""+stl+"\"/>");		
			}
	
			st=sm.GetTexture("_RightTex");
			if (st==null)
				Debug.Log("no sky tex "+sm.name);
			else
			{
				string str=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_right\" src=\""+str+"\"/>");		
			}
			st=sm.GetTexture("_UpTex");
			if (st==null)
				Debug.Log("no sky tex "+sm.name);
			else
			{
				string stu=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_up\" src=\""+stu+"\"/>");		
			}
			
			st=sm.GetTexture("_DownTex");
			if (st==null)
				Debug.Log("no sky tex "+sm.name);
			else
			{
				string std=AssetDatabase.GetAssetPath(st);
				sbJanusVR.AppendLine("<AssetImage id=\"sky_down\" src=\""+std+"\"/>");		
			}
			if (st!=null) // make a room with a skybox
			{
				sbJanusVR.AppendLine("</Assets>\n<Room use_local_asset=\"room_plane\" visible=\"false\" pos=\""+
						camerapos.x+" "+camerapos.y+" "+camerapos.z+"\" fwd=\""+
						camerafwd.x+" "+camerafwd.y+" "+camerafwd.z+
						"\" skybox_left_id=\"sky_left\" skybox_right_id=\"sky_right\" skybox_front_id=\"sky_front\" skybox_back_id=\"sky_back\" skybox_up_id=\"sky_up\" skybox_down_id=\"sky_down\">");
			}		
			else
			{
				st=sm.GetTexture("_Tex"); 
				
				if (st==null)	// make a room with no skybox
					sbJanusVR.AppendLine("</Assets>\n<Room use_local_asset=\"room_plane\" visible=\"false\" pos=\""+
						camerapos.x+" "+camerapos.y+" "+camerapos.z+"\" fwd=\""+
						camerafwd.x+" "+camerafwd.y+" "+camerafwd.z+"\">");
				else	// make a room with a sphere mapped sky
				{
					string std=AssetDatabase.GetAssetPath(st);
					sbJanusVR.AppendLine("<AssetImage id=\"sky_tex\" src=\""+std+"\"/>");
					sbJanusVR.AppendLine("</Assets>\n<Room use_local_asset=\"room_plane\" visible=\"false\" pos=\""+
						camerapos.x+" "+camerapos.y+" "+camerapos.z+"\" fwd=\""+
						camerafwd.x+" "+camerafwd.y+" "+camerafwd.z+"\">");
					sbJanusVR.AppendLine("<Object id=\"sphere\" image_id=\"sky_tex\" cull_face=\"none\" pos=\"0 -400 0\" scale=\"800 800 800\" lighting=\"false\"/>");

				}
			}
		}
		else
		{
			//make a room with no skybox
			sbJanusVR.AppendLine("</Assets>\n<Room use_local_asset=\"room_plane\" visible=\"false\" pos=\""+
						camerapos.x+" "+camerapos.y+" "+camerapos.z+"\" fwd=\""+
						camerafwd.x+" "+camerafwd.y+" "+camerafwd.z+"\">");			
		}
		
		for(int i = 0; i < sceneMeshes.Length; i++)	// write out the objects
        {
			// get asset filenames and build janusVR .html file here.

			string gameobjName = sceneMeshes[i].gameObject.name;
            float progress = (float)(i + 1) / maxExportProgress;
            EditorUtility.DisplayProgressBar("Instancing objects... (" + Mathf.Round(progress * 100) + "%)", "Exporting object " + gameobjName, progress);
            MeshFilter mf = sceneMeshes[i];
            MeshRenderer mr = sceneMeshes[i].gameObject.GetComponent<MeshRenderer>();
			Mesh msh = mf.sharedMesh;
			if (msh==null)
			{
				Debug.Log("ERROR (skipping null mesh object creation): "+gameobjName);
				continue;
			}
			string meshName=msh.name;
						
			Vector3 rvx=mf.gameObject.transform.TransformDirection(Vector3.left);//right);//(1.0,0.0,0.0));
			Vector3 rvy=mf.gameObject.transform.TransformDirection(Vector3.up);//(0.0,1.0,0.0));
			Vector3 rvz=mf.gameObject.transform.TransformDirection(Vector3.forward);//(0.0,0.0,1.0));

			Vector3 objscale=mf.gameObject.transform.lossyScale;
			if (outputFBX)											// fbx object units seem to need scaling.
				objscale=0.01F*objscale;
			
			string collisionid="";
			Collider mc=mf.gameObject.GetComponent<Collider>();			
			if (mc!=null)
			{
				if(mc.GetType() == typeof(MeshCollider))
				{	MeshCollider mc2=mf.gameObject.GetComponent<MeshCollider>();
					Mesh mshb=mc2.sharedMesh;
					if (mshb!=null)
						collisionid=" collision_id=\""+mshb.name+"\"";
					else	// the mesh collider is set up but has an empty shared mesh, use the same mesh as its collider.
						collisionid=" collision_id=\""+meshName+"\"";
				}
				else if (mc.GetType() == typeof(BoxCollider)) 
				{
					BoxCollider mc2=mf.gameObject.GetComponent<BoxCollider>();
					//not dealing with boxe colliders yet KS
				}
			}

			string invisible="";
			if (mr==null || !mr.enabled)
				invisible=" visible=\"false\"";

			string xformstr=" pos=\"" + mf.gameObject.transform.position.x +" "+
										mf.gameObject.transform.position.y+" "+
										mf.gameObject.transform.position.z+
						"\" scale=\""+objscale.x+" "+objscale.y+" "+objscale.z+ 
						"\" xdir=\""+rvx.x+" "+rvx.y+" "+rvx.z+
						"\" ydir=\""+rvy.x+" "+rvy.y+" "+rvy.z+
						"\" zdir=\""+rvz.x+" "+rvz.y+" "+rvz.z+"\"";

			sbJanusVR.AppendLine("<Object id=\""+meshName+"\""+collisionid+invisible+xformstr+"/>");
		}
		
		Debug.Log("assets="+assetcnt+" meshcolliders="+meshcollidercnt+" boxcolliders="+boxcollidercnt+" objects="+sceneMeshes.Length);
		sbJanusVR.AppendLine("</Room>\n</FireBoxRoom>\n</body>\n</html>");
		
		System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\" + baseFileName + ".html", sbJanusVR.ToString());
        //export complete, close progress dialog
        EditorUtility.ClearProgressBar();
    }

    string ExportTexture(Texture2D t)
    {
		string exportName="en";
        try
        {
            if (autoMarkTexReadable)
            {
                string assetPath = AssetDatabase.GetAssetPath(t);
                var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImporter != null)
                {
                    tImporter.textureType = TextureImporterType.Advanced;

                    if (!tImporter.isReadable)
                    {
                        tImporter.isReadable = true;

                        AssetDatabase.ImportAsset(assetPath);
                        AssetDatabase.Refresh();
                    }
                }
            }

			string aPath = AssetDatabase.GetAssetPath(t);
            //    var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            //exportName = lastExportFolder + "\\" + t.name + ".png";
            //Texture2D exTexture = new Texture2D(t.width, t.height, TextureFormat.ARGB32, false);
			//Debug.Log("hmm...looking to export texture : " + t.name+ " " + aPath);
            //try{
			//t.GetPixels();}
			//catch (System.Exception ex)
			//{
				//Debug.LogException(ex);
				//return "null";
			//}
			
			//Debug.Log("still...look to export texture : " + t.name);
			//exTexture.SetPixels(t.GetPixels());
			//Debug.Log("KSgotpixels : " + t.name);
            //System.IO.File.WriteAllBytes(exportName, exTexture.EncodeToPNG());
			//Debug.Log("KSexported texture : " + t.name);
            return ("../../"+aPath);//exportName;
        }
        catch (System.Exception ex)
        {
            //Debug.Log("KSCould not export texture : " + t.name + "to "+exportName);
            return "null";
        }

    }

    private string ConstructOBJString(int index)
    {
        string idxString = index.ToString();
        return idxString + "/" + idxString + "/" + idxString;
    }
    string MaterialToString(Material m)
    {
        StringBuilder sb = new StringBuilder();

            sb.AppendLine("newmtl " + m.name);


        //add properties
        if (m.HasProperty("_Color"))
        {
            sb.AppendLine("Kd " + m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString());
            if (m.color.a < 1.0f)
            {
                sb.AppendLine("Tr " + (1f - m.color.a).ToString());
            }
        }
        if (m.HasProperty("_SpecColor"))
        {
            Color sc = m.GetColor("_SpecColor");
            sb.AppendLine("Ks " + sc.r.ToString() + " " + sc.g.ToString() + " " + sc.b.ToString());
        }
        if (m.HasProperty("_MainTex") && exportTextures)
        {
            Texture t = m.GetTexture("_MainTex");
			string exPath;
			if(m.HasProperty("_OcclusionMap"))
			{
				Texture t4 = m.GetTexture("_OcclusionMap");
				if (t4!=null)
				{
					exPath = ExportTexture((Texture2D)t4);
					sb.AppendLine("map_Ka " + exPath);
					//Debug.Log("occlusion map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_OcclusionMap").x + " " + m.GetTextureScale("_OcclusionMap").y);
					if(t4.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}		
			}
            if(t != null)
            {
                exPath = ExportTexture((Texture2D)t);
                sb.AppendLine("map_Kd " + exPath);

                sb.AppendLine("-s " + m.mainTextureScale.x + " " + m.mainTextureScale.y);
                if(t.wrapMode == TextureWrapMode.Clamp)
                {
                    sb.AppendLine("-clamp on");
                }                
            }
			if(m.HasProperty("_SpecGlossMap"))
			{
				Texture t3 = m.GetTexture("_SpecGlossMap");
                if (t3!=null)
				{
					exPath = ExportTexture((Texture2D)t3);
					sb.AppendLine("map_Ks " + exPath);
					//Debug.Log("spec map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_SpecGlossMap").x + " " + m.GetTextureScale("_SpecGlossMap").y);
					if(t3.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
            }
			if(m.HasProperty("_BumpMap"))
            {
				Texture t2 = m.GetTexture("_BumpMap");
				if (t2!=null)
                {
					exPath = ExportTexture((Texture2D)t2);
					sb.AppendLine("map_bump " + exPath);
					//Debug.Log("bump map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_BumpMap").x + " " + m.GetTextureScale("_BumpMap").y);
					if(t2.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
				else Debug.Log("missing bump map file: "+m.name);		
            }
			if(m.HasProperty("_DetailNormalMap"))
            {
				Texture t2 = m.GetTexture("_DetailNormalMap");
				if (t2!=null)
                {
					exPath = ExportTexture((Texture2D)t2);
					sb.AppendLine("map_normal " + exPath);
					//Debug.Log("detail normal map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_DetailNormalMap").x + " " + m.GetTextureScale("_DetailNormalMap").y);
					if(t2.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
				else Debug.Log("missing detail normal map file: "+m.name);		
            }
			if(m.HasProperty("_EmissionMap"))
            {
				Texture t2 = m.GetTexture("_EmissionMap");
				if (t2!=null)
                {
					exPath = ExportTexture((Texture2D)t2);
					sb.AppendLine("map_emission " + exPath);
					//Debug.Log("emission map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_EmissionMap").x + " " + m.GetTextureScale("_EmissionMap").y);
					if(t2.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
				else Debug.Log("missing emission map file: "+m.name);		
            }
			if(m.HasProperty("_ParallaxMap"))
            {
				Texture t2 = m.GetTexture("_ParallaxMap");
				if (t2!=null)
                {
					exPath = ExportTexture((Texture2D)t2);
					sb.AppendLine("map_parallax " + exPath);
					//Debug.Log("parallax map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_ParallaxMap").x + " " + m.GetTextureScale("_ParallaxMap").y);
					if(t2.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
				else Debug.Log("missing parallax map file: "+m.name);		
            }
			if(m.HasProperty("_DetailAlbedoMap"))
            {
				Texture t2 = m.GetTexture("_DetailAlbedoMap");
				if (t2!=null)
                {
					exPath = ExportTexture((Texture2D)t2);
					sb.AppendLine("map_albedo " + exPath);
					//Debug.Log("albedo map: "+exPath);
					sb.AppendLine("-s " + m.GetTextureScale("_DetailAlbedoMap").x + " " + m.GetTextureScale("_DetailAlbedoMap").y);
					if(t2.wrapMode == TextureWrapMode.Clamp)
					{
						sb.AppendLine("-clamp on");
					}
				}
				else Debug.Log("missing albedo map file: "+m.name);		
            }
        }
		return sb.ToString();
    }
    [MenuItem("File/JanusVR Export")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("JanusVR Export", typeof(janusVR_Exporter), "Export");
    }
}
#endif