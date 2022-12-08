# MBI2 Binary Scene Exporter
#
# blender:
#   exec(compile(open("/home/mario/git/PirateCraft/tools/mbi_export.py").read(), "/home/mario/git/PirateCraft/tools/mbi_export.py", 'exec'))
#
# terminal:
#  ./blender -b -P ~/git/PirateCraft/tools/mbi_export.py  ~/git/PirateCraft/data/aasdf.blend  -- -o ./mob
#
# Notes: Running from terminal is faster
#        Make sure actions are in the NLA editor
#        Output file name is same as file name

import bpy
from mathutils import Vector, Matrix, Euler
from bpy import context
import bmesh
import os
import math
import shutil
import time
import struct
import sys, traceback
import argparse
import zlib
import multiprocessing
import builtins as __builtins__

class MobConfig:
  _exportText = False # export (mob) text file as well (for debugging) this is slow
  _blnMakeBackup = True
  _calcNormals = False
  _flipTris = False
  _flipUVs = False
  _saveTextures = True
  _prsKeyframes = True
  _keyframeSampleRate = 1 # [0.01,inf]
  _convertY_Up = True #openGL - y=up
  _pbr_material_images_only = True #only export images used for materials

class KeySamples:
    kmin : int = 0
    kmax : int = 0
    count : int =0
    pos = bytearray()
    rot = bytearray()
    scl = bytearray()

class FastBuffer:
  _chunksize = 8192
  _buffer = bytearray(_chunksize)
  _index = 0
  
  def buffer(self): return self._buffer

  def pack(self, bytes : bytearray):
    newsize = self._index + len(bytes)
    if newsize > len(self._buffer):
      chunks = (newsize - len(self._buffer)) / self._chunksize
      newsize = int(chunks) * self._chunksize
      self._buffer.extend(newsize)
    self._buffer[self._index : self._index + len(bytes)] = bytes     
  def packVec2(self, val):
    self.packFloat(val[0])
    self.packFloat(val[1])
  def packVec3(self, val):
    self.packFloat(val[0])
    self.packFloat(val[1])
    self.packFloat(val[2])
  def packVec4(self, val):
    self.packFloat(val[0])
    self.packFloat(val[1])
    self.packFloat(val[2])
    self.packFloat(val[3])
  def packInt16(self, val): self.pack(struct.pack('h', val))
  def packUInt16(self, val): self.pack(struct.pack('H', val))
  def packInt32(self, val): self.pack(struct.pack('i', val))
  def packUInt32(self, val): self.pack(struct.pack('I', val))
  def packBoneIDu16(self, val): self.packUInt16(val)
  def packNodeIDu32(self, val): self.packUInt32(val)
  def packFloat(self, val): self.pack(struct.pack('f', val))
  def packDouble(self, val): self.pack(struct.pack('d', val))
  def packString(self, str):
      bts = bytes(str,'utf-8')
      self.packInt32(len(bts))
      self.pack(bts)

#(bpy.types.Operator)
#https://docs.blender.org/api/current/bpy.types.Operator.html
class Mbi2Export:
  _mbiFileVersion = 0.01
  _strPathName = ""
  _exportName = ""

  _iWarningCount = 0
  _iErrorCount = 0
  
  _arm_bone_ids = dict() # [armature, [bone, boneid]]
  _arm_bone_name_id = dict() # [armature, [string, bone]]
  _node_ids = dict()
  _node_name_node = dict()
  _mat_ids = dict()
  _mat_name_mat = dict() 
  _arm_ids = dict()
  _arm_name_arm = dict() 
  _image_ids = dict()
  _image_name_image = dict()
  _mesh_ids = dict()
  _mesh_name_mesh = dict()
  _material_images = dict() # [bpy.image] -> [image id]

  _config = MobConfig()

  _binFile = None
  _strText = ""
  _block = None
  _blockname = None
  _bytesWritten = 0

  _bm_new_tmp = None
  _bm_old_tmp = None

  #stats
  _t_nodes = 0
  _t_meshes = 0
  _t_materials = 0
  _t_images = 0
  _t_armatures = 0
  _t_skins = 0
  _t_animation = 0

  def __init__(self, strOutputPath):
    self._strPathName = strOutputPath

  def export(self):
    #blendfile =os.path.abspath(__file__);
    #ok.. https://developer.blender.org/T54312
    bpy.ops.wm.open_mainfile(filepath="/home/mario/git/PirateCraft/data/angelina.blend")
    print(bpy.data.filepath)
    print(bpy.context.blend_data.filepath)
    print(bpy.context.scene.name)
    self._exportName = ""# os.path.splitext(os.path.basename(blendfile))[0]

    #Select NOTHING
    #bpy.context.scene.objects.active = None
    
    t_beg = millis()

    textpath = self.getMBITextPath()
    binpath = self.getMBIBinaryPath()
    
    #TODO: load and save individual configs
    self._config = MobConfig()
    #jsonStr = json.dumps(laptop1.__dict__)
    #data = json.loads('{"CalcNormals":False, "FlipTris": False, "FlipUVs": False, "SaveTextures": True}',
    #              object_pairs_hook=OrderedDict)

    msg("exporting '" + binpath + "'")
    if self._config._exportText:
      msg("exporting '" + textpath + "'")

    #Create mob dir
    self.makePath(binpath)
    self.makePath(textpath)

    #backup
    if self._config._blnMakeBackup == True:
      self.tryBackupFile(binpath)
      if self._config._exportText:
        self.tryBackupFile(textpath)

    #export
    self.export_start(binpath, textpath)

    #print result
    ap = "kB"
    amt = self._bytesWritten/1024
    if amt > 1:
      amt = amt / 1024
      ap = "MB"

    msg("..done " + str(millis() - t_beg) + "ms, " + ('{:.2f}'.format(amt)) + ap);


    return
  def getMobFullPathWithoutFilename(self):
    if self._strPathName[-1:] != "\\" and self._strPathName[-1:] != "/":
      self._strPathName += "/"
      
    basePath = self._strPathName
    
    if not os.path.exists(basePath):
      os.mkdir(basePath)
    mobFolder = self._exportName
    mobFullPath = os.path.join(basePath, mobFolder)
    
    return mobFullPath

  ##############################################################################
  #
  # Files / Path / System
  #

  def getMBIBinaryPath(self):
    mobFileName = self._exportName + ".mbi"
    mobFullPath = self.getMobFullPathWithoutFilename()
    mobFullPathWithFilename = os.path.join(mobFullPath, mobFileName)
    return mobFullPathWithFilename    
  def getMBITextPath(self):
    mobFileName = self._exportName + ".mob"
    mobFullPath = self.getMobFullPathWithoutFilename()
    mobFullPathWithFilename = os.path.join(mobFullPath, mobFileName)
    return mobFullPathWithFilename    
  def makePath(self, filePath):
    #Create path for file if it doesn't exist
    strPath = os.path.dirname(os.path.abspath(filePath.strip()))
    if not os.path.exists(strPath):
      os.mkdir(strPath)
    return
  def tryBackupFile(self, filePath):
    fullPath = self.getMobFullPathWithoutFilename()
    #This will remove the folder from the  path
    strPath = os.path.dirname(os.path.abspath(fullPath.strip())) 
    strBackupPath = os.path.join(strPath, "_backup")
    
    if not os.path.exists(strBackupPath):
      os.mkdir(strBackupPath)
    
    strFileName, strFileExt = os.path.splitext(os.path.basename(filePath))    
    strBackupFilePath = os.path.join(strBackupPath, strFileName + "_" + str(time.time()) + strFileExt)
    
    #copy src, dst
    if os.path.isfile(filePath):
      shutil.copyfile(filePath, strBackupFilePath)
      
    msg("Backup success: " + strBackupFilePath)
    
    return

  ##############################################################################
  #
  # Write
  #
  def writeString(self, str):
    #https://docs.python.org/3/library/struct.html
    bts = bytes(str,'utf-8')
    self.writeInt32(len(bts))
    self.writeData(bts)
  def writeBool(self, val):
    self.writeData(struct.pack('?', val))
  def writeByte(self, val):
    #unsigned char..
    self.writeData(struct.pack('B', val))
  def writeInt16(self, val):
    self.writeData(struct.pack('h', val))
  def writeUInt16(self, val):
    self.writeData(struct.pack('H', val))       
  def writeInt32(self, val):
    self.writeData(struct.pack('i', val))  
  def writeUInt32(self, val):
    self.writeData(struct.pack('I', val))  
  def writeInt64(self,val):
    self.writeData(struct.pack('q', val))
  def writeUInt64(self, val):
    self.writeData(struct.pack('Q', val))  
  def writeFloat(self, val):
    self.writeData(struct.pack('f', val))  
  def writeDouble(self, val):
    self.writeData(struct.pack('d', val))  
  def writeData(self, data : bytearray):
    #append to the current block, or, none
    if self._block != None:
      self._block.extend(data) #append?
    else:
      self._binFile.write(data)
      self._bytesWritten += len(data)

  def writeMat4(self, val):
    #mat_4 = val.to_4x4()
    for row in range(4):
      for col in range(4):
        self.writeFloat(val[row][col])
    return
  def writeVec2(self, val):
    self.writeFloat(val[0])
    self.writeFloat(val[1])
    return
  def writeVec3(self,  val):
    v = val
    self.writeFloat(val[0])
    self.writeFloat(val[1])    
    self.writeFloat(val[2])    
    return
  def writeVec4(self,  val):
    self.writeFloat(val[0])
    self.writeFloat(val[1])    
    self.writeFloat(val[2])       
    self.writeFloat(val[3])       
    return
  def writeQuat(self, val):
    self.writeVec4(val)
    return        
  def writeMatrixPRS(self, mat):
    loc, rot, sca = mat.decompose()
    self.writeVec3(self.glVec3(loc))
    self.writeVec4(self.glQuat(rot))
    self.writeVec3(self.glVec3(sca))    
  def writeNodeID(self, val):
    self.writeUInt32(val)
  def writeBoneID(self, val):
    self.writeUInt16(val)      
  def startBlock(self, blockname):
    if self._block != None:
      throw("Tried to start a new block '" + blockname + "' within current block")
    self._block = bytearray()
    self._blockname = blockname
  def endBlock(self):
    if self._block == None:
      throw("Tried to end a not started block")
    cmp = zlib.compress(self._block)
    self.writeString(self._blockname)
    self.writeBool(True) # Always compressing blocks in this one
    crc = zlib.crc32(cmp)
    self.writeUInt32(crc)
    self.writeUInt32(len(cmp))
    self._binFile.write(cmp)
    #msg("[" + self._blockname + "] " + str(len(self._block)) + " -> " + str(len(cmp)))
    self._block = None
    self._blockname = None
  ##############################################################################
  #
  # Export
  #
  def export_start(self, binpath, textpath):
    cur_mode = bpy.context.object.mode
    bpy.ops.object.mode_set(mode = 'OBJECT')
    self.export_to_file(binpath, textpath)
    bpy.ops.object.mode_set(mode = cur_mode)
  def export_to_file(self, binpath, textpath):
    with open(binpath, 'wb') as self._binFile:
      self.writeString("MBI2")
      self.writeFloat(self._mbiFileVersion)
     
      self.create_maps()
      self.export_nodes()
      msg('nodes '+str(self._t_nodes)+"ms")
      self.export_meshes()
      msg('meshes '+str(self._t_meshes)+"ms")
      self.export_materials()
      msg('materials '+str(self._t_materials)+"ms")
      self.export_images()
      msg('images '+str(self._t_images)+"ms")
      self.export_armatures()
      msg('armatures '+str(self._t_armatures)+"ms")      
      self.export_skins()
      msg('skins '+str(self._t_skins)+"ms")
      self.export_animation()
      msg('animation '+str(self._t_animation)+"ms")

      self._binFile.close()
    if self._config._exportText:
      with open(textpath, 'w') as t:
        t.write(self._strText)
        t.close()
  def create_maps(self):
    self.gen_ids(bpy.data.objects, self._node_ids, self._node_name_node)
    self.gen_ids(bpy.data.materials, self._mat_ids, self._mat_name_mat)
    self.gen_ids(bpy.data.armatures, self._arm_ids, self._arm_name_arm)
    self.gen_ids(bpy.data.images, self._image_ids, self._image_name_image)
    self.gen_ids(bpy.data.meshes, self._mesh_ids, self._mesh_name_mesh)
    self.create_bone_ids()
  def gen_ids(self, datasource : list, id_dict : dict, name_dict : dict):
    iid = 0
    for x in datasource:
      id_dict[x] = iid
      name_dict[x.name] = x
      iid += 1    
  def create_bone_ids(self):
    for arm in bpy.data.armatures:
      boneid = 0
      self._arm_bone_ids[arm] = dict()
      self._arm_bone_name_id[arm] = dict()
      for bone in arm.bones:
        self._arm_bone_ids[arm][bone] = boneid
        self._arm_bone_name_id[arm][bone.name] = bone
        boneid += 1
  def export_nodes(self):
    t = millis()
    self.startBlock("nodes")
    for ob in bpy.data.objects:
      self.export_node(ob)
    self._t_nodes = millis() - t;
    self.endBlock()
  def export_node(self, ob):
    self.writeString(ob.name)
    self.writeNodeID(self._node_ids[ob])
    self.writeMatrixPRS(ob.matrix_local)

    #refs
    self.writeByte(get_node_type_id(ob))
    data = bytearray()
    if is_mesh(ob):
      packNodeIDu32(data, self._mesh_ids[ob.data])
      count = 0
      for mat_slot in ob.material_slots:
        packNodeIDu32(data,self._mat_ids[mat_slot.material])
        count += 1
      packUInt32(data,count)
    elif is_armature(ob):
      packNodeIDu32(data, self._arm_ids[ob.data])
    self.writeData(data)

    #children
    self.writeInt32(len(ob.children))
    for obc in ob.children:
      self.writeNodeID(self._node_ids[obc])

  def export_animation(self):
    t = millis()
    ob_samples = dict() #[ob, <[Action, KeySample]>]

    #get min/max
    min_start = 0 
    max_end = 0
    for ob in self._node_ids.keys():
      if not ( ob.animation_data == None ):
        ob_samples[ob] = dict()
        for nla in ob.animation_data.nla_tracks:
          for strip in nla.strips:
            ob_samples[ob][strip.action] = KeySamples()
            ob_samples[ob][strip.action].kmin = strip.action.frame_range[0]
            ob_samples[ob][strip.action].kmax = strip.action.frame_range[1]
            max_end = max(strip.action.frame_range[1], max_end)

    #bake
    lastframe = bpy.context.scene.frame_float
    sample = min_start
    count=0
    while sample < max_end:
      self.set_frame(sample)
      sample += self._config._keyframeSampleRate
      for ob in ob_samples.keys():
        self.export_tracks_frame(ob, ob_samples[ob])
      count += 1
    self.set_frame(lastframe)

    #write
    for ob in ob_samples.keys():
      self.writeNodeID(self._node_ids[ob])
      self.writeUInt32(len(ob_samples[ob].keys()))
      for act in ob_samples[ob]:
        self.startBlock(ob.name + "." + act.name + ".keyframes")
        self.writeString(act.name)
        self.writeFloat(ob_samples[ob][act].kmin)
        self.writeFloat(ob_samples[ob][act].kmax)        
        self.writeUInt32(ob_samples[ob][act].count)
        self.writeData(ob_samples[ob][act].pos)
        self.writeData(ob_samples[ob][act].rot)
        self.writeData(ob_samples[ob][act].scl)
        self.endBlock()
    self._t_animation = millis() - t;
  def export_tracks_frame(self, ob : bpy.types.Object, act_samples : dict):
    for nla in ob.animation_data.nla_tracks:
      #nla.select = True
      for strip in nla.strips:
        samps = act_samples[strip.action]
        self.export_keyframes(ob, samps)
        if is_armature(ob):
          for pose_bone in ob.pose.bones:
            self.export_keyframes(pose_bone, samps)
  def set_frame(self, sample):
    #frame_set is slowest this method comes first
    bpy.context.scene.frame_set(int(sample), subframe = math.fmod(sample, 1))
    bpy.context.view_layer.update()
  def export_keyframes(self, ob, samps : KeySamples):
    #dbg_count=0
    #v ob.matrix_local
    #parentMatrix = Matrix.Identity(4)
    #curParent = ob.parent
    #kf_mat = ob.matrix_channel.to_3x3().to_4x4()
    #if curParent != None:
    #  kf_mat = curParent.matrix_channel.to_3x3().to_4x4().inverted() * kf_mat  #effectively removes translation component
    if is_pose_bone(ob):
      kf_mat = ob.matrix_channel 
    else:
      kf_mat = ob.matrix_local 
    
    vpos, vrot, vscl = self.glMat4(kf_mat).decompose()
    packVec3(samps.pos, self.glVec3(vpos))
    packVec4(samps.rot, self.glQuat(vrot))
    packVec3(samps.scl, self.glVec3(vscl))
    samps.count += 1
  def export_meshes(self):
    t = millis()
    #voff = noff = xoff = toff = 0
    offs = dict.fromkeys({'v','n','x','t'},0)
    for mesh_dat in bpy.data.meshes:
      self.export_mesh(mesh_dat, offs)
    self._t_meshes = millis() - t;
  def export_mesh(self, mesh_dat, offs):
    #self.appendLine("mesh " + mesh_dat.name)
    self.writeString(mesh_dat.name);
    self.writeNodeID(self._mesh_ids[mesh_dat])

    self.begin_mesh(mesh_dat)

    vcount = 0
    ncount = 0
    tcount = 0
    xcount = 0

    bHasTCoords = False
    
    # Vert data
    #https://blender.stackexchange.com/questions/26116/script-access-to-tangent-and-bitangent-per-face-how
    #https://docs.blender.org/api/current/bpy.types.MeshLoop.html
    mesh_dat.calc_tangents() # ** Requires a uv map

    vert_dict = {}
    norm_dict = {}
    tan_dict = {}
    uv_dict = {}
    bEmitted = False
    normal_face_mapping = [0] * len(mesh_dat.loops)
    vert_face_mapping = [0] * len(mesh_dat.loops)
    tangent_face_mapping = [0] * len(mesh_dat.loops)
    uv_face_mapping = [None] * len(mesh_dat.polygons) #store a map of each face index to the given tex coord indexes ordered by vertex index
    tangents = bytearray()
    verts = bytearray()
    norms = bytearray()
    tcoords = bytearray()
    for f in mesh_dat.polygons:
      uv_face_mapping[f.index] = []
      for iInd, loop_index in enumerate(f.loop_indices):
        loop = mesh_dat.loops[loop_index]
        #VERT
        vert = mesh_dat.vertices[f.vertices[iInd]].co
        key = self.veckey3d(vert)
        if vert_dict.get(key) is None:
          vert_dict[key] = len(vert_dict)
          packVec3(verts, self.glVec3(vert))
        vert_face_mapping[loop_index] = vert_dict.get(key)
        #NORMAL
        v_or_f_norm = None
        if f.use_smooth == True:
          v_or_f_norm = mesh_dat.vertices[f.vertices[iInd]].normal
        else:
          v_or_f_norm = f.normal # this is the split normal
        norm_key = self.veckey3d(v_or_f_norm)#create key for dict
        if norm_dict.get(norm_key) is None:
          norm_dict[norm_key] = len(norm_dict)
          packVec3(norms, self.glVec3(v_or_f_norm))
        normal_face_mapping[loop_index] = norm_dict.get(norm_key)
        #TANGENT
        tan = loop.tangent #we also need loop.bitangent or loop . normal
        key = self.veckey3d(tan)
        if tan_dict.get(key) is None:
          tan_dict[key] = len(tan_dict)
          packVec3(tangents,self.glVec3(tan))
        tangent_face_mapping[loop_index] = tan_dict.get(key)      
        # UVS
        if mesh_dat.uv_layers is not None:
          if mesh_dat.uv_layers.active is not None:
            if mesh_dat.uv_layers.active.data is not None:
              bHasTCoords = True
              cur_uv = mesh_dat.uv_layers.active.data[loop_index].uv
              key = mesh_dat.loops[loop_index].vertex_index, self.veckey2d(mesh_dat.uv_layers.active.data[loop_index].uv)
              if uv_dict.get(key) is None:
                packVec2(tcoords, cur_uv)
                uv_dict[key] = len(uv_dict)
              uv_face_mapping[f.index].append(uv_dict.get(key))
            else:       
              self.logError(mesh_dat.name + ": Active UV layer had no data");                  
          else:       
            if bEmitted == False:
              self.logError(mesh_dat.name + ": One Or More Faces (or All) did not have any ACTIVE UV layers");                   
              bEmitted = True
        else:       
          self.logError(mesh_dat.name + ": Model did not have any UV layers");      

    vcount += len(vert_dict)
    ncount += len(norm_dict)
    tcount += len(tan_dict)
    xcount += len(uv_dict)

    self.writeUInt32(len(vert_dict))
    self.startBlock(mesh_dat.name+".verts")
    self.writeData(verts)
    self.endBlock()
    self.writeUInt32(len(norm_dict))
    self.startBlock(mesh_dat.name + ".norms")
    self.writeData(norms)
    self.endBlock()
    self.writeUInt32(len(tan_dict))
    self.startBlock(mesh_dat.name + ".tangents")
    self.writeData(tangents)
    self.endBlock()
    self.writeUInt32(len(uv_dict))
    self.startBlock(mesh_dat.name + ".tcoords")
    self.writeData(tcoords)
    self.endBlock()

    del vert_dict
    del norm_dict
    del tan_dict
    del uv_dict
    
    mesh_dat.free_tangents()

    #FACES
    self.writeUInt32(vcount)
    self.writeUInt32(ncount)
    self.writeUInt32(tcount)
    self.writeUInt32(xcount)
    self.writeUInt32(len(mesh_dat.polygons))

    self.startBlock(mesh_dat.name + ".faces")
    for f in mesh_dat.polygons:   
      self.export_face(f, offs, uv_face_mapping, vert_face_mapping, normal_face_mapping, tangent_face_mapping, bHasTCoords)
    self.endBlock()
    
    offs['v'] += vcount
    offs['n'] += ncount
    offs['t'] += tcount
    offs['x'] += xcount

    self.end_mesh(mesh_dat)
  def begin_mesh(self, ob_mesh):
    #triangulate the mesh, and save temp copy of old mesh
    self._bm_new_tmp = bmesh.new()
    self._bm_old_tmp = bmesh.new()
    self._bm_new_tmp.from_mesh(ob_mesh)
    self._bm_old_tmp.from_mesh(ob_mesh)
    bmesh.ops.triangulate(self._bm_new_tmp, faces=self._bm_new_tmp.faces[:])#, quad_method=0, ngon_method=0
    #redo normals
    #bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    #bmesh.ops.recalc_face_normals(bm_new, faces=bm_new.faces[:])
    #bm_new.normal_update()
    self._bm_new_tmp.to_mesh(ob_mesh)
    self._bm_new_tmp.free()
  def end_mesh(self, ob_mesh):
    self._bm_old_tmp.to_mesh(ob_mesh)
    self._bm_old_tmp.free()  
  def export_face(self, f, offs, uv_face_mapping, vert_face_mapping, normal_face_mapping, tangent_face_mapping, bHasTCoords):
    #smooth group
    #self.writeByte(f.use_smooth == True : 1 : 0)
    for iInd, loop_index in enumerate(f.loop_indices):
      v_ind = vert_face_mapping[loop_index] + offs['v']
      n_ind = normal_face_mapping[loop_index] + offs['n']
      t_ind = tangent_face_mapping[loop_index] + offs['t']
      self.writeUInt32(v_ind)
      self.writeUInt32(n_ind)
      self.writeUInt32(t_ind)
      
      if bHasTCoords == True:
        t_ind = uv_face_mapping[f.index][iInd] + offs['x']
        self.writeUInt32(t_ind)
  def export_armatures(self):
    self.writeUInt32(len(bpy.data.armatures))
    for arm in bpy.data.armatures:
      for bone in arm.bones:
        self.writeBoneID(self._arm_bone_ids[arm][bone])
        self.writeString(bone.name)
        self.writeMatrixPRS(bone.matrix_local)
  def export_skins(self):
    t = millis()
    self.writeUInt32(self.skins_count())
    for ob in bpy.data.objects:
      if ob.type == 'MESH':
        self.export_skin(ob)
    self._t_skins = millis() - t;        
  def get_mesh_armatures(self, ob_mesh):
    arms = []
    for mod in ob_mesh.modifiers:
      if mod.type == 'ARMATURE':
        if mod.object == None:
          self.logWarn("Armature with no object found on mesh, no weight applied " + self.getObjectName(ob_mesh, None))
        else:
          arms.append(mod.object.data)
    return arms
  def skins_count(self):
    count=0
    for ob in bpy.data.objects:
      if ob.type == 'MESH':
        if len(self.get_mesh_armatures(ob)) > 0:
          count +=1
    return count
  def export_skin(self, ob_mesh):
    #vert, [count, boneid, weight, count, boneid, weight]
    #optimized for wd_in_st and jw_in_st

    arms = self.get_mesh_armatures(ob_mesh)
    if len(arms) > 1:
      self.logWarn("More than one armature on mesh " + ob_mesh.name)

    self.writeUInt16(len(arms))
    self.startBlock(ob_mesh.name + ".skins")
    for arm in arms:
      for iVert in ob_mesh.data.vertices:
        for v_group in iVert.groups:
          vgroup = ob_mesh.vertex_groups[v_group.group]
          arr = bytearray()
          count = 0
          if vgroup.name in self._arm_bone_name_id[arm].keys():
            boneid = self._arm_bone_ids[arm][self._arm_bone_name_id[arm][vgroup.name]]
            packBoneIDu16(arr, boneid)
            packFloat(arr, v_group.weight)
            count+=1
          self.writeUInt16(count)
          self.writeData(arr)
    self.endBlock()
  def export_materials(self):
    t = millis()
    self.writeUInt32(len(bpy.data.materials))
    for mat in bpy.data.materials:
      self.export_material(mat)
    self._t_materials = millis() - t;    
    #https://blender.stackexchange.com/questions/160042/principled-bsdf-via-python-api
  def export_material(self, mat):
    self.writeNodeID(self._mat_ids[mat])
    #export PBR inputs or default shader
    if mat.node_tree != None:
      #https://docs.blender.org/api/current/bpy.types.ShaderNodeBsdfPrincipled.html
      bsdf = mat.node_tree.nodes.get("Principled BSDF") 
      if bsdf != None:
        self.writeBool(True)
        self.export_shader_node_color(bsdf.inputs['Base Color'])
        self.export_shader_node_factor(bsdf.inputs['Metallic']) 
        self.export_shader_node_factor(bsdf.inputs['Specular'])
        self.export_shader_node_factor(bsdf.inputs['Roughness'])
        self.export_shader_node_factor(bsdf.inputs['Alpha']) 
        self.export_shader_node_vector(bsdf.inputs['Normal'])
      else:
        self.logError("Material " + mat.name + " did not have principled bsdf")
        self.writeBool(False)
        self.writeVec3(mat.specular_color)
        self.writeFloat(mat.specular_intensity)
        self.writeBool(mat.use_backface_culling)
        self.writeFloat(mat.roughness)
        self.writeFloat(mat.metallic)
        self.writeVec4(mat.diffuse_color)        

    if mat.blend_method == 'OPAQUE':
      self.writeBool(False)
    elif mat.blend_method == 'BLEND':
      self.writeBool(True)
  def export_shader_node_color(self, node):
    #[color][img id] NodeSocketColor
    assert(type(node) is bpy.types.NodeSocketColor)
    img = self.get_shader_node_image_input(node)
    self.writeVec4(node.default_value)
    if img != None:
      self.writeNodeID(self._image_ids[img])
    else:
      self.writeNodeID(0)
  def export_shader_node_vector(self, node):
    #[color][img id] 
    assert(type(node) is bpy.types.NodeSocketVector)
    img = self.get_shader_node_image_input(node)
    self.writeVec3(node.default_value)
    if img != None:
      self.writeNodeID(self._image_ids[img])
    else:
      self.writeNodeID(0)      
  def export_shader_node_factor(self, node):
    assert(type(node) is bpy.types.NodeSocketFloatFactor)
    #[factor][img id] 
    img = img = self.get_shader_node_image_input(node)
    self.writeFloat(node.default_value)
    if img != None:
      self.writeNodeID(self._image_ids[img])
    else:
      self.writeNodeID(0)
  def get_shader_node_image_input(self, node):
    img = None
    for input in node.links:
      if type(input.from_node) is bpy.types.ShaderNodeTexImage: # .type== 'TEX_IMAGE'
        img = input.from_node.image
        self._material_images[img] = self._image_ids[img]
    return img
  def export_images(self):
    t = millis()
    for img in bpy.data.images:
      self.export_image(img)
    self._t_images = millis() - t; 
  def export_image(self, img):
    if img.source == 'FILE':
      img.update()
      self.startBlock(img.name)
      self.writeString(img.name)
      self.writeInt32(img.size[0])
      self.writeInt32(img.size[1])

      if img.size == None or img.size[0] == 0:
        self.logError("Image "+img.name+" failed to load.")
        return
      
      #indexing pixels is very slow
      self.writeData(struct.pack(str(len(img.pixels))+'f', *img.pixels))
      self.endBlock()

  ##############################################################################
  # Debug
  #
  def debugDumpMatrix(self, str, in_matrix):
    #return ""
    strDebug = ""
    loc, rot, sca = in_matrix.decompose()
    strDebug += "\n\n"
    strDebug += "#" + str + " mat\n" + self.matToString(in_matrix.to_4x4(), ",", True) + "\n"
    strDebug += "#  loc     (" + self.vec3ToString(loc) + ")\n"
    strDebug += "#  quat    (" + self.vec4ToString(rot) + ")\n"

    strDebug += "#gl_quat:  (" + self.vec4ToString(self.glQuat(rot)) + ")\n"
    strDebug += "#to_euler_deg: (" + self.vec3ToString(euler3ToDeg(rot.to_euler("XYZ"))) + ")\n" 
    strDebug += "#gl_euler_deg: (" + self.vec3ToString(euler3ToDeg(self.glEuler3(rot.to_euler()))) + ")\n" 
    
    strDebug += "#AxAng(Blender) " + self.vec3ToString(self.glVec3(in_matrix.to_quaternion().axis)) 
    strDebug += "," + self.fPrec() % ((180.0)*in_matrix.to_quaternion().angle/3.14159)
    strDebug += "\n"
    
    strDebug += "#Ax,Ang(conv)   " + self.vec3ToString(self.glVec3(self.glMat4(in_matrix).to_quaternion().axis)) 
    strDebug += "," + self.fPrec() % ((180.0)*self.glMat4(in_matrix).to_quaternion().angle/3.14159)
    strDebug += "\n"     
        
    return strDebug


  ##############################################################################
  # Old








  # Okay, for now we're going to set the bind pose to be Keyframe 1
  def getVertexWeightsForMesh(self, ob_mesh):
    strFile = ""
    #https://docs.blender.org/api/blender_python_api_2_77_0/bpy.types.Modifier.html
    strFile += self.getVertexWeightsForMeshArm(ob_mesh)
    return strFile
  def exportAllActionNames(self):
    strActions = "\n"
    strActions += "#All Actions, tells us what actions are in this file without ctrl+f\n"
    for ob in bpy.data.objects:
      #select the object
      bpy.context.scene.objects.active = ob
      bpy.data.objects[ob.name].select = True   
       
      if ob.type == 'ARMATURE' or ob.type == 'MESH':
        if ob.animation_data != None:
          for nla in ob.animation_data.nla_tracks:
            nla.select = True
            for strip in nla.strips:
              iMinKeyf = self.getMinKeyframeForAction(strip.action)
              iMaxKeyf = self.getMaxKeyframeForAction(strip.action)
              strActions += "action "+str(ob.type)+" \"" + self.getObjectName(strip.action, None) + "\" " +str(iMinKeyf)+ " " +str(iMaxKeyf)+ "\n"
            
    return strActions    
  def exportAllObjectNames(self):
    strObjects = "\n"
    strObjects += "#All Objects\n"
    for ob in bpy.data.objects:
      #select the object
      bpy.context.scene.objects.active = ob
      bpy.data.objects[ob.name].select = True   
       
      if ob.type == 'ARMATURE' or ob.type == 'MESH':
        strObjects += "object " + str(ob.type) + " \"" + self.getObjectName(ob, None) + "\" " + str(ob.is_visible(bpy.context.scene)) + "\n"
            
    return strObjects
  def getArmatureId(self, ob_arm):
    id = 0
    for ob in bpy.data.objects:
      if ob.type == 'ARMATURE':
        if ob == ob_arm:
          return id;
        id += 1
    return id
  def getParentInverse(self, ob):
    #the reason for this is that blener has inverse parent mat data,e ven when/
    #ob has no parent - thus the data is invalid.  So return the identity matrix
    parentMatrix = Matrix.Identity(4)
    if ob != None:
      if ob.parent != None:
        parentMatrix = ob.matrix_parent_inverse
    return parentMatrix
  def exportAllArmatures(self):
    strFile = ""
    for ob in bpy.data.objects:
      #select the object
      bpy.context.scene.objects.active = ob
      bpy.data.objects[ob.name].select = True   
      
      if not ob:
        continue
      elif ob.type == 'ARMATURE':
        #Export the armature bone relationships.
        #bone "name" index "parent" vbeg vend bpoi bpjr
        # Bone Hierarchy
        if self.isValidArmature(ob):
        
          strParent = self.getParentString(ob)

          strFile += "\n"
          strFile += "arm_beg "+"\"" + self.getObjectName(ob, None) + "\" " + strParent +" " + str(self.getArmatureId(ob)) + "\n"
          bpy.ops.object.mode_set(mode='OBJECT')
          strFile += "arm_world " + self.matToString(self.glMat4(ob.matrix_world)) + "\n"
          strFile += "arm_parent_inverse " + self.matToString(self.glMat4(self.getParentInverse(ob))) + "\n"
          
          #Keyframe must be bind pose, or else head/tail will be wrong  For this we set the first key frame to a very far away
          #https://blender.stackexchange.com/questions/15170/blender-python-exporting-bone-matrices-for-animation-relative-to-parent
          bpy.ops.object.mode_set(mode='EDIT') #Must be in edit mode for bind pose
          bpy.data.objects[ob.name].select = True 
          strFile += self.getAllBones(ob, ob.pose.bones) #"hc " + self.getBoneHierarchyString(ob_arm.pose.bones) + "\n"
          #From gran 6
          #strFile += self.getAllBoneBind(ob, ob.data.bones) #"hc " + self.getBoneHierarchyString(ob_arm.pose.bones) + "\n"

          strFile += "arm_end \"" + self.getObjectName(ob, None) + "\"\n\n"
      
    return strFile
  def isValidArmature(self, ob_arm):
    #Arm is bound to meshes
    ch = [child for child in ob_arm.children if child.type == 'MESH' and child.find_armature()]
    if len(ch) == 0:
      self.logError("Armature " + ob_arm.name + " has no bound meshes ");
      return False
    #Arm has more than 1 bone
    if len(ob_arm.data.bones) == 0:
      self.logError("Armature " + ob_arm.name + " has no bones ");
      return False
    
    return True
  def exportAllKeyframes(self):
    strFile = ""
    for ob in bpy.data.objects:
      if ob.hide == False:
        #select the object
        bpy.context.scene.objects.active = ob
        bpy.data.objects[ob.name].select = True   

        #Export everything for the object.
        #Avoid trying to export random ass animations.
        #strFile += "#KF [index] M44 [16 values] Curve xy, Curve mode, easing, left bezier handle xy, type, right bezier handle xy, type\n"
        if ob.type == 'ARMATURE' or ob.type == 'MESH':
          bpy.ops.object.mode_set(mode='OBJECT')
          bpy.data.objects[ob.name].select = True 
          strFile += self.exportActionsForObject(ob)
      else:
        strFile += self.logWarn("Object was hidden, it won't get exported: " + self.getObjectName(ob, None))
    return strFile
  def getParentString(self, ob):
    strParent = ""
    strParentType = "NONE"
    if ob.parent != None:
      strParentType = str(ob.parent_type)
      if ob.parent_type == "BONE":
        bone = ob.parent.data.bones[ob.parent_bone]
        strParent = self.getObjectName(ob.parent, bone)
      else:
        strParent = self.getObjectName(ob.parent, None)
    return "\"" + strParent + "\" \"" + strParentType + "\""
  #Export .MOD file
  def getMeshPartMODFileDescBeg(self, ob):
    #this is the MPT mod file part in the OBJ fie.
    strFile = ""
  
    #Mesh Parts
    #if ob.name.endswith(self.getContactBoxNameSuffix()):
    #  #Contact Box - Export the min/max of the meshbox.   
    #  strFile += "cbx " + self.strModelName + "." + self.getObjectName(ob, None) + " " + self.getBoxString(ob.data) + "\n\n"
    #elif ob.name.endswith(self.getPhysicsVolumeNameSuffix()):
    #  #Physics Volume - Export the min/max of the box.       
    #  strFile += "phyv " + self.strModelName + "." + self.getObjectName(ob, None) + " " + self.getBoxString(ob.data) + "\n\n"
    #else:
    #Regular Mesh
    strParent = self.getParentString(ob)

    #Part Name
    strFile += "mpt_beg \"" + self.getObjectName(ob, None) + "\" " + strParent + "\n"
    strFile += "mpt_hide_render " + str(ob.hide_render) + "\n"
          
    if ob.rigid_body != None:
      msg("Object has rigid body");
      strFile += "# collision shape, kinematic (t/f - if it follows an armature) dynamic (t/f - if it RESPONDS physically, otherwise it just regiters collisions) \n"
      strFile += "physics_shape \"" + str(ob.rigid_body.collision_shape) + "\" " + str(ob.rigid_body.kinematic) + " " + str(ob.rigid_body.enabled) + "\n"
    
    #Matrices
    
    cur_mode = bpy.context.object.mode
    bpy.ops.object.mode_set(mode='EDIT')
    strFile += "#mpt_world " + self.matToString(self.glMat4(ob.matrix_world)) + "\n"
    strFile += "#mpt_local " + self.matToString(self.glMat4(ob.matrix_local)) + "\n"
    strFile += "mpt_basis " + self.matToString(self.glMat4(ob.matrix_basis)) + "\n"
    strFile += "#location " + self.vec3ToString(self.glVec3(ob.location)) + "\n"
    strFile += "mpt_parent_inverse " + self.matToString(self.glMat4(self.getParentInverse(ob))) + "\n"
    bpy.ops.object.mode_set(mode=cur_mode)
    #strFile += "mpt_local " + self.matToString(self.glMat4(ob.matrix_local)) + "\n"
    #strFile += "mpt_basis " + self.matToString(self.glMat4(ob.matrix_basis)) + "\n"
    
    #Export Material
    strFile += self.printMaterialForMpt(ob)

    return strFile
  def printMaterialForMpt(self, ob):
    strFile = ""
    if ob.material_slots != None:
      for mat_slot in ob.material_slots:
        bDiffuseTex = False
        if mat_slot != None:
          if mat_slot.material != None:
            strFile += "mat_beg \"" + self.getObjectName(ob, None) + "\"\n";
            for mtex_slot in mat_slot.material.texture_slots:
              if mtex_slot != None:
                if mtex_slot.texture != None:
                  if hasattr(mtex_slot.texture , 'image'):
                    if mtex_slot.texture.image != None:
                      img = mtex_slot.texture.image
                      if img.file_format == "PNG" or img.file_format == "TARGA" or img.file_format == "TARGA_RAW":
                        #fileName = os.path.splitext(os.path.basename(img.filepath))[0]
                        #fileExt = os.path.splitext(os.path.basename(img.filepath))[1]
                        filename = os.path.basename(img.filepath)
                        #These values are taken from the INFLUENCE dropdown on the texture panel of the object
                        if len(os.path.basename(img.filepath)) > 0:
                          if mtex_slot.use_map_color_diffuse:
                            #Note: use_map_alpha doesn't matter.  we enable alpha if 'Transparency' is enabled on the model.
                            strFile += "mat_tex_diffuse \"" + filename + "\" " + str(mtex_slot.diffuse_color_factor) + " " + str(mtex_slot.alpha_factor) + "\n"
                            bDiffuseTex = True
                          if mtex_slot.use_map_normal:
                            strFile += "mat_tex_normal \"" + filename + "\" " + str(mtex_slot.normal_factor) + "\n"
                        if self.blnSaveTextures == True:
                          self.saveImagePng(img)
                          strFile += "#saved to " + os.path.join(self.getMobFullPathWithoutFilename() , os.path.basename(img.filepath)) + "\n"
                      else:
                        strErr = "#File Format " + img.file_format + " not supported, or image not found.\n"
                        msg(strErr)
                        strFile+= strErr
        
            #Uif diffuse texture isn't supplied we need to render in the color.
            strFile += "mat_diffuse " + self.color3ToString(mat_slot.material.diffuse_color) + " " + str(mat_slot.material.diffuse_intensity) +"\n"                    
            strFile += "mat_spec " + self.color3ToString(mat_slot.material.specular_color) + " " + str(mat_slot.material.specular_intensity) + " " + str(mat_slot.material.specular_hardness) + "\n"
            if mat_slot.material.raytrace_mirror.use:
              strFile += "mat_mirror " + self.color3ToString(mat_slot.material.mirror_color) + " " + str(mat_slot.material.raytrace_mirror.reflect_factor) + "\n"
            #So - to support transparency - Under "Transparency" of the material
            #Alpha - used, IOR - used, Filter - used (for glass color)
            #the followign must be set to equal blender
            #Fresnel - 0.0, Falloff - 1.0, Limit - 0.0, Depth - 1, Amount - 1.0
            if mat_slot.material.use_transparency:
              strFile += "mat_transparency " + str(mat_slot.material.alpha) + " " + str(mat_slot.material.raytrace_transparency.ior) +  " " + str(mat_slot.material.raytrace_transparency.filter) + "\n"
                
            strFile += "mat_end \"" + self.getObjectName(ob, None) + "\"\n";  
      return strFile
  def saveImagePng(self, img):
    #image = bpy.data.images.new("Test", alpha=True, width=16, height=16)
    #image.use_alpha = True
    #image.alpha_mode = 'STRAIGHT'
    tmp1 = img.filepath_raw
    filename = os.path.join(self.getMobFullPathWithoutFilename() , os.path.basename(img.filepath))
    msg("Full image path: " + filename)
    img.filepath_raw = filename
    
    tmp2 = img.file_format 
    img.file_format = 'PNG'
    try:
      img.save()
    except:
      msg("Failed to save image " + filename)
    
    img.filepath_raw = tmp1
    img.file_format = tmp2
    return
  def getMeshPartMODFileDescEnd(self, ob):
    #this is the MPT mod file part in the OBJ fie.
    strFile = ""
    strFile += "mpt_end \"" + self._exportName + "." + self.getObjectName(ob, None) + "\"\n"
    return strFile;
  def exportFace(self, f, iMeshVertexOffset, iMeshNormalOffset, iMeshTCoordOffset, uv_face_mapping, normal_face_mapping, bHasTCoords):
    strFile = ""
    #create list of indices first. if we have quads then we have to triangulate.
    indices = []
    for iInd, loop_index in enumerate(f.loop_indices):
      #indexes index ALL VERTS IN THE FILE BEFORE.  so we have to add an offset to them.
      v_ind = f.vertices[iInd] + iMeshVertexOffset + 1  #All OBJ files start at index 1
      n_ind = normal_face_mapping[loop_index] + iMeshNormalOffset + 1
      
      if bHasTCoords == True:
        t_ind = uv_face_mapping[f.index][iInd] + iMeshTCoordOffset + 1
        strIndex = "%d" % (v_ind) + "/" + "%d" % (t_ind)  + "/" + "%d" % (n_ind)
      else:
        strIndex = "%d" % (v_ind) + "//" + "%d" % (n_ind)
      indices.append(strIndex)

    #2017 12 28 now that we triangulate the mesh, we shouldn't be doing quads
    if len(indices) == 3:
      #acording to doc loop_indices is equivalent to range(p.loop_start, p.loop_start + p.loop_total):
      ind0 = indices[0]
      ind1 = indices[1]
      ind2 = indices[2]
      
      strFile += "f "
      strFile += ind0 + " "
      strFile += ind1 + " "
      strFile += ind2 + "\n"          
    else:
      raise Exception('Error - Too many indices "' + str(len(indices)) + '" for face vertex in mesh "'+ob_mesh.name+'". Consider triangulating mesh (edit mode, select faces, CTRL+T).')
    return strFile
  def veckey2d(self, v):
    return round(v[0], 4), round(v[1], 4)
  def veckey3d(self, v):
    return round(v.x, 4), round(v.y, 4), round(v.z, 4)

  #Get the list of child bones form the given bone oject
  #  obj - the given Scene object
  #  parentBone - the given parent bone.
  #  NOTE: blender doesn't store parent/child bones automaticalyl.  cdum
  def getBoneChildren(self, parentBone, boneList):
    #obj = self.id_data
    children = []
    for iBone in boneList:
      if not iBone or not parentBone or not iBone.parent:
        continue
      if iBone.parent.name == parentBone.name:
        children.append(iBone)
    
    return children
  def getAllBones(self, ob, boneList):
    strBones = ""
    strBones += "bones_beg\n"
    for pose_bone in boneList:
      strBones += self.getBoneString(ob, pose_bone, boneList)
    strBones += "bones_end\n"
    return strBones
  def getBoneString(self, ob_arm, in_pose_bone, boneList):
    ################################################################
    #from the .x exportrer
    ## BoneMatrix transforms mesh vertices into the
    ## space of the bone.
    ## Here are the final transformations in order:
    ##  - Object Space to World Space
    ##  - World Space to Armature Space
    ##  - Armature Space to Bone Space
    ## This way, when BoneMatrix is transformed by the bone's
    ## Frame matrix, the vertices will be in their final world
    ## position.
    #
    #self.BoneMatrix = ArmatureObject.data.bones[BoneName] \
    #  .matrix_local.inverted()
    #self.BoneMatrix *= ArmatureObject.matrix_world.inverted()
    #self.BoneMatrix *= BlenderObject.matrix_world
    ################################################################


    strBone = "bone_beg " +  "\"" + self.getObjectName(ob_arm, in_pose_bone) + "\" " + str(self.getBoneId(in_pose_bone.name, boneList)) + " "
    if not in_pose_bone.parent:
      strBone += "\"\" \"NONE\" \n" # No parent, root
    else:
      strBone += "\"" + self.getObjectName(ob_arm, in_pose_bone.parent) + "\" \"NONE\" \n"
    
    strBone += "bone_head " + self.vec3ToString(self.glVec3(in_pose_bone.head)) + "\n"
    strBone += "bone_tail " + self.vec3ToString(self.glVec3(in_pose_bone.tail)) + "\n"
    
    # bpy.ops.object.mode_set(mode='EDIT')
    #editBoneList = ob_arm.data.edit_bones
    # strFile += "# INVERSE BIND POSE MATRICES INCOMING (BIND_POSES)\n"
    # strFile += "bp_beg " + str(len(editBoneList)) + " \n"   
    
    #bindworld = self.matrix_world(ob_arm, in_bone.name)
    #bindworld = in_pose_bone.bone.matrix_local
    #if in_pose_bone.parent:
    #  bindworld = in_pose_bone.bone.parent.matrix_local.inverted() * in_pose_bone.bone.matrix_local
      
    #else:
    #  #no parent, try armature i guess
    #  bindworld = ob_arm.matrix_local.inverted() * in_pose_bone.bone.matrix_local
    #strBone += "#debug: basis mat before gl conv = " + str(in_bone.matrix_basis) + "\n"
    #strBone += "#debug: basis mat before gl conv = " + self.matToString(bindworld) + "\n"
    #https://blender.stackexchange.com/questions/44637/how-can-i-manually-calculate-bpy-types-posebone-matrix-using-blenders-python-ap
    
    #strBone += "bone_bind_world " +  "m44 " + self.getKeyframeDataMat4(bindworld) + "\n";
    #strBone += "bone_bind_world_inv " +  "m44 " + self.getKeyframeDataMat4(in_pose_bone.bone.matrix_local.copy().inverted()) + "\n";
    #DEBUG
    #self.dump(in_pose_bone)
    #self.dump(ob_arm)
    #2017 12 16 - this made no sense.  why not just use the bind?
    # 2AM - because  blender bind mat makes no sense, it's all wrong basis
    for iBone in boneList:  
      if iBone.parent == None:
        strBone += self.getPoseBoneBindMatrices(iBone, Vector([0,0,0]), boneList, in_pose_bone.name) 
    
    strBone += "bone_end " +  "\"" + self.getObjectName(ob_arm, in_pose_bone) + "\"\n"
    
    
    return strBone 
  #def getNumAnimations(self):
  #  return self.iMaxAnimations
  def getBoneId(self, strName, objBoneList):
    #Note: this could prove problematic in the future.
    #Assuming the ordinal position of the bone is it's bone ID referenced in the
    #Group in the Vertex Weight

    i=0
    for bone in objBoneList:
      if bone.name == strName:
        return i
      i+=1
    #-1 is an allowed value for some vgroups if you have multiple armatures on a single mesh
    return -1
  def exportActionsForObject(self, ob):
    #New export which exports actions
    strFile = ""

    #AnimData (struct)
    #https://docs.blender.org/api/blender_python_api_2_62_release/bpy.types.AnimData.html
    if ob.animation_data == None:
      strFile += "#" + self.getObjectName(ob, None) + " - No animation data\n"
      return strFile

    strFile += "#" + self.getObjectName(ob, None) + " has animation data " + str(len(ob.animation_data.nla_tracks)) + " NLA Tracks\n"
    for nla in ob.animation_data.nla_tracks:
      nla.select = True
      strFile += "# NLA STrips: " + str(len(nla.strips)) + "\n"
      for strip in nla.strips:
        curAction = strip.action
        strFile += "#Action Found : " + curAction.name + "\n"
        #print name
        strFile += "\n"
        
        #keyrames
        keyframes = []

        iMinKeyf = self.getMinKeyframeForAction(curAction)
        iMaxKeyf = self.getMaxKeyframeForAction(curAction)
        
        #dump keyframes to file.
        if iMinKeyf < iMaxKeyf == 0:
          strFile += "#Animation had no keyframes.\n"
        else:
          strFile += self.printKeyframeBlockForSelectedObject(ob, iMinKeyf, iMaxKeyf, curAction)

    return strFile 
  def getMinKeyframeForAction(self, curAction):
    iRet = 9999999
    for fcu in curAction.fcurves:
      for keyf in fcu.keyframe_points:
        x, y = keyf.co
        if x < iRet:
          iRet = x
    return int(iRet)
  def getMaxKeyframeForAction(self, curAction):    
    iRet = -9999999
    for fcu in curAction.fcurves:
      for keyf in fcu.keyframe_points:
        x, y = keyf.co
        if x > iRet:
          iRet = x
    return int(iRet)  
  def getActionKeyframeDataForKeyframeIndex(self, pAction, iIndex):
    for fcu in pAction.fcurves:
      #strFile += "DEBUG: data_path '" + str(fcu.data_path) +
      #"' channel '" + str(fcu.array_index) + "' driver '" + str(fcu.driver) + "'\n"
      for keyf in fcu.keyframe_points:
        #x, y = keyf.co
        x, y = keyf.co
        ind = math.ceil(x)
        if iIndex == ind:
          return keyf
          
    return None
  #get the keyframe string for selected keyframe
  def printKeyframeBlockForSelectedObject(self, ob, iMinKey, iMaxKey, curAction):
    strKeyframe = ""
    if ob.type == 'ARMATURE':
      for pose_bone in ob.pose.bones:
        strKeyframe += "act_beg \"" + self.getObjectName(curAction, None) + "\" \"" + self.getObjectName(ob, pose_bone) + "\"\n"
        strKeyframe += self.printKeyframeBlockForPoseBone(ob, pose_bone, iMinKey, iMaxKey, curAction)
        strKeyframe += "act_end \"" + self.getObjectName(curAction, None) + "\" \"" + self.getObjectName(ob, pose_bone) + "\"\n\n"
    #Mesh only here, but obviously we can do this for ANY object (armature, etc)
    elif ob.type == 'MESH':
      strKeyframe += "act_beg \"" + self.getObjectName(curAction, None) + "\" \"" + self.getObjectName(ob, None) + "\"\n"    
      strKeyframe += self.printKeyframeBlockForObject(ob, iMinKey, iMaxKey, curAction)
      strKeyframe += "act_end \"" + self.getObjectName(curAction, None) + "\" \"" + self.getObjectName(ob, None) + "\"\n\n"
    return strKeyframe
  def printKeyframeBlockForObject(self, ob_mesh, iMinKey, iMaxKey, curAction):
    strKeyframe = ""
    #set the action.
    ob_mesh.animation_data.action = curAction
    #strKeyframe += "kf_beg \"" + self.getObjectName(ob_mesh, None) + "\" " + str(len(keyframes)) + "\n"
    iGrainKey = 0
    for iKeyFrame in range(iMinKey, iMaxKey+1):
      #this little block gets the final keyframe
      iGrainMax = self.intGranularity
      if iKeyFrame == iMaxKey:
        iGrainMax = 1
      for iGrain in range(0, iGrainMax):
        fGrain = float(iGrain) / float(iGrainMax)
        bpy.context.scene.frame_set(iKeyFrame, fGrain)  
        
        #*Matrix world here hopefully applies all parent transforms
        #20171228 - old object export
        #curMat = None
        #curMat = ob_mesh.matrix_world.to_4x4()
        
        #20171228 new relative export
        curMat = ob_mesh.matrix_basis.to_4x4();
        #if ob_mesh.parent != None:
        #  curMat = ob_mesh.parent.matrix_basis.to_4x4().inverted() * curMat  
        #curMat = ob_mesh.matrix_world.to_4x4()
        #if ob_mesh.parent != None:
        #  curMat = ob_mesh.parent.matrix_world.to_4x4().inverted() * curMat  
        


        #curMat = (ob_mesh.parent.matrix_local.inverted() * ob_mesh.matrix_local) * ob_mesh.matrix_basis
        #else:
          #curMat = ob_mesh.matrix_local * ob_mesh.matrix_basis
          
        #strKeyframe += self.debugDumpMatrix("curMat", curMat)
        #strKeyframe += self.debugDumpMatrix("ob_mesh.matrix_basis", ob_mesh.matrix_basis)
        #strKeyframe += self.debugDumpMatrix("ob_mesh.matrix_world.to_4x4()", ob_mesh.matrix_world.to_4x4())

        strKeyframe += self.printKeyframeMatrixForAction(iGrainKey, curMat, curAction)
        iGrainKey+=1
    #strKeyframe += "kf_end \"" + self.getObjectName(ob, None) + "\"\n"
    return strKeyframe
  def printKeyframeBlockForPoseBone(self, ob_arm, pose_bone, iMinKey, iMaxKey, curAction):
    strKeyframe = ""
    #set the action.
    
    ob_arm.animation_data.action = curAction
    iGrainKey = 0
    #strKeyframe += "kf_beg \"" + self.getObjectName(ob_arm, pose_bone) + "\" " + str(len(keyframes)) + "\n"
    #for iKeyFrame in range(bpy.context.scene.frame_start, bpy.context.scene.frame_end+1):
    for iKeyFrame in range(iMinKey, iMaxKey+1):
      #this little block gets the final keyframe
      iGrainMax = self.intGranularity 
      if iKeyFrame == iMaxKey:
        iGrainMax = 1    
      for iGrain in range(0, iGrainMax):
        fGrain = float(iGrain) / float(iGrainMax) 
        bpy.context.scene.frame_set(iKeyFrame, fGrain)

        # parentMatrix = Matrix.Identity(4)
        # #we werwe using the channel matrix, I think we want the actual final driver matrix.
        # #this is the exact code from https://blender.stackexchange.com/questions/15170/blender-python-exporting-bone-matrices-for-animation-relative-to-parent
        
        #20171218 debugging shows matrix-basis to contain the actual information we need.
        #This contains only rotation as far as I can tell
        #curMatrix = pose_bone.matrix_channel
        
        parentMatrix = Matrix.Identity(4)

        curParent = pose_bone.parent
        curMatrix = pose_bone.matrix_channel.to_3x3().to_4x4()
        if curParent != None:
          curMatrix = curParent.matrix_channel.to_3x3().to_4x4().inverted() * curMatrix  #effectively removes translation component

        
        #https://blender.stackexchange.com/questions/35125/what-is-matrix-basis
        #It's also worth mentioning it's role in other matrices: matrix_local =
        #  matrix_parent_inverse * matrix_basis, and matrix_world = parent.matrix_world * matrix_local. – misnomer Dec 10 '16 
        
        #parentMat = cbm = Matrix.Identity(4)
        #if pose_bone.parent != None:
        #  parentMat = pose_bone.parent.matrix.inverted() # * pose_bone.matrix  #effectively removes translation component
        #"chartest_noa.Punch" "chartest_noa.Armature.BoneShoulderR
        #strKeyframe += self.debugDumpMatrix("pose_bone.bone.matrix_local.to_4x4()", pose_bone.bone.matrix_local.to_4x4())
        #strKeyframe += self.debugDumpMatrix("pose_bone.bone.matrix", pose_bone.bone.matrix.to_4x4())
        #strKeyframe += self.debugDumpMatrix("pose_bone.matrix_basis", pose_bone.matrix_basis)
        #strKeyframe += self.debugDumpMatrix("pose_bone.matrix_channel", pose_bone.matrix_channel)
        #strKeyframe += self.debugDumpMatrix("pose_bone.matrix", pose_bone.matrix)
        
        #strKeyframe += self.debugDumpMatrix("pose_bone.parent.matrix.inverted() * pose_bone.matrix", (parentMat.inverted() * pose_bone.matrix) * pose_bone.bone.matrix_local.to_4x4());
        #strKeyframe += self.debugDumpMatrix("pose_bone.parent.matrix.inverted() * pose_bone.matrix_basis", parentMat.inverted() * pose_bone.matrix_basis);
        #strKeyframe += self.debugDumpMatrix("pose_bone.bone.matrix_local.to_4x4() * pose_bone.matrix_basis",  pose_bone.matrix_basis * pose_bone.bone.matrix_local.to_4x4());
        #It looks like matrix_local is changing the basis of matrix_bind.  Matrix_bind has the correct rotation.
        
        #strKeyframe += self.debugDumpMatrix("pose_bone.parent.matrix.inverted() * pose_bone.matrix", curMatrix);
        #strKeyframe += self.debugDumpMatrix("self.glMat4(pose_bone.parent.matrix.inverted() * pose_bone.matrix)", self.glMat4(curMatrix));
        
       #   curParent = pose_bone.parent
       #   curMatrix = pose_bone.matrix_channel.to_3x3().to_4x4()
       #   if curParent != None:
       #     curMatrix = curParent.matrix_channel.to_3x3().to_4x4().inverted() * curMatrix  #effectively removes translation component
        #skinMat = curMatrix 

        strKeyframe += self.printKeyframeMatrixForAction(iGrainKey, curMatrix, curAction);
        iGrainKey+=1
      
    #strKeyframe += "kf_end \"" + self.getObjectName(ob_arm, pose_bone) + "\"\n"
    return strKeyframe
  def getKeyframeData(self, in_matrix, iKeyFrame):
    strKeyframe = ""
    strFrame = ""
    if iKeyFrame >=0:
      strFrame = str(iKeyFrame) 
      
    #if self.blnPRSKeyframes == True:
    strKeyframe = "prs " + strFrame + " " + self.getKeyframeDataPRS(in_matrix);
    #else: 
    #  strKeyframe = "m44 " + strFrame + " " + self.getKeyframeDataMat4(in_matrix);
    
    return strKeyframe
  def getKeyframeDataMat4(self, in_matrix):
    strKeyframe = self.matToString(self.glMat4(in_matrix))
    return strKeyframe
  def getKeyframeDataPRS(self, in_matrix):
    #https://docs.blender.org/api/blender_python_api_2_78_release/mathutils.html
    
    loc, rot, sca = self.glMat4(in_matrix).transposed().decompose()
    #rot is a quaternion, but we need to flip the angles.

    strKeyframe = self.vec3ToString(loc, ",") + "," + self.vec4ToString(rot, ",") + "," + self.vec3ToString(sca, ",")
    return strKeyframe    
  def getVertexWeightsForMeshArm(self, ob_mesh):
    strFile = ""
    bpy.ops.object.mode_set(mode='OBJECT')
    
    #for ob_mesh in bpy.data.objects:
    if ob_mesh.type != 'MESH':
      return "#No vertex weights for object (not mesh)\n";
    
    #if self.isSpecialMesh(ob_mesh):
    #  return "#No vertex weights for object (special mesh)\n";

    #group_names = [g.name for g in ob_mesh.vertex_groups]
    #group_names_tot = len(group_names)
    
    bWarn = False
    strFile += "#BEGIN VERTEX WEIGHTS: " + self.getObjectName(ob_mesh, None) + " " + str(len(ob_mesh.data.vertices)) + "\n"
    strFile += "#vw [arm count] 'arm name' bonecount boneid,weight 'arm name' bonecount boneid,weight ...\n"
    for iVert in ob_mesh.data.vertices:
      strVertWeights = ""
      iArmCount = 0
      for mod in ob_mesh.modifiers:
        if mod.type == 'ARMATURE':
          ob_arm = mod.object
          if ob_arm == None:
            if bWarn == False:
              strFile += self.logWarn("Armature with no object found on mesh, no weight applied " + self.getObjectName(ob_mesh, None))
              bWarn = True
          else:
            iArmCount += 1
            
            #We count groups because not all groups apply to this mesh vertex (multiple armatures for instance)
            iGroupCount = 0
            strWeightList = ""
            
            #Weight List
            strApp = ""
            for v_group in iVert.groups:
              #Get the vertex group (the bone name) associated with this vertex
              # Note - I'm not sure if v_group.group is always able to index into vertex_groups.
              vgroup = ob_mesh.vertex_groups[v_group.group]
              if vgroup.index != v_group.group:
                strVertWeights += self.logError("Index for group is invalid \"" + self.getObjectName(ob_arm, None) + "\"\"n")
              
              boneIndex = self.getBoneId(vgroup.name, ob_arm.pose.bones)
              
              if boneIndex > -1:
                #For multiple armatures bound to a single mesh the index will be -1 - no bone found
                #We just skip it - this is valid - because armature modifiers apply on the MESH level - but weights might be bound to each armature.
                iGroupCount+=1
                strWeightList +=  strApp + str(boneIndex) + "," + self.floatToString(v_group.weight)
                strApp = ","
            strWeightList += " "
 
            strVertWeights +=  "" + str(self.getArmatureId(ob_arm)) + " " + str(iGroupCount) + " " + strWeightList
        
      strFile += "vw " +  str(iArmCount) + " " + strVertWeights + "\n"         

    return strFile
  def getPoseBoneBindMatrices(self, in_bone, parentBoneHead, boneList, forBoneName):
    strFile = ""
    worldPos = in_bone.head
    #msg(in_bone.head)

    #Object Space Inverse Bind Pose
    bpoi =  Matrix.Translation(worldPos).copy().to_4x4()
    #bpoi.invert();

    #Joint Space Relative Bind Pose
    boneLocalTranslation = worldPos - parentBoneHead;
    bpjr = Matrix.Translation(boneLocalTranslation).copy().to_4x4()
    
    if in_bone.name == forBoneName:
      #keeping these as matrices (why change?)
      strFile += "bone_bind_world " +  "m44 " + self.getKeyframeDataMat4(bpoi) + "\n";
      #strFile += "bone_bind_local " +  "m44 " + self.getKeyframeDataMat4(bpjr) + "\n";
      #strFile += "bpoi " +  self.getKeyframeData(self.glMat4(bpoi)) + "\n"
      #strFile += "bpjr " +  self.getKeyframeData(self.glMat4(bpjr)) + "\n"
    
    boneChildren = self.getBoneChildren(in_bone, boneList)
    for child in boneChildren:      
      strFile += self.getPoseBoneBindMatrices(child, worldPos, boneList, forBoneName)
      
    return strFile
  #Convert matrix to 4x4 matrix, then to string
  def fPrec(self):
    return "%.8f"
  def matToString(self, mat, delim = ',', sp = False):
    strRet = ""
    mat_4 = mat.to_4x4()
    strApp = ""
    for row in range(4):
      if sp == True:
        strRet += "#"
      for col in range(4):
        #strRet += str(row) + " " + str(col)
        strFormat = "" + strApp + self.fPrec() + ""
        strRet += strFormat % mat_4[row][col] 
        strApp = delim
      if sp == True:
        strRet += "\n"
    return strRet
  def floatToString(self, float):
    strFormat = "" + self.fPrec() + ""
    strRet =  strFormat % (float)
    return strRet;
  def vec4ToString(self, vec4, delim=' '):
    strRet = ""
    strFormat = "" + self.fPrec() + delim + self.fPrec() + delim + self.fPrec() + delim + self.fPrec() + ""
    strRet =  strFormat % (vec4.x, vec4.y, vec4.z, vec4.w)
    return strRet    
  def vec3ToString(self, vec3, delim=' '):
    strRet = ""
    strFormat = "" + self.fPrec() + delim + self.fPrec() + delim + self.fPrec() + ""
    strRet =  strFormat % (vec3.x, vec3.y, vec3.z)
    return strRet    
  def color3ToString(self, vec3, delim=' '):
    strRet = ""
    strFormat = "" + self.fPrec() + delim + self.fPrec() + delim + self.fPrec() + ""
    strRet =  strFormat % (vec3.r, vec3.g, vec3.b)
    return strRet    
  def vec2ToString(self, vec2, delim=' '):
    strRet = ""
    strFormat = "" + self.fPrec() + delim + self.fPrec() + ""
    strRet =  strFormat % (vec2.x, vec2.y) 
    return strRet
  def glEuler3(self, eu):
    #NOTE: use Deep exploration to test- same coordinate system as vault
    #Convert Vec3 tgo OpenGL coords
    #-x,-z,-y is the correct export into deep expl
    #This is the correct OpenGL conversion
    if self._config._convertY_Up:
      ret = Euler([eu.x, eu.y, eu.z])
      tmp = ret.y
      ret.y = ret.z
      ret.z = tmp
      return ret
    else:
      return eu
  def glQuat(self, quat):
    if self._config._convertY_Up:
      e = quat.to_euler()
      e = self.glEuler3(e)
      return e.to_quaternion()
    else:
      return quat
  def glVec3(self, vec):
    #NOTE: use Deep exploration to test- same coordinate system as vault
    #Convert Vec3 tgo OpenGL coords
    ret = Vector([vec.x, vec.y, vec.z])

    #-x,-z,-y is the correct export into deep expl
    #This is the correct OpenGL conversion
    if(self._config._convertY_Up):
      tmp = ret.y
      ret.y = ret.z
      ret.z = tmp
      ret.x = ret.x
    
    return ret
  def glMat4(self, in_mat):
    #NOTE this functio works
     # global_matrix = io_utils.axis_conversion(to_forward="-Z", to_up="Y").to_4x4()
     # mat_conv = global_matrix * in_mat * global_matrix.inverted()
     # mat_conv = mat_conv.transposed()
     # return mat_conv
    
    #NOTE: t12/20/17 this actually works but seems to return a negative z value?
  
    #NOTE: use Deep exploration to test- same coordinate system as vault
    #convert matrix from Blender to OpenGL Coords
    m1 = in_mat.copy()
    m1 = m1.to_4x4()
    
    x=0
    y=1
    z=2
    w=3
    
    #change of basis matrix
    if self._config._convertY_Up:
      cbm = Matrix.Identity(4)
      cbm[x][0] = 1
      cbm[x][1] = 0
      cbm[x][2] = 0
      cbm[x][3] = 0
      
      cbm[y][0] = 0
      cbm[y][1] = 0
      cbm[y][2] = 1
      cbm[y][3] = 0
      
      cbm[z][0] = 0
      cbm[z][1] = 1
      cbm[z][2] = 0
      cbm[z][3] = 0
      
      cbm[w][0] = 0
      cbm[w][1] = 0
      cbm[w][2] = 0
      cbm[w][3] = 1
      
      #multiply CBM twice
      m1 = cbm.inverted() * m1 * cbm.inverted();
    
    #blender is row-major?
   # m1.transpose()  
    
    return m1
  def getContactBoxNameSuffix(self):
    return "_CB"
  def getPhysicsVolumeNameSuffix(self):
    return "_PV"    
  def getBoxString(self, data):
    #exports the bounding box of a mesh.
    strFile = ""
    flt_max = 10000000
    
    amin = Vector([flt_max,flt_max,flt_max])
    amax = Vector([-flt_max,-flt_max,-flt_max])
    
    for vert in data.vertices:
      if self.glVec3(vert.co).x < amin.x: amin.x = self.glVec3(vert.co).x
      if self.glVec3(vert.co).y < amin.y: amin.y = self.glVec3(vert.co).y
      if self.glVec3(vert.co).z < amin.z: amin.z = self.glVec3(vert.co).z
      if self.glVec3(vert.co).x > amax.x: amax.x = self.glVec3(vert.co).x
      if self.glVec3(vert.co).y > amax.y: amax.y = self.glVec3(vert.co).y
      if self.glVec3(vert.co).z > amax.z: amax.z = self.glVec3(vert.co).z
        
    #Note: amin, amax are already in OpenGL coordinates above.
    strFile += self.vec3ToString(amin) + " "
    strFile += self.vec3ToString(amax) + "\n"
    #strFile += "%.6f %.6f %.6f " % amin[:] + ""
    #strFile += "%.6f %.6f %.6f" % amax[:] + ""  
    
    return strFile
  def applyAllLocalTransforms(self):
    #bake all transforms for all objects
    for ob in bpy.data.objects:
      if ob.type != 'MESH':
        continue
        #Apply object transform
        context.scene.objects.active = ob
        ob.select = True
        bpy.ops.Object.transform_apply(location=True, rotation=True, scale=True)
  def getObjectName(self, ob, ob_bone):
    strName = ""
    strName += self._exportName + "." + ob.name
    if ob_bone != None:
      strName += "." + ob_bone.name
    return strName
  def logWarn(self, strMsg):
    strMsg = "WARNING: " + strMsg
    msg(strMsg)
    self._iWarningCount+=1
    #self.appendLine(strMsg)
  def logError(self, strMsg):
    msg(strMsg)
    self._iErrorCount+=1
    #self.appendLine(strMsg)
    

def euler3ToDeg(e3):
  #math.radians(45.0)
  eul = Euler((0.0, 0.0, 0.0), 'XYZ')
  eul.x = math.degrees(e3.x)
  eul.y = math.degrees(e3.y)
  eul.z = math.degrees(e3.z)
  return eul
def is_pose_bone(ob):
  return type(ob) == bpy.types.PoseBone
def is_armature(ob):
  return type(ob) == bpy.types.Object and ob.type == 'ARMATURE'
def is_mesh(ob):
  return type(ob) == bpy.types.Object and ob.type == 'MESH' 
def is_object(ob):
  return type(ob) == bpy.types.Object
def is_light(ob):
  return type(ob) == bpy.types.Object and ob.type == 'LIGHT'  
def is_camera(ob):
  return type(ob) == bpy.types.Object and ob.type == 'CAMERA'    
def get_node_type_id(ob):
  if is_mesh(ob): return 1
  elif is_armature(ob): return 2
  elif is_light(ob): return 3
  elif is_camera(ob): return 4
  #elif is_object(ob): return 4 # e.g. - 'node'?
  else: throw("invalid object: " + str(ob))
def throw(ex):
  raise Exception("Exception: " + ex)
def millis():
  return int(round(time.time() * 1000))
def msg(str):
  __builtins__.print(str)
  sys.stdout.flush()
  time.sleep(0)
def packVec2(block : bytearray, val):
  packFloat(block, val[0])
  packFloat(block, val[1])
def packVec3(block : bytearray, val):
  packFloat(block, val[0])
  packFloat(block, val[1])
  packFloat(block, val[2])
def packVec4(block : bytearray, val):
  packFloat(block, val[0])
  packFloat(block, val[1])
  packFloat(block, val[2])
  packFloat(block, val[3])
def packInt16(block : bytearray, val): block.extend(struct.pack('h', val))
def packUInt16(block : bytearray, val): block.extend(struct.pack('H', val))
def packInt32(block : bytearray, val): block.extend(struct.pack('i', val))
def packUInt32(block : bytearray, val): block.extend(struct.pack('I', val))
def packBoneIDu16(block : bytearray, val): packUInt16(block,val)
def packNodeIDu32(block : bytearray, val): packUInt32(block,val)
def packFloat(block : bytearray, val): block.extend(struct.pack('f', val))
def packDouble(block : bytearray, val): block.extend(struct.pack('d', val))
def packData(block : bytearray, val : bytearray): block.extend(val)
def packString(block : bytearray, str):
    bts = bytes(str,'utf-8')
    packInt32(block, len(bts))
    packData(block, bts)
def launch_async(fn, timeout):
  p = multiprocessing.Process(target=fn, name=str(fn), args=())
  p.start()
  p.join(timeout)
  if p.is_alive():
    msg(str(fn) + " timed out.")
    p.terminate()
def printExcept(e):
  msg(str(e))
  exc_type, exc_obj, exc_tb = sys.exc_info()
  fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
  msg(fname, "line ", exc_tb.tb_lineno)
  msg(traceback.format_exc())


def getArgs():
  # get the args passed to blender after "--", all of which are ignored by
  # blender so scripts may receive their own arguments
  argv = sys.argv
  if "--" not in argv:
    argv = []  # as if no args are passed
  else:
    argv = argv[argv.index("--") + 1:]  # get all args after "--"
  # When --help or no args are given, print this help
  usage_text = ("-o = output path")
  parser = argparse.ArgumentParser(description=usage_text)
  parser.add_argument("-o", dest="outpath", type=str, required=False, help="Output Path")
  args = parser.parse_args(argv)  # In this example we wont use the args
  if not argv:
    parser.print_help()
    return args
  return args

try:
  outpath = os.path.abspath("./");
  p = None
  try:
    p = getArgs();
  except Exception as e:
    printExcept(e)
  
  if p == None:
    msg("Error parsing args, outpath will default to " + outpath)
  elif not p.outpath:
    msg("Error parsing args, outpath will default to " + outpath)
  else:
    outpath = p.outpath
    msg("Output path found: " + p.outpath);

  mob = Mbi2Export(outpath)
  mob.export()
except Exception as e:
  #traceback.print_exc()
  msg("Error: Failed to export model: ")
  printExcept(e)












