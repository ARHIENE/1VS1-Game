using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimatorFixer : MonoBehaviour
{
    [MenuItem("Tools/Fix Animator and Clips")]
    public static void FixAll()
    {
        string animatorPath = "Assets/Resources/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
        if (controller == null) { Debug.LogError("Animator not found"); return; }

        // Fix Clips Loop Time
        string[] fbxPaths = {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Crouch/HumanM@Crouch01_Idle.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Crouch/CrouchWalk/HumanM@Crouch01_Walk_Forward.fbx"
        };

        foreach (string path in fbxPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] animations = importer.clipAnimations;
                if (animations == null || animations.Length == 0) animations = importer.defaultClipAnimations;
                
                foreach (var anim in animations)
                {
                    anim.loopTime = true;
                    anim.loopPose = true;
                }
                importer.clipAnimations = animations;
                importer.SaveAndReimport();
                Debug.Log($"Fixed loops for: {path}");
            }
        }

        // Re-verify Blend Tree
        var rootStateMachine = controller.layers[0].stateMachine;
        foreach (var state in rootStateMachine.states)
        {
            if (state.state.name == "Movement")
            {
                // Always rebuild to ensure it's correct
                Debug.Log("Rebuilding Movement BlendTree...");
                BlendTree bt = new BlendTree();
                bt.name = "MovementBT";
                bt.blendType = BlendTreeType.Simple1D;
                bt.blendParameter = "Speed";
                bt.useAutomaticThresholds = false;

                bt.AddChild(GetClip(fbxPaths[0], "Idle01"), 0f);
                bt.AddChild(GetClip(fbxPaths[1], "Walk01_Forward"), 0.5f);
                bt.AddChild(GetClip(fbxPaths[2], "Run01_Forward"), 1.0f);

                AssetDatabase.AddObjectToAsset(bt, controller);
                state.state.motion = bt;
            }
            
            if (state.state.name == "Crouch")
            {
                Debug.Log("Rebuilding Crouch BlendTree...");
                BlendTree bt = new BlendTree();
                bt.name = "CrouchBT";
                bt.blendType = BlendTreeType.Simple1D;
                bt.blendParameter = "Speed";
                bt.useAutomaticThresholds = false;

                bt.AddChild(GetClip(fbxPaths[3], "Crouch01_Idle"), 0f);
                bt.AddChild(GetClip(fbxPaths[4], "Crouch01_Walk_Forward"), 0.5f);

                AssetDatabase.AddObjectToAsset(bt, controller);
                state.state.motion = bt;
            }
}

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Animator and Clips fixed.");
    }

    private static AnimationClip GetClip(string path, string name)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip && asset.name.Contains(name) && !asset.name.Contains("__preview__")) 
                return asset as AnimationClip;
        }
        return null;
    }
}
