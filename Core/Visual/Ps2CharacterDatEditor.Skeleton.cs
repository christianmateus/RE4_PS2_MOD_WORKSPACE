using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static partial class Ps2CharacterDatEditor
{
    private readonly record struct RawBone(byte Id, byte ParentId, Vector3 Local, int Offset);

    private static void AdaptMeshToTargetSkeleton(byte[] targetBin, byte[] replacementBin, SkeletonAdaptMode mode)
    {
        if (targetBin.Length < 0x10 || replacementBin.Length < 0x10) throw new InvalidDataException("BIN sem cabeçalho suficiente para validar o esqueleto.");
        int targetBonesOffset = checked((int)BitConverter.ToUInt32(targetBin, 4));
        int sourceBonesOffset = checked((int)BitConverter.ToUInt32(replacementBin, 4));
        int targetCount = targetBin[9], sourceCount = replacementBin[9];
        if (targetCount <= 0 || sourceCount <= 0) throw new InvalidOperationException("Um dos BINs não possui esqueleto adaptável.");
        if (targetBonesOffset < 0 || targetBonesOffset + targetCount * 16 > targetBin.Length || sourceBonesOffset < 0 || sourceBonesOffset + sourceCount * 16 > replacementBin.Length)
            throw new InvalidDataException("Tabela de ossos inválida em um dos BINs.");

        RawBone[] ReadBones(byte[] data, int offset, int count) => Enumerable.Range(0, count).Select(i =>
        {
            int p = offset + i * 16;
            return new RawBone(data[p], data[p + 1], new Vector3(BitConverter.ToSingle(data, p + 4), BitConverter.ToSingle(data, p + 8), BitConverter.ToSingle(data, p + 12)), p);
        }).ToArray();
        RawBone[] targetBones = ReadBones(targetBin, targetBonesOffset, targetCount);
        RawBone[] sourceBones = ReadBones(replacementBin, sourceBonesOffset, sourceCount);

        Vector3[] Globals(RawBone[] bones)
        {
            var result = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                int parent = -1;
                for (int p = i - 1; p >= 0; p--) if (bones[p].Id == bones[i].ParentId) { parent = p; break; }
                result[i] = bones[i].Local + (parent >= 0 ? result[parent] : Vector3.Zero);
            }
            return result;
        }
        Vector3[] targetGlobal = Globals(targetBones), sourceGlobal = Globals(sourceBones);
        (Vector3 Center, float Scale) Bounds(Vector3[] points)
        {
            Vector3 min = points.Aggregate(new Vector3(float.PositiveInfinity), Vector3.Min), max = points.Aggregate(new Vector3(float.NegativeInfinity), Vector3.Max);
            return ((min + max) * 0.5f, Math.Max(0.0001f, (max - min).Length()));
        }
        var tb = Bounds(targetGlobal); var sb = Bounds(sourceGlobal);
        var mapping = new Dictionary<byte, RawBone>();
        var usedTargetIds = new HashSet<byte>();
        RawBone? rigidAnchor = null;
        // A donor with more bones than the receiver slot cannot be mapped one to
        // one. Mapping the surplus by proximity commonly reaches Leon's secondary
        // hair chain (wind physics), making an entire transplanted head wobble.
        // Attach such meshes rigidly to the receiver bone with the largest direct
        // subtree -- for pl00 head slots this is the stable head anchor (bone 4).
        bool rigidAttachment = mode == SkeletonAdaptMode.Rigid || mode == SkeletonAdaptMode.Automatic && sourceBones.Length > targetBones.Length;
        if (rigidAttachment)
        {
            RawBone anchor = targetBones
                .OrderByDescending(candidate => targetBones.Count(other => other.ParentId == candidate.Id))
                .ThenBy(candidate => candidate.Id)
                .First();
            rigidAnchor = anchor;
            foreach (RawBone sourceBone in sourceBones)
            {
                mapping[sourceBone.Id] = anchor;
                Buffer.BlockCopy(targetBin, anchor.Offset, replacementBin, sourceBone.Offset, 16);
                replacementBin[sourceBone.Offset + 1] = 0xFF;
            }
        }
        else for (int i = 0; i < sourceBones.Length; i++)
        {
            RawBone sourceBone = sourceBones[i];
            Vector3 normalizedSource = (sourceGlobal[i] - sb.Center) / sb.Scale;
            mapping.TryGetValue(sourceBone.ParentId, out RawBone mappedParent);
            bool hasMappedParent = sourceBone.ParentId != 0xFF && mapping.ContainsKey(sourceBone.ParentId);
            int best = Enumerable.Range(0, targetBones.Length).OrderBy(t =>
            {
                RawBone candidate = targetBones[t];
                float score = Vector3.DistanceSquared(normalizedSource, (targetGlobal[t] - tb.Center) / tb.Scale);
                if ((sourceBone.ParentId == 0xFF) != (candidate.ParentId == 0xFF)) score += 4f;
                // Parent-chain continuity matters more than coincidental numeric IDs
                // when adapting meshes between different character families.
                if (hasMappedParent && candidate.ParentId != mappedParent.Id) score += 0.75f;
                // Duplicate IDs make two distinct donor pivots receive the same matrix.
                // Prefer a one-to-one assignment while the receiver has spare bones.
                if (usedTargetIds.Contains(candidate.Id) && usedTargetIds.Count < targetBones.Length) score += 2f;
                if (candidate.Id == sourceBone.Id) score -= 0.04f;
                return score;
            }).First();
            mapping[sourceBone.Id] = targetBones[best];
            usedTargetIds.Add(targetBones[best].Id);
        }

        // Keep the donor bind pose (XYZ) intact. Copying the receiver's complete
        // 16-byte bone record changes the donor's pivots and produces long spikes
        // around the jaw/neck once the game applies an animation. Only IDs and the
        // parent chain belong to the receiver animation rig.
        if (!rigidAttachment) foreach (RawBone sourceBone in sourceBones)
        {
            if (!mapping.TryGetValue(sourceBone.Id, out RawBone mapped)) continue;
            replacementBin[sourceBone.Offset] = mapped.Id;
            replacementBin[sourceBone.Offset + 1] = sourceBone.ParentId != 0xFF && mapping.TryGetValue(sourceBone.ParentId, out RawBone mappedParentBone)
                ? mappedParentBone.Id
                : (byte)0xFF;
        }

        // Material nodes carry compact lists of bone IDs used by their weight maps.
        int materialCount = BitConverter.ToUInt16(replacementBin, 0x0A);
        int materialOffset = checked((int)BitConverter.ToUInt32(replacementBin, 0x0C));
        if (materialOffset > 0 && materialOffset + materialCount * 16 <= replacementBin.Length)
            for (int i = 0; i < materialCount; i++)
            {
                int node = checked((int)BitConverter.ToUInt32(replacementBin, materialOffset + i * 16 + 12));
                if (node <= 0 || node + 4 > replacementBin.Length) continue;
                int boneIds = replacementBin[node + 3];
                for (int b = 0; b < boneIds && node + 4 + b < replacementBin.Length; b++)
                    if (rigidAnchor.HasValue) replacementBin[node + 4 + b] = rigidAnchor.Value.Id;
                    else if (mapping.TryGetValue(replacementBin[node + 4 + b], out RawBone mapped)) replacementBin[node + 4 + b] = mapped.Id;
            }
    }

}
