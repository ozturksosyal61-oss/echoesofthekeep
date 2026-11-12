using UnityEditor;
using UnityEngine;

namespace CoopGame.EditorTools
{
    public static partial class CoopSetup
    {
        /// <summary>Okunabilir mektup (LoreTexts id): katlanmış kâğıt + mum mühür, etkileşimde okuma ekranı açılır (yerel, ağsız).</summary>
        static GameObject Letter(Transform parent, Vector3 pos, float yaw, int id)
        {
            var go = new GameObject("LoreLetter" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Slab("Sheet", go.transform, new Vector3(0, 0.003f, 0), new Vector3(0.21f, 0.006f, 0.29f), K.paperA, 1f, false);
            Slab("Fold", go.transform, new Vector3(0.01f, 0.008f, -0.06f), new Vector3(0.2f, 0.004f, 0.15f), K.paperB, 1f, false).transform.localRotation = Quaternion.Euler(0, 4f, 0);
            Prim("Seal", go.transform, PrimitiveType.Cylinder, new Vector3(0.03f, 0.012f, -0.02f), new Vector3(0.045f, 0.004f, 0.045f), K.leatherRed);
            var c = go.AddComponent<BoxCollider>(); c.center = new Vector3(0, 0.08f, 0); c.size = new Vector3(0.42f, 0.16f, 0.48f);
            var it = go.AddComponent<Interactable>(); it.Key = 9000 + id; it.Label = "Mektubu oku";
            var lt = go.AddComponent<LoreLetter>();
            var so = new SerializedObject(lt); so.FindProperty("id").intValue = id; so.ApplyModifiedPropertiesWithoutUndo();
            ProbeLit(go);
            return go;
        }
    }
}
