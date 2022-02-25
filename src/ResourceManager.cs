namespace PirateCraft
{
   public class ResourceManager
   {
      public Dictionary<FileLoc, WeakReference<Shader>> Shaders { get; private set; } = new Dictionary<FileLoc, WeakReference<Shader>>();
      public Dictionary<FileLoc, WeakReference<Texture2D>> Textures { get; private set; } = new Dictionary<FileLoc, WeakReference<Texture2D>>();

      private void ParseNodes(WorldObject parent, int[] nodeIndexes, glTFLoader.Schema.Gltf myModel, List<WorldObject> worldobjs_ordered_toplevel)
      {
         if (nodeIndexes != null)
         {
            foreach (var iNode in nodeIndexes)
            {
               var node = myModel.Nodes[iNode];
               //glTFLoader.Schema.Node 
               if (node != null)
               {
                  vec3 trans = new vec3(node.Translation);
                  vec4 rot = new vec4(node.Rotation);
                  vec3 scale = new vec3(node.Scale);
                  WorldObject wo = new WorldObject();
                  wo.Position = trans;
                  wo.Rotation = new quat(rot.x, rot.y, rot.z, rot.w);
                  wo.Scale = scale;
                  wo.Name = node.Name;
                  wo.LoaderTempData = (object)node;
                  if (parent == null)
                  {
                     worldobjs_ordered_toplevel.Add(wo);
                  }
                  else
                  {
                     parent.AddChild(wo);
                  }
                  ParseNodes(wo, node.Children, myModel, worldobjs_ordered_toplevel);
               }
               else
               {
                  Gu.Log.Error("A node was was null loading GLTF.");
               }
            }
         }
      }
      private List<WorldObject> LoadObjectNodes(glTFLoader.Schema.Gltf myModel)
      {
         //Loads a hierarchy of objects no mesh data (yet)
         List<WorldObject> worldobjs_ordered_toplevel = new List<WorldObject>();
         if (myModel != null)
         {
            //Only use first scene
            if (myModel.Scenes.Length == 0)
            {
               Gu.Log.Error("There were no scenes in the GLTF.");
               return worldobjs_ordered_toplevel;
            }
            var scene = myModel.Scenes[0];
            if (myModel.Scenes.Length > 1)
            {
               Gu.Log.Warn("There was more than 1 scene in GLTF. Only 1 scene is supported.");
            }

            ParseNodes(null, scene.Nodes, myModel, worldobjs_ordered_toplevel);
         }
         else
         {
            Gu.Log.Error("GLTF model was null upon loading nodes");
         }

         return worldobjs_ordered_toplevel;
      }
      private class MeshRaw
      {
         public byte[] Data;
         public MeshRaw(byte[] data)
         {
            Data = data;
         }
         public unsafe List<vec2> ParseVec2fArray(int scalar_count, int byte_offset)
         {
            List<vec2> ret = new List<vec2>();
            fixed (byte* raw = Data)
            {
               int component_byte_size = 4;//sizeof float
               int tensor_rank = 2;// 2 sca
               //***** not sure if count is number of bytes, or components
               for (int ioff = 0; ioff < scalar_count; ioff += tensor_rank)
               {
                  int offset = byte_offset + ioff * component_byte_size;
                  Gu.Assert(offset < Data.Length);
                  vec2 v = *((vec2*)(raw + offset));
                  ret.Add(v);
               }
            }
            return ret;
         }
         public unsafe List<vec3> ParseVec3fArray(int scalar_count, int byte_offset)
         {
            List<vec3> ret = new List<vec3>();
            fixed (byte* raw = Data)
            {
               int component_byte_size = 4;//sizeof float
               int tensor_rank = 3;// 3 scalars
               //***** not sure if count is number of bytes, or components
               for (int ioff = 0; ioff < scalar_count; ioff += tensor_rank)
               {
                  int offset = byte_offset + ioff * component_byte_size;
                  Gu.Assert(offset < Data.Length);
                  vec3 v = *((vec3*)(raw + offset));
                  ret.Add(v);
               }
            }
            return ret;
         }
         public unsafe List<ushort> ParseUInt16Array(int scalar_count/*not sure*/, int byte_offset)
         {
            List<ushort> ret = new List<ushort>();
            fixed (byte* raw = Data)
            {
               int component_byte_size = 2;//sizeof ushort
               int tensor_rank = 1;// 3 scalars
               //***** not sure if count is number of bytes, or components
               for (int ioff = 0; ioff < scalar_count; ioff += tensor_rank)
               {
                  int offset = byte_offset + ioff * component_byte_size;
                  Gu.Assert(offset < Data.Length);
                  ushort v = *((ushort*)(raw + offset));
                  ret.Add(v);
               }
            }
            return ret;
         }
         public unsafe List<uint> ParseUInt32Array(int scalar_count/*not sure*/, int byte_offset)
         {
            List<uint> ret = new List<uint>();
            fixed (byte* raw = Data)
            {
               int component_byte_size = 4;//sizeof uint
               int tensor_rank = 1;// 3 scalars
               //***** not sure if count is number of bytes, or components
               for (int ioff = 0; ioff < scalar_count; ioff += tensor_rank)
               {
                  int offset = byte_offset + ioff * component_byte_size;
                  Gu.Assert(offset < Data.Length);
                  uint v = *((uint*)(raw + offset));
                  ret.Add(v);
               }
            }
            return ret;
         }
         public unsafe float[] ParseFloatArray(int scalar_count/*not sure*/, int byte_offset)
         {
            float[] floats = new float[scalar_count];
            fixed (byte* raw = Data)
            {
               int component_byte_size = 4;//sizeof uint
               int tensor_rank = 1;// 3 scalars
               //***** not sure if count is number of bytes, or components
               for (int ioff = 0; ioff < scalar_count; ioff += tensor_rank)
               {
                  int offset = byte_offset + ioff * component_byte_size;
                  Gu.Assert(offset < Data.Length);
                  float v = *((float*)(raw + offset));
                  floats[ioff] = v;
               }
            }
            return floats;
         }
      }
      private void LoadMesh(WorldObject root, glTFLoader.Schema.Gltf myModel, byte[] gltf_data)
      {
         //TODO: there can be more than 1 buffer her, so the gltf_data would need to be a list of buffer
         //and we would use the BufferView.Buffer to select the correct buffer.
         glTFLoader.Schema.Node? node = root.LoaderTempData as glTFLoader.Schema.Node;
         if (node == null)
         {
            Gu.Log.Error("Loader temp data was not set for a node.");
            return;
         }
         if (node.Mesh == null)
         {
            Gu.Log.Warn("Given node '" + node.Name + "' had no mesh.");
            return;
         }
         //Get the mesh at the index.
         var mesh = myModel.Meshes[node.Mesh.Value];
         MeshRaw meshRaw = new MeshRaw(gltf_data);

         if (mesh.Primitives.Length == 0)
         {
            Gu.Log.Error("No mesh primitives were available for mesh..");
            return;
         }
         if (mesh.Primitives.Length > 1)
         {
            Gu.Log.Warn("Only one mesh primitive of triangles is supported.");
         }

         var prim = mesh.Primitives[0];
         glTFLoader.Schema.MeshPrimitive.ModeEnum mode = prim.Mode;
         OpenTK.Graphics.OpenGL4.PrimitiveType mesh_prim_type = OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles;
         if (mode != glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
         {
            Gu.Log.Error("Primitive mode for mesh not supported..");
            return;
         }

         //Hoist raw data into buffers.
         //This is slow - we could just use some raw index offsets to create v_.. but my brain is not working... later we fix this. No need for these buffers.
         //List<vec3> positions = new List<vec3>();
         //List<vec3> normals = new List<vec3>();
         //List<vec2> texs_0 = new List<vec2>();

         float[] positions = null;
         float[] normals = null;
         float[] texs_0 = null;

         foreach (var attr in prim.Attributes)
         {
            //3.7.2.1. Overview - these are all valid as part of the spec
            //https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html
            string position_att = "POSITION";
            string normal_att = "NORMAL";
            string texcoord0_att = "TEXCOORD_0";
            string texcoord1_att = "TEXCOORD_1";
            string texcoord2_att = "TEXCOORD_2";
            string texcoord3_att = "TEXCOORD_3";
            string texcoord4_att = "TEXCOORD_4";

            string attr_name = attr.Key;
            int accessor_index = attr.Value;
            if (accessor_index < myModel.Accessors.Length)
            {
               var attribute_accessor = myModel.Accessors[accessor_index];
               int? bufferViewID = myModel.Accessors[accessor_index].BufferView;
               if (bufferViewID != null && bufferViewID.Value < myModel.BufferViews.Length)
               {
                  var buffer_view = myModel.BufferViews[bufferViewID.Value];
                  Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
                  Gu.Assert(buffer_view.Buffer == 0);
                  if (buffer_view != null)
                  {
                     if (attr_name.Equals(position_att))
                     {
                        if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                        {
                           if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3)
                           {
                              positions = meshRaw.ParseFloatArray(attribute_accessor.Count * 3, buffer_view.ByteOffset);// meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                           }
                           else
                           {
                              Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                           }
                        }
                        else
                        {
                           Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                        }

                     }
                     else if (attr_name.Equals(normal_att))
                     {
                        if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                        {
                           if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3)
                           {
                              normals = meshRaw.ParseFloatArray(attribute_accessor.Count * 3, buffer_view.ByteOffset);// = meshRaw.ParseVec3fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                           }
                           else
                           {
                              Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                           }
                        }
                        else
                        {
                           Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                        }
                     }
                     else if (attr_name.Equals(texcoord0_att))
                     {
                        if (attribute_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT)
                        {
                           if (attribute_accessor.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC2)
                           {
                              texs_0 = meshRaw.ParseFloatArray(attribute_accessor.Count * 2, buffer_view.ByteOffset);//= meshRaw.ParseVec2fArray(attribute_accessor.Count, buffer_view.ByteOffset);
                           }
                           else
                           {
                              Gu.Log.Error("Attribute: " + attr_name + " - Invalid data type");
                           }
                        }
                        else
                        {
                           Gu.Log.Error("Attribute: " + attr_name + " - Invalid component type");
                        }
                     }
                     else
                     {
                        Gu.Log.Error("Invalid attribute name " + attr_name);
                     }
                  }
                  else
                  {
                     Gu.Log.Error("Buffer view was null.");
                  }
               }
               else
               {
                  Gu.Log.Error("Buffer view ID was null, or out of bounds.");
               }
            }
            else
            {
               Gu.Log.Error("attribute index outside of accessor");
               return;
            }
         }

         List<ushort> indices_ushort = null;
         List<uint> indices_uint = null;

         if (prim.Indices == null || prim.Indices.Value > myModel.Accessors.Length)
         {
            Gu.Log.Error("Mesh had no index data or outside bounds. This is not supported.");
            return;
         }
         {
            var index_accessor = myModel.Accessors[prim.Indices.Value];
            int? bufferViewID = index_accessor.BufferView;
            if (bufferViewID != null && bufferViewID.Value < myModel.BufferViews.Length)
            {
               //According to the spec target should not be null - element array or array buf
               var buffer_view = myModel.BufferViews[bufferViewID.Value];
               Gu.Assert(buffer_view.ByteStride == null || buffer_view.ByteStride.Value == 0);
               Gu.Assert(buffer_view.Buffer == 0);//TODO:
               if (index_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
               {
                  indices_ushort = meshRaw.ParseUInt16Array(index_accessor.Count, buffer_view.ByteOffset);
               }
               else if (index_accessor.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
               {
                  indices_uint = meshRaw.ParseUInt32Array(index_accessor.Count, buffer_view.ByteOffset);
               }
               else
               {
                  Gu.Log.Error("Invalid index type.");
                  return;
               }
            }
            else
            {
               Gu.Log.Error("Buffer View ID was null or empty for indexes.");
               return;
            }
         }
         //Ugh..
         if (indices_ushort != null)
         {
            indices_uint = new List<uint>();
            foreach (var ind in indices_ushort)
            {
               indices_uint.Add((UInt32)ind);
            }
            indices_ushort = null;
         }
         //When indices property is not defined, the number of vertex indices to render is
         //defined by count of attribute accessors (with the implied values from range [0..count));
         //when indices property is defined, the number of vertex indices to render is defined by count
         //of accessor referred to by indices. In either case, the number of vertex indices MUST be valid
         //for the topology type used:
         List<v_v3n3x2> verts = new List<v_v3n3x2>();
         for (int ind = 0; ind < indices_uint.Count; ind ++)
         {
            //** This is all incorrect. 
            //2 - v float[2]
            //13 n float[13]
            //19 t float[19
            //so each index indexes into a component in the order in which the accessors specify them.
            //Ex if accessors are POSITION NORMAL TEXCOORD, then the indexes 1 5 0, .. .. index into each of those buffers
            int num_attrs = prim.Attributes.Count;
            v_v3n3x2 v = new v_v3n3x2();
            int pos_ind = (int)indices_uint[ind];
            //int n_ind = (int)indices_uint[ind+1]/3;
            //int t0_ind = (int)indices_uint[ind+2]/2;

            //24 floats but byteLength = 288 = 72 floats = 24 verts .. ? YES this make sense. since the planes must be detached.
            //Now the question is why there are only 36 indexes up to 23 (0-24)

            //Fixed it: TODO: - make thees back into vec3/vec2 accessors

            //Wait - float count 24 for the accessors, indexes must be indexing bu ALL attributes for each float.
            v._v = new vec3(positions[indices_uint[ind]*3+0], positions[indices_uint[ind] * 3 + 1], positions[indices_uint[ind] * 3 + 2] );// positions[pos_ind];// 2 13 19, 2 19 8, 0 to 23, 36 indices - correct 1 for each of 6*2*3 triangles BUT 0-23 ? why
            v._n = new vec3(normals[indices_uint[ind] * 3 + 0], normals[indices_uint[ind] * 3 + 1], normals[indices_uint[ind] * 3 + 2]);//normals[n_ind];//8 + 8 + 12 = 16+12 = 28,  8 16 24 = so I am assuming that we lay the buffers on the side. This is the accessor.
            v._x = new vec2(texs_0[indices_uint[ind] * 2 + 0], texs_0[indices_uint[ind] * 2 + 1]);//texs_0[t0_ind];
            verts.Add(v);
         }

         root.Mesh = new MeshData(mesh.Name, mesh_prim_type, v_v3n3x2.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()));

         //TODO: materials
         Gu.Log.Warn("TODO: must create materials for objects");
         root.Material = Material.DefaultFlatColor();

         if (root.LoaderTempData != null)
         {
            if (prim.Material != null)
            {
               var ind = prim.Material.Value;
               var mat = myModel.Materials[ind];
               if(mat != null)
               {
                  if(mat.PbrMetallicRoughness != null)
                  {
                     root.Color = new vec4(
                        mat.PbrMetallicRoughness.BaseColorFactor[0],
                        mat.PbrMetallicRoughness.BaseColorFactor[1],
                        mat.PbrMetallicRoughness.BaseColorFactor[2],
                        mat.PbrMetallicRoughness.BaseColorFactor[3]);
                  }
               }
            }
            //var nn = root.LoaderTempData as glTFLoader.Schema.Node;
            //nn.
         }

         //Recur children.
         foreach (var child in root.Children)
         {
            LoadMesh(child, myModel, gltf_data);
         }
      }

      public List<WorldObject> LoadObjects(FileLoc loc)
      {
         //Load GLTF object
         //Note: this is not an optimal loading - it loads the entire mesh into memory.
         //For small meshes, small games. Not for research.

         List<WorldObject> objs = new List<WorldObject>();

         try
         {
            string path = loc.QualifiedPath;
            byte[]? model_bytes = null;
            glTFLoader.Schema.Gltf myModel = null;
            using (Stream? stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            {
               if (stream != null)
               {
                  myModel = glTFLoader.Interface.LoadModel(stream);
               }
               else
               {
                  Gu.BRThrowException("Stream from '" + path + "'was null");
               }
            }
            using (Stream? stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            {
               if (stream != null)
               {
                  model_bytes = glTFLoader.Interface.LoadBinaryBuffer(stream);
               }
               else
               {
                  Gu.BRThrowException("Stream from '" + path + "'was null");
               }
            }

            objs = LoadObjectNodes(myModel);
            foreach (var ob in objs)
            {
               LoadMesh(ob, myModel, model_bytes);
            }



         }
         catch (Exception ex)
         {
            Gu.Log.Error(ex.ToString());
         }

         return objs;
      }

      private Tx FindItem<Tx>(FileLoc loc, Dictionary<FileLoc, WeakReference<Tx>> dict) where Tx : class
      {
         Tx ret = null;
         dict.TryGetValue(loc, out var tex);
         if (tex != null)
         {
            if (tex.TryGetTarget(out var tex_target))
            {
               ret = tex_target;
            }
            else
            {
               //Remove the std::weak_reference item as it is no longer used
               dict.Remove(loc);
            }
         }
         return ret;
      }
      public Texture2D LoadTexture(FileLoc loc, bool mipmaps, TexFilter filter)
      {
         Texture2D ret = FindItem(loc, Textures);

         if (ret == null)
         {
            ret = new Texture2D(loc, true, TexFilter.Bilinear);
         }
         return ret;
      }
      public Shader LoadShader(string generic_name, bool gs, bool use_cached = true)
      {
         string vert_name = generic_name + ".vs.glsl";
         string geom_name = gs ? generic_name + ".fs.glsl" : "";
         string frag_name = generic_name + ".fs.glsl";
         string fileloc_name = vert_name + "-" + geom_name + "-" + frag_name; //hacky, but it will work
         var cache_loc = new FileLoc(vert_name, FileStorage.Embedded);

         Shader ret = FindItem(cache_loc, Shaders);

         if (ret == null)
         {
            //string vert = Gu.ReadTextFile(new FileLoc(vert_name, FileStorage.Embedded));
            //string geom = gs ? Gu.ReadTextFile(new FileLoc(geom_name, FileStorage.Embedded)) : "";
            //string frag = Gu.ReadTextFile(new FileLoc(frag_name, FileStorage.Embedded));


            string vert = Shader.ProcessFile(new FileLoc(vert_name, FileStorage.Embedded));
            string geom = gs ? Shader.ProcessFile(new FileLoc(geom_name, FileStorage.Embedded)) : "";
            string frag = Shader.ProcessFile(new FileLoc(frag_name, FileStorage.Embedded));


            ret = new Shader(generic_name, vert, frag, geom);
            Shaders.Add(cache_loc, new WeakReference<Shader>(ret));
         }
         return ret;
      }
   }
}
