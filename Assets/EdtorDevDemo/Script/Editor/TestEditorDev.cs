using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Nt.Editor
{
    public class PreviewScene : SceneView
    { 
    }


    public class CustomPreviewSceneStage : PreviewSceneStage
    {
        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Test");
        }

        protected override void OnCloseStage()
        {
            Debug.Log("close Preview Stage");
            base.OnCloseStage();
        }

        protected override bool OnOpenStage()
        {
            Debug.Log("OpenStage Preview Stage");
            return base.OnOpenStage();
        }


    }
}

