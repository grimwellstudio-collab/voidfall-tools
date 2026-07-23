using UnityEditor;

namespace Grimwell.SessionBoard
{
    class ScriptEditTracker : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (!path.StartsWith("Assets/") || !path.EndsWith(".cs")) continue;
                int lines;
                try { lines = System.IO.File.ReadAllLines(path).Length; }
                catch (System.Exception) { lines = 0; }
                PresencePublisher.AddScriptEdit(lines);
            }
        }
    }
}
