using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Grimwell.SessionBoard
{
    [Serializable]
    public class SceneEntry
    {
        public SceneAsset scene;
        public string suggestedOwner;
    }

    [CreateAssetMenu(menuName = "Grimwell/Session Definition")]
    public class SessionDefinition : ScriptableObject
    {
        public string sessionName = "Main Session";
        public List<SceneEntry> pieces;

        public static SessionDefinition FindFirst()
        {
            var guids = AssetDatabase.FindAssets("t:SessionDefinition");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SessionDefinition>(path);
        }
    }

}
