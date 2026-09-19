using TMPro;
using UnityEngine;

namespace VRShooting.Unity.UI
{
    /// <summary>Build-safe art references shared by all runtime-authored training screens.</summary>
    public sealed class TacticalUITheme : ScriptableObject
    {
        public TMP_FontAsset Font;
        public Sprite Hall;
        public Sprite Trench;
        public Sprite Urban;
        public Sprite[] Floors;

        static TacticalUITheme current;
        public static TacticalUITheme Current
        {
            get { if (current == null) current = Resources.Load<TacticalUITheme>("UI/Tactical/Theme"); return current; }
        }

        public Sprite Map(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == "trench-a" || id == "trench-a.map") return Trench;
            if (id == "urban-a" || id == "urban-a.street") return Urban;
            for (var i = 0; Floors != null && i < Floors.Length; i++)
                if (id == "urban-" + (i + 1) + "f" || id == "urban-a.floor-" + (i + 1) || id == "urban-a.floor-" + (i + 1) + ".map") return Floors[i];
            return null;
        }
    }
}
