// Reference: URP17 Samples
#if UNITY_6000_3_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Chris.Gameplay.Graphics.Editor
{
// This script is for combining multiple GraphicsStateCollection files into one.
// Select multiple GraphicsStateCollection files in the ProjectView,
// and right click to combine the contents into one.
// The first selected file will be the result file.
    public static class GraphicsStateCollectionCombiner
    {
        private static GraphicsStateCollection[] s_Selected;
        
        private const string k_RightClickMenu = "Assets/Combine GraphicsStateCollections";

        // Enable the menu option if there are more than 1 GraphicsStateCollection files selected.
        [MenuItem(k_RightClickMenu, true)]
        private static bool ShowRightClickOption()
        {
            s_Selected = Selection.GetFiltered<GraphicsStateCollection>(SelectionMode.Assets);
            return s_Selected.Length > 1;
        }

        // Combine the selected GraphicsStateCollections files.
        [MenuItem(k_RightClickMenu)]
        private static void DoCombine()
        {
            int addedVariantCount = 0;
            int addedGfxStateCount = 0;
            int combinedCollectionCount = 0;

            // Selection.activeObject is the first selected object, so use it as the result file.
            GraphicsStateCollection result = Selection.activeObject as GraphicsStateCollection;

            // In case it is null, use the first one.
            if (result == null)
            {
                result = s_Selected[0];
            }

            string resultPath = AssetDatabase.GetAssetPath(result);

            for (int i = 0; i < s_Selected.Length; i++)
            {
                GraphicsStateCollection collection = s_Selected[i];

                // Skip the result file itself.
                if (collection == result)
                {
                    continue;
                }

                // Skip this collection file if any setting does not match.
                if (collection.runtimePlatform != result.runtimePlatform ||
                    collection.graphicsDeviceType != result.graphicsDeviceType ||
                    collection.qualityLevelName != result.qualityLevelName)
                {
                    string collectionPath = AssetDatabase.GetAssetPath(collection);
                    Debug.LogError(string.Concat("Skip combining ", ColorString(collectionPath), " into ",
                            ColorString(resultPath), " because platform or gfx api or quality level does not match."),
                        collection);
                    continue;
                }

                // Add the variants and graphics states into the result collection file.
                List<GraphicsStateCollection.ShaderVariant> variants = new();
                collection.GetVariants(variants);
                foreach (GraphicsStateCollection.ShaderVariant v in variants)
                {
                    Shader shader = v.shader;
                    PassIdentifier passId = v.passId;
                    LocalKeyword[] keywords = v.keywords;

                    // Add the variant.
                    if (result.AddVariant(shader, passId, keywords))
                    {
                        addedVariantCount++;
                    }

                    // Also add the graphics states from the variant.
                    List<GraphicsStateCollection.GraphicsState> states = new();
                    collection.GetGraphicsStatesForVariant(v, states);
                    foreach (GraphicsStateCollection.GraphicsState s in states)
                    {
                        if (result.AddGraphicsStateForVariant(shader, passId, keywords, s))
                        {
                            addedGfxStateCount++;
                        }
                    }
                }

                // Delete the collection file.
                string path = AssetDatabase.GetAssetPath(collection);
                AssetDatabase.DeleteAsset(path);
                combinedCollectionCount++;
            }

            // Save the result collection file.
            result.SaveToFile(resultPath);

            Debug.Log(string.Concat(
                "Combined ", ColorString(combinedCollectionCount.ToString()),
                " GraphicsStateCollections into ", ColorString(resultPath),
                ". Added ", ColorString(addedVariantCount.ToString()),
                " variants and ", ColorString(addedGfxStateCount.ToString()),
                " graphics states."), result);
        }

        private static string ColorString(string text)
        {
            return string.Concat("<color=#00ffff>", text, "</color>");
        }
    }
}
#endif