using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Reflect : MonoBehaviour {

    private MirrorRenderer mrenderer;

    // Use this for initialization
    void Start() {
        mrenderer = Camera.main.GetComponent<MirrorRenderer>();
        if (mrenderer == null)
            return;
        Material[] materials = GetComponent<Renderer>().sharedMaterials;
        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_MirrorTex"))
            {
                //Debug.Log(mrenderer.ReflectionTexture);
                mat.SetTexture("_MirrorTex", mrenderer.ReflectionTexture);
            }
        }
    }
}
