using Coimbra;
using UnityEngine;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// Applies hair / beard prefabs and hair colour tint to a humanoid preview dummy.
    ///
    /// Static hair prefabs attach to the animated head bone at origin. Meshes are authored in
    /// model space with a +Z-forward tilt, so the socket applies a -90° X correction.
    /// </summary>
    public static class HumanoidStyleApplier
    {
        private const string HairGroupName = "03 Hair";
        private const string HairSocketName = "HairSocket";
        private const string FacialHairSocketName = "FacialHairSocket";
        private const string BakedHairName = "Hair";
        private const string BakedFacialHairName = "Facial Hair";

        /// <summary>Corrects HumanHair FBX prefabs that point +Z instead of +Y on the head.</summary>
        private static readonly Quaternion StyleMeshCorrection = Quaternion.Euler(-90f, 0f, 0f);

        // HumanHair.mat uses Simple Toon (_Color); _BaseColor covers URP/Lit style meshes.
        private static readonly int ShaderColorId = Shader.PropertyToID("_Color");
        private static readonly int ShaderBaseColorId = Shader.PropertyToID("_BaseColor");

        /// <param name="root">Root GameObject of the preview dummy.</param>
        /// <param name="hairPrefab">Head hair prefab, or null for none.</param>
        /// <param name="beardPrefab">Facial hair prefab, or null for none.</param>
        /// <param name="hairColor">Tint applied to hair renderers.</param>
        /// <param name="beardColor">Independent tint applied to beard renderers.</param>
        public static void Apply(
            GameObject root,
            GameObject hairPrefab,
            GameObject beardPrefab,
            Color hairColor,
            Color beardColor)
        {
            if (root == null)
            {
                return;
            }

            Transform hairSocket = EnsureSocket(root, HairSocketName);
            Transform facialSocket = EnsureSocket(root, FacialHairSocketName);

            DisableBakedStyleMeshes(root, hairSocket, facialSocket);

            ClearChildren(hairSocket);
            ClearChildren(facialSocket);

            SpawnStyle(hairPrefab, hairSocket);
            SpawnStyle(beardPrefab, facialSocket);

            ApplyHairTint(hairSocket, hairColor);
            ApplyHairTint(facialSocket, beardColor);
        }

        /// <summary>Tint only skin/eye material slots; cut surfaces, clothing and shared assets stay intact.</summary>
        public static void ApplyFace(GameObject root, Color skin, Color eyes, GameObject eyebrows, Color browColor)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null) continue;
                    bool isSkin = material.name == "HumanSkin";
                    bool isEye = material.name == "HumanEye";
                    if (!isSkin && !isEye) continue;
                    MaterialPropertyBlock block = new();
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(ShaderColorId, isSkin ? skin : eyes);
                    block.SetColor(ShaderBaseColorId, isSkin ? skin : eyes);
                    renderer.SetPropertyBlock(block, i);
                }
            }
            Transform socket = EnsureSocket(root, "EyebrowSocket");
            Transform group = FindDescendant(root.transform, HairGroupName);
            if (group != null) DisableRendererOn(FindDirectChild(group, "Eyebrows"), socket, null);
            ClearChildren(socket);
            SpawnStyle(eyebrows, socket);
            ApplyHairTint(socket, browColor);
        }

        /// <summary>True for the spawned head hair: anything under the hair socket. Beards and eyebrows have their own sockets.</summary>
        public static bool IsHeadHair(Transform transform)
        {
            for (Transform t = transform; t != null; t = t.parent)
            {
                if (t.name == HairSocketName) return true;
            }

            return false;
        }

        private static Transform EnsureSocket(GameObject root, string socketName)
        {
            Transform existing = FindDescendant(root.transform, socketName);
            if (existing != null)
            {
                ApplySocketTransform(existing);
                return existing;
            }

            Transform head = FindHead(root.transform);
            Transform parent = head != null ? head : root.transform;

            GameObject socket = new(socketName);
            socket.transform.SetParent(parent, false);
            ApplySocketTransform(socket.transform);
            return socket.transform;
        }

        private static void ApplySocketTransform(Transform socket)
        {
            socket.localPosition = Vector3.zero;
            socket.localRotation = StyleMeshCorrection;
            socket.localScale = Vector3.one;
        }

        private static void DisableBakedStyleMeshes(
            GameObject root,
            Transform hairSocket,
            Transform facialSocket)
        {
            Transform hairGroup = FindDescendant(root.transform, HairGroupName);
            if (hairGroup == null)
            {
                return;
            }

            DisableRendererOn(FindDirectChild(hairGroup, BakedHairName), hairSocket, facialSocket);
            DisableRendererOn(FindDirectChild(hairGroup, BakedFacialHairName), hairSocket, facialSocket);

        }

        private static void DisableRendererOn(
            Transform target,
            Transform hairSocket,
            Transform facialSocket)
        {
            if (target == null || IsUnder(target, hairSocket) || IsUnder(target, facialSocket))
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void ClearChildren(Transform socket)
        {
            if (socket == null)
            {
                return;
            }

            for (int i = socket.childCount - 1; i >= 0; i--)
            {
                socket.GetChild(i).gameObject.Dispose(true);
            }
        }

        private static void SpawnStyle(GameObject prefab, Transform socket)
        {
            if (prefab == null || socket == null)
            {
                return;
            }

            Object.Instantiate(prefab, socket, false);
        }

        private static void ApplyHairTint(Transform socket, Color color)
        {
            if (socket == null)
            {
                return;
            }

            MaterialPropertyBlock mpb = new();
            mpb.SetColor(ShaderColorId, color);
            mpb.SetColor(ShaderBaseColorId, color);

            foreach (Renderer r in socket.GetComponentsInChildren<Renderer>(true))
            {
                r.SetPropertyBlock(mpb);
            }
        }

        private static Transform FindHead(Transform root)
        {
            Transform head = FindDescendant(root, "head");
            if (head != null)
            {
                return head;
            }

            return FindDescendant(root, "Head");
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsUnder(Transform t, Transform ancestor)
        {
            if (ancestor == null)
            {
                return false;
            }

            Transform current = t.parent;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
