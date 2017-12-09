using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

namespace XNodeEditor {
    public class NodeEditorAssetModProcessor : UnityEditor.AssetModificationProcessor {
        public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options) {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (!(obj is UnityEditor.MonoScript)) return AssetDeleteResult.DidNotDelete;

            UnityEditor.MonoScript script = obj as UnityEditor.MonoScript;
            System.Type scriptType = script.GetClass();

            if (scriptType != typeof(XNode.Node) && !scriptType.IsSubclassOf(typeof(XNode.Node))) return AssetDeleteResult.DidNotDelete;

            //Find ScriptableObjects using this script
            string[] guids = AssetDatabase.FindAssets("t:" + scriptType);
            for (int i = 0; i < guids.Length; i++) {
                string assetpath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Object[] objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetpath);
                for (int k = 0; k < objs.Length; k++) {
                    XNode.Node node = objs[k] as XNode.Node;
                    if (node.GetType() == scriptType) {
                        if (node != null && node.graph != null) {
                            Debug.LogWarning(node.name + " of " + node.graph + " depended on deleted script and has been removed automatically.", node.graph);
                            node.graph.RemoveNode(node);
                            GameObject.DestroyImmediate(node , true);
                        }
                    }
                }

            }
            return AssetDeleteResult.DidNotDelete;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainAsset==null) continue;
                if (mainAsset is NodeGraph || mainAsset.GetType().IsSubclassOf(typeof(NodeGraph)))
                {
                    NodeGraph nodeGraph = (NodeGraph) AssetDatabase.LoadMainAssetAtPath(path);
                    CheckGraphNodes(nodeGraph);
                }
            }
            return paths;
        }

        static void CheckGraphNodes(NodeGraph nodeGraph)
        {
            string filePath = AssetDatabase.GetAssetPath(nodeGraph);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(filePath);
            HashSet<Node> savedNodes = new HashSet<Node>();
            for (int i = 0; i < assets.Length; i++)
            {
                Object asset = assets[i];
                if (AssetDatabase.IsMainAsset(asset)) continue;
                if (asset is Node || asset.GetType().IsSubclassOf(typeof(Node))) savedNodes.Add((Node)asset);
            }
            HashSet<Node> referedNodes = new HashSet<Node>(nodeGraph.nodes);
            if (!referedNodes.SetEquals(savedNodes))
            {
                foreach (Node node in savedNodes.Except(referedNodes))
                {
                    GameObject.DestroyImmediate(node, true);
                }
                foreach (Node node in referedNodes.Except(savedNodes))
                {
                    AssetDatabase.AddObjectToAsset(node, nodeGraph);
                }
            }
        }
    }
}