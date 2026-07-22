using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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

    public static class SessionDefinitionMenu
    {
        [MenuItem("Grimwell/Create Demo Session")]
        public static void CreateDemoSession()
        {
            const string folder = "Assets/DemoSession";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "DemoSession");

            var names = new[] { "A", "B", "C" };
            var xPositions = new[] { -3f, 0f, 3f };
            var scenes = new SceneAsset[names.Length];

            for (var i = 0; i < names.Length; i++)
            {
                var pieceName = "DemoPiece_" + names[i];
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = pieceName;
                cube.transform.position = new Vector3(xPositions[i], 0f, 0f);

                var scenePath = folder + "/" + pieceName + ".unity";
                EditorSceneManager.SaveScene(scene, scenePath);
                scenes[i] = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            }

            var definition = ScriptableObject.CreateInstance<SessionDefinition>();
            definition.sessionName = "Demo Session";
            definition.pieces = scenes.Select(s => new SceneEntry { scene = s, suggestedOwner = "" }).ToList();

            var assetPath = folder + "/DemoSession.asset";
            AssetDatabase.CreateAsset(definition, assetPath);
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorGUIUtility.PingObject(definition);
            Selection.activeObject = definition;
        }
    }
}
