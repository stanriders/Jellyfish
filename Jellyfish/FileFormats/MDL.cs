using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.FileFormats;

#region MDL

public class MDL
{
    public studiohdr_t Header { get; set; }

    public VTX Vtx { get; set; } = null!;

    public static MDL Load(string path)
    {
        var file = File.ReadAllBytes($"{path}.mdl");
        return new MDL
        {
            Header = DeserializeStudiohdr_t(file),
            Vtx = VTX.Load(path)
        };
    }

    private static studiohdr_t DeserializeStudiohdr_t(byte[] file)
    {
        var header = new studiohdr_t();
        var offset = 0;

        header.id = ParseInt(file, ref offset);
        header.version = ParseInt(file, ref offset);
        header.checksum = ParseInt(file, ref offset);

        header.name = new char[64];
        for (var i = 0; i < 64; i++)
            header.name[i] = (char)file[offset + sizeof(byte) * i];
        offset += sizeof(byte) * 64;

        header.dataLength = ParseInt(file, ref offset);
        header.eyeposition = ParseVector(file, ref offset);
        header.illumposition = ParseVector(file, ref offset);
        header.hull_min = ParseVector(file, ref offset);
        header.hull_max = ParseVector(file, ref offset);
        header.view_bbmin = ParseVector(file, ref offset);
        header.view_bbmax = ParseVector(file, ref offset);
        header.flags = ParseInt(file, ref offset);
        header.bone_count = ParseInt(file, ref offset);
        header.bone_offset = ParseInt(file, ref offset);
        header.bonecontroller_count = ParseInt(file, ref offset);
        header.bonecontroller_offset = ParseInt(file, ref offset);
        header.hitbox_count = ParseInt(file, ref offset);
        header.hitbox_offset = ParseInt(file, ref offset);
        header.localanim_count = ParseInt(file, ref offset);
        header.localanim_offset = ParseInt(file, ref offset);
        header.localseq_count = ParseInt(file, ref offset);
        header.localseq_offset = ParseInt(file, ref offset);

        // it breaks starting from here
        header.activitylistversion = ParseInt(file, ref offset);
        header.eventsindexed = ParseInt(file, ref offset);
        header.texture_count = ParseInt(file, ref offset);
        header.texture_offset = ParseInt(file, ref offset);
        header.texturedir_count = ParseInt(file, ref offset);
        header.texturedir_offset = ParseInt(file, ref offset);
        header.skinreference_count = ParseInt(file, ref offset);
        header.skinrfamily_count = ParseInt(file, ref offset);
        header.skinreference_index = ParseInt(file, ref offset);
        header.bodypart_count = ParseInt(file, ref offset);
        header.bodypart_offset = ParseInt(file, ref offset);
        header.attachment_count = ParseInt(file, ref offset);
        header.attachment_offset = ParseInt(file, ref offset);
        header.localnode_count = ParseInt(file, ref offset);
        header.localnode_index = ParseInt(file, ref offset);
        header.localnode_name_index = ParseInt(file, ref offset);
        header.flexdesc_count = ParseInt(file, ref offset);
        header.flexdesc_index = ParseInt(file, ref offset);
        header.flexcontroller_count = ParseInt(file, ref offset);
        header.flexcontroller_index = ParseInt(file, ref offset);
        header.flexrules_count = ParseInt(file, ref offset);
        header.flexrules_index = ParseInt(file, ref offset);
        header.ikchain_count = ParseInt(file, ref offset);
        header.ikchain_index = ParseInt(file, ref offset);
        header.mouths_count = ParseInt(file, ref offset);
        header.mouths_index = ParseInt(file, ref offset);
        header.localposeparam_count = ParseInt(file, ref offset);
        header.localposeparam_index = ParseInt(file, ref offset);
        header.surfaceprop_index = ParseInt(file, ref offset);
        header.keyvalue_index = ParseInt(file, ref offset);
        header.keyvalue_count = ParseInt(file, ref offset);
        header.iklock_count = ParseInt(file, ref offset);
        header.iklock_index = ParseInt(file, ref offset);

        return header;
    }

    private static int ParseInt(byte[] file, ref int offset)
    {
        var res = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);
        return res;
    }

    private static float[] ParseVector(byte[] file, ref int offset)
    {
        var res = new float[3];
        for (var i = 0; i < 3; i++)
            res[i] = BitConverter.ToSingle(file, offset + sizeof(float) * i);

        offset += sizeof(float) * 3;
        return res;
    }

    public struct studiohdr_t
    {
        public int id; // Model format ID, such as "IDST" (0x49 0x44 0x53 0x54)
        public int version; // Format version number, such as 48 (0x30,0x00,0x00,0x00)
        public int checksum; // This has to be the same in the phy and vtx files to load!

        public char[] name; // The internal name of the model, padding with null bytes.
        // Typically "my_model.mdl" will have an internal name of "my_model"

        public int dataLength; // Data size of MDL file in bytes.

        // A vector is 12 bytes, three 4-byte float-values in a row.
        public float[] eyeposition; // Position of player viewpoint relative to model origin

        public float[]
            illumposition; // ?? Presumably the point used for lighting when per-vertex lighting is not enabled.

        public float[] hull_min; // Corner of model hull box with the least X/Y/Z values

        public float[] hull_max; // Opposite corner of model hull box

        public float[] view_bbmin;

        public float[] view_bbmax;

        public int flags; // Binary flags in little-endian order. 
        // ex (00000001,00000000,00000000,11000000) means flags for position 0, 30, and 31 are set. 
        // Set model flags section for more information

        /*
         * After this point, the header contains many references to offsets
         * within the MDL file and the number of items at those offsets.
         *
         * Offsets are from the very beginning of the file.
         * 
         * Note that indexes/counts are not always paired and ordered consistently.
         */

        // mstudiobone_t
        public int bone_count; // Number of data sections (of type mstudiobone_t)
        public int bone_offset; // Offset of first data section

        // mstudiobonecontroller_t
        public int bonecontroller_count;
        public int bonecontroller_offset;

        // mstudiohitboxset_t
        public int hitbox_count;
        public int hitbox_offset;

        // mstudioanimdesc_t
        public int localanim_count;
        public int localanim_offset;

        // mstudioseqdesc_t
        public int localseq_count;
        public int localseq_offset;

        public int activitylistversion; // ??
        public int eventsindexed; // ??

        // VMT texture filenames
        // mstudiotexture_t
        public int texture_count;
        public int texture_offset;

        // This offset points to a series of ints.
        // Each int value, in turn, is an offset relative to the start of this header/the-file,
        // At which there is a null-terminated string.
        public int texturedir_count;
        public int texturedir_offset;

        // Each skin-family assigns a texture-id to a skin location
        public int skinreference_count;
        public int skinrfamily_count;
        public int skinreference_index;

        // mstudiobodyparts_t
        public int bodypart_count;
        public int bodypart_offset;

        // Local attachment points		
        // mstudioattachment_t
        public int attachment_count;
        public int attachment_offset;

        // Node values appear to be single bytes, while their names are null-terminated strings.
        public int localnode_count;
        public int localnode_index;
        public int localnode_name_index;

        // mstudioflexdesc_t
        public int flexdesc_count;
        public int flexdesc_index;

        // mstudioflexcontroller_t
        public int flexcontroller_count;
        public int flexcontroller_index;

        // mstudioflexrule_t
        public int flexrules_count;
        public int flexrules_index;

        // IK probably referse to inverse kinematics
        // mstudioikchain_t
        public int ikchain_count;
        public int ikchain_index;

        // Information about any "mouth" on the model for speech animation
        // More than one sounds pretty creepy.
        // mstudiomouth_t
        public int mouths_count;
        public int mouths_index;

        // mstudioposeparamdesc_t
        public int localposeparam_count;
        public int localposeparam_index;

        /*
         * For anyone trying to follow along, as of this writing,
         * the next "surfaceprop_index" value is at position 0x0134 (308)
         * from the start of the file.
         */

        // Surface property value (single null-terminated string)
        public int surfaceprop_index;

        // Unusual: In this one index comes first, then count.
        // Key-value data is a series of strings. If you can't find
        // what you're interested in, check the associated PHY file as well.
        public int keyvalue_index;
        public int keyvalue_count;

        // More inverse-kinematics
        // mstudioiklock_t
        public int iklock_count;
        public int iklock_index;


        public float mass; // Mass of object (4-bytes)
        public int contents; // ??

        // Other models can be referenced for re-used sequences and animations
        // (See also: The $includemodel QC option.)
        // mstudiomodelgroup_t
        public int includemodel_count;
        public int includemodel_index;

        public int virtualModel; // Placeholder for mutable-void*

        // mstudioanimblock_t
        public int animblocks_name_index;
        public int animblocks_count;
        public int animblocks_index;

        public int animblockModel; // Placeholder for mutable-void*

        // Points to a series of bytes?
        public int bonetablename_index;

        public int vertex_base; // Placeholder for void*
        public int offset_base; // Placeholder for void*

        // Used with $constantdirectionallight from the QC 
        // Model should have flag #13 set if enabled
        public byte directionaldotproduct;

        public byte rootLod; // Preferred rather than clamped

        // 0 means any allowed, N means Lod 0 -> (N-1)
        public byte numAllowedRootLods;

        public byte unused1; // ??
        public int unused2; // ??

        // mstudioflexcontrollerui_t
        public int flexcontrollerui_count;
        public int flexcontrollerui_index;

        /**
             * Offset for additional header information.
             * May be zero if not present, or also 408 if it immediately 
             * follows this studiohdr_t
             */
        // studiohdr2_t
        public int studiohdr2index;

        public int unused3; // ??
    }
}

#endregion

public class VTX
{
    public FileHeader_t Header { get; set; }
    public List<BodyPartHeader_t> BodyParts { get; set; } = new();
    public List<ModelHeader_t> Models { get; set; } = new();
    public List<ModelLODHeader_t> Lods { get; set; } = new();
    public List<MeshHeader_t> Meshes { get; set; } = new();
    public List<StripGroupHeader_t> VertexStrips { get; set; } = new();

    public List<MeshInfo> MeshInfos { get; set; } = new();

    public static VTX Load(string path)
    {
        var file = File.ReadAllBytes($"{path}.vtx");

        var res = new VTX();
        res.Header = DeserializeFileHeader_t(file);

        res.BodyParts = DeserializeBodyPartHeader_t(file, res.Header.bodyPartOffset, res.Header.numBodyParts);
        for (var i = 0; i < res.Header.numBodyParts; i++)
        {
            var bpoffset = res.Header.bodyPartOffset * (i + 1);
            var bodyPart = res.BodyParts[i];

            res.Models.AddRange(DeserializeModelHeader_t(file, bpoffset + bodyPart.modelOffset, bodyPart.numModels));
            for (var j = 0; j < bodyPart.numModels; j++)
            {
                var modelsoffset = bodyPart.modelOffset * (j + 1);
                var model = res.Models[i * j];

                res.Lods.AddRange(DeserializeModelLODHeader_t(file, bpoffset + modelsoffset + model.lodOffset,
                    model.numLODs));
                for (var k = 0; k < model.numLODs; k++)
                {
                    var lodsoffset = model.lodOffset * (k + 1);
                    var lod = res.Lods[i + j + k];

                    res.Meshes.AddRange(DeserializeMeshHeader_t(file,
                        bpoffset + modelsoffset + lodsoffset + lod.meshOffset, lod.numMeshes));
                    for (var l = 0; l < lod.numMeshes; l++)
                    {
                        var meshessoffset = lod.meshOffset * (l + 1);
                        var mesh = res.Meshes[i + j + k + l];

                        res.VertexStrips.AddRange(DeserializeStripGroupHeader_t(file,
                            bpoffset + modelsoffset + lodsoffset + meshessoffset + mesh.stripGroupHeaderOffset));
                        for (var m = 0; m < 1; m++)
                        {
                            var stripssoffset = mesh.stripGroupHeaderOffset * (m + 1);
                            var strip = res.VertexStrips[ /*i + j + k + l + m*/0];

                            var vertices = new List<Vector3>();
                            var normals = new List<Vector3>();
                            var texcoords = new List<Vector2>();
                            var indices = new List<uint>();

                            var vvd = VVD.Load(path, strip.numVerts);
                            foreach (var vert in vvd.Vertices)
                            {
                                vertices.Add(vert.m_vecPosition);
                                normals.Add(vert.m_vecNormal);
                                texcoords.Add(vert.m_vecTexCoord);
                            }

                            var indOffset = bpoffset + modelsoffset + lodsoffset + meshessoffset + stripssoffset +
                                            strip.indexOffset;
                            for (var indIndex = 0; indIndex < strip.numIndices; indIndex++)
                            {
                                var index = BitConverter.ToUInt16(file, indOffset);
                                if (index <= strip.numVerts)
                                    indices.Add(index);
                                indOffset += sizeof(ushort);
                            }

                            res.MeshInfos.Add(new MeshInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(path),
                                Vertices = vertices,
                                Normals = normals,
                                UVs = texcoords,
                                Indices = indices
                            });
                        }
                    }
                }
            }
        }

        return res;
    }

    private static FileHeader_t DeserializeFileHeader_t(byte[] file)
    {
        var header = new FileHeader_t();
        var offset = 0;

        header.version = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.vertCacheSize = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.maxBonesPerStrip = BitConverter.ToUInt16(file, offset);
        offset += sizeof(ushort);

        header.maxBonesPerTri = BitConverter.ToUInt16(file, offset);
        offset += sizeof(ushort);

        header.maxBonesPerVert = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.checkSum = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.numLODs = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.materialReplacementListOffset = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.numBodyParts = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.bodyPartOffset = BitConverter.ToInt32(file, offset);

        return header;
    }

    private static List<BodyPartHeader_t> DeserializeBodyPartHeader_t(byte[] file, int offset = 0, int amt = 1)
    {
        var ret = new List<BodyPartHeader_t>();
        var size = Marshal.SizeOf<BodyPartHeader_t>();
        for (var i = 0; i < amt; i++)
            ret.Add(new BodyPartHeader_t
            {
                numModels = BitConverter.ToInt32(file, offset + size * i),
                modelOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int))
            });

        return ret;
    }

    private static List<ModelHeader_t> DeserializeModelHeader_t(byte[] file, int offset = 0, int amt = 1)
    {
        var ret = new List<ModelHeader_t>();
        var size = Marshal.SizeOf<ModelHeader_t>();
        for (var i = 0; i < amt; i++)
            ret.Add(new ModelHeader_t
            {
                numLODs = BitConverter.ToInt32(file, offset + size * i),
                lodOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int))
            });
        return ret;
    }

    private static List<ModelLODHeader_t> DeserializeModelLODHeader_t(byte[] file, int offset = 0, int amt = 1)
    {
        var ret = new List<ModelLODHeader_t>();
        var size = Marshal.SizeOf<ModelLODHeader_t>();
        for (var i = 0; i < amt; i++)
            ret.Add(new ModelLODHeader_t
            {
                numMeshes = BitConverter.ToInt32(file, offset + size * i),
                meshOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int)),
                switchPoint = BitConverter.ToSingle(file, offset + size * i + sizeof(int) + sizeof(int))
            });

        return ret;
    }

    private static List<MeshHeader_t> DeserializeMeshHeader_t(byte[] file, int offset = 0, int amt = 1)
    {
        var ret = new List<MeshHeader_t>();
        var size = 9; //Marshal.SizeOf<MeshHeader_t>();
        for (var i = 0; i < amt; i++)
            ret.Add(new MeshHeader_t
            {
                numStripGroups = BitConverter.ToInt32(file, offset + size * i),
                stripGroupHeaderOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int)),
                flags = file[offset + size * i + sizeof(int) + sizeof(int)]
            });

        return ret;
    }

    private static List<StripGroupHeader_t> DeserializeStripGroupHeader_t(byte[] file, int offset = 0, int amt = 1)
    {
        var ret = new List<StripGroupHeader_t>();
        var size = 25; //Marshal.SizeOf<StripGroupHeader_t>();
        for (var i = 0; i < amt; i++)
        {
            ret.Add(new StripGroupHeader_t
            {
                numVerts = BitConverter.ToInt32(file, offset + size * i),
                vertOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int)),
                numIndices = BitConverter.ToInt32(file, offset + size * i + sizeof(int) + sizeof(int)),
                indexOffset = BitConverter.ToInt32(file, offset + size * i + sizeof(int) + sizeof(int) + sizeof(int)),
                numStrips = BitConverter.ToInt32(file,
                    offset + size * i + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int)),
                stripOffset = BitConverter.ToInt32(file,
                    offset + size * i + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int)),
                flags = file[
                    offset + size * i + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int) +
                    sizeof(int)]
            });
            offset += size;
        }

        return ret;
    }

    public struct FileHeader_t
    {
        // file version as defined by OPTIMIZED_MODEL_FILE_VERSION (currently 7)
        public int version;

        // hardware params that affect how the model is to be optimized.
        public int vertCacheSize;
        public ushort maxBonesPerStrip;
        public ushort maxBonesPerTri;
        public int maxBonesPerVert;

        // must match checkSum in the .mdl
        public int checkSum;

        public int numLODs; // Also specified in ModelHeader_t's and should match

        // Offset to materialReplacementList Array. one of these for each LOD, 8 in total
        public int materialReplacementListOffset;

        //Defines the size and location of the body part array
        public int numBodyParts;
        public int bodyPartOffset;
    }

    public struct BodyPartHeader_t
    {
        //Model array
        public int numModels;
        public int modelOffset;
    }

    public struct ModelHeader_t
    {
        //LOD mesh array
        public int numLODs; //This is also specified in FileHeader_t
        public int lodOffset;
    }

    public struct ModelLODHeader_t
    {
        //Mesh array
        public int numMeshes;
        public int meshOffset;

        public float switchPoint;
    }

    public struct MeshHeader_t
    {
        public int numStripGroups;
        public int stripGroupHeaderOffset;

        /*
        0x01	STRIPGROUP_IS_FLEXED
        0x02	STRIPGROUP_IS_HWSKINNED
        0x04	STRIPGROUP_IS_DELTA_FLEXED
        0x08	STRIPGROUP_SUPPRESS_HW_MORPH
        */
        public /*unsigned */ byte flags; // originally char, but c# char is 16 bit and file assumes 8 bit
    }

    public struct StripGroupHeader_t
    {
        // These are the arrays of all verts and indices for this mesh.  strips index into this.
        public int numVerts;
        public int vertOffset;

        public int numIndices;
        public int indexOffset;

        public int numStrips;
        public int stripOffset;

        public /*unsigned */ byte flags; // originally char, but c# char is 16 bit and file assumes 8 bit

        //if you have problems with parsing try to skip 8 bytes here
    }

    // A strip is a piece of a stripgroup which is divided by bones 
    public struct StripHeader_t
    {
        private int numIndices;
        private int indexOffset;

        private int numVerts;
        private int vertOffset;

        private short numBones;

        /*unsigned*/
        private byte flags; // originally char, but c# char is 16 bit and file assumes 8 bit

        private int numBoneStateChanges;
        private int boneStateChangeOffset;
    }

    public struct Vertex_t
    {
        // these index into the mesh's vert[origMeshVertID]'s bones
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        /*unsigned*/
        private byte[] boneWeightIndex; // originally char, but c# char is 16 bit and file assumes 8 bit

        /*unsigned*/
        private byte numBones; // originally char, but c# char is 16 bit and file assumes 8 bit

        private ushort origMeshVertID;

        // for sw skinned verts, these are indices into the global list of bones
        // for hw skinned verts, these are hardware bone indices
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private byte[] boneID; // originally char, but c# char is 16 bit and file assumes 8 bit
    }
}

public class VVD
{
    public vertexFileHeader_t Header { get; set; }
    public List<mstudiovertex_t> Vertices { get; set; } = null!;

    public static VVD Load(string path, int vertNum)
    {
        var file = File.ReadAllBytes($"{path}.vvd");
        var result = new VVD
        {
            Header = DeserializeFileHeader_t(file),
            Vertices = new List<mstudiovertex_t>(vertNum)
        };

        var offset = result.Header.vertexDataStart;
        for (var i = 0; i < vertNum; i++)
        {
            result.Vertices.Add(Deserializemstudiovertex_t(file, offset));
            offset += 48;
        }

        return result;
    }

    private static vertexFileHeader_t DeserializeFileHeader_t(byte[] file)
    {
        var header = new vertexFileHeader_t();
        var offset = 0;

        header.id = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.version = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.checksum = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.numLODs = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.numLODVertexes = new int[8];
        for (var i = 0; i < 8; i++)
        {
            header.numLODVertexes[i] = BitConverter.ToInt32(file, offset);
            offset += sizeof(int);
        }

        header.numFixups = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.fixupTableStart = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.vertexDataStart = BitConverter.ToInt32(file, offset);
        offset += sizeof(int);

        header.tangentDataStart = BitConverter.ToInt32(file, offset);

        return header;
    }

    private static mstudiovertex_t Deserializemstudiovertex_t(byte[] file, int offset)
    {
        offset += 16; // skip mstudioboneweight_t
        var res = new mstudiovertex_t();

        res.m_vecPosition = new Vector3(BitConverter.ToSingle(file, offset),
            BitConverter.ToSingle(file, offset + sizeof(float)),
            BitConverter.ToSingle(file, offset + sizeof(float) + sizeof(float)));
        offset += 12;

        res.m_vecNormal = new Vector3(BitConverter.ToSingle(file, offset),
            BitConverter.ToSingle(file, offset + sizeof(float)),
            BitConverter.ToSingle(file, offset + sizeof(float) + sizeof(float)));
        offset += 12;

        res.m_vecTexCoord = new Vector2(BitConverter.ToSingle(file, offset),
            BitConverter.ToSingle(file, offset + sizeof(float)));

        return res;
    }

    public struct vertexFileHeader_t
    {
        public int id; // MODEL_VERTEX_FILE_ID
        public int version; // MODEL_VERTEX_FILE_VERSION
        public long checksum; // same as studiohdr_t, ensures sync
        public int numLODs; // num of valid lods
        public int[] numLODVertexes; // num verts for desired root lod // MAX_NUM_LODS = 8
        public int numFixups; // num of vertexFileFixup_t
        public int fixupTableStart; // offset from base to fixup table
        public int vertexDataStart; // offset from base to vertex block
        public int tangentDataStart; // offset from base to tangent block
    }

    // NOTE: This is exactly 48 bytes
    public struct mstudiovertex_t
    {
        public mstudioboneweight_t m_BoneWeights;
        public Vector3 m_vecPosition;
        public Vector3 m_vecNormal;
        public Vector2 m_vecTexCoord;
    }

    // 16 bytes
    public struct mstudioboneweight_t
    {
        public float[] weight; //MAX_NUM_BONES_PER_VERT = 3
        public byte[] bone; //MAX_NUM_BONES_PER_VERT = 3 // char
        public byte numbones;
    }
}