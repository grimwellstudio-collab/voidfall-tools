using UnityEditor;

namespace Grimwell.SessionBoard
{
    class ScriptEditTracker : AssetPostprocessor
    {
        // a hand edit touches a few scripts; a clone/branch-switch imports dozens at once
        // and would falsely credit the whole codebase to whoever opened the project
        const int BulkImportThreshold = 15;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var scriptCount = 0;
            foreach (var path in importedAssets)
                if (path.StartsWith("Assets/") && path.EndsWith(".cs")) scriptCount++;
            if (scriptCount == 0 || scriptCount > BulkImportThreshold) return;

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
