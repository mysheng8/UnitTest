using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blurImageEffect : MonoBehaviour {

	private string ShaderName="ShadowMap/FastBlur";
	public Shader CurShader;
	private Material CurMaterial;

	public float BlurSpreadSize = 3.0f;
	public int BlurIterations=3;
	// Use this for initialization
	void Start () {
		CurShader = Shader.Find (ShaderName);
		if (!SystemInfo.supportsImageEffects) {
			enabled = false;
			return;
		}
	}
	Material material
	{
		get
		{ 
			if (CurMaterial == null) {
				CurMaterial = new Material (CurShader);
				CurMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return CurMaterial;
		}

	}

	void OnRenderImage(RenderTexture sourceTexture,RenderTexture destTexture)
	{
		if (CurShader) {
			sourceTexture.filterMode = FilterMode.Bilinear;

			int renderWidth = sourceTexture.width;
			int renderHeight = sourceTexture.height;

			RenderTexture renderBuffer = RenderTexture.GetTemporary (renderWidth, renderHeight, 0, sourceTexture.format);
			renderBuffer.filterMode = FilterMode.Bilinear;
			Graphics.Blit (sourceTexture, renderBuffer);

			float blursize = 1 / renderWidth;
			blursize *= BlurSpreadSize;
			material.SetFloat ("_BlurSpreadSize", BlurSpreadSize);
			for (int i = 0; i < BlurIterations; ++i) {
				RenderTexture tempBuffer = RenderTexture.GetTemporary (renderWidth, renderHeight, 0, sourceTexture.format);
				Graphics.Blit (renderBuffer, tempBuffer, material, 0);
				RenderTexture.ReleaseTemporary (renderBuffer);
				renderBuffer = tempBuffer;
				tempBuffer = RenderTexture.GetTemporary (renderWidth, renderHeight, 0, sourceTexture.format);
				Graphics.Blit (renderBuffer, tempBuffer, material, 1);
				RenderTexture.ReleaseTemporary (renderBuffer);
				renderBuffer = tempBuffer;
			}
			Graphics.Blit (renderBuffer, destTexture);
			RenderTexture.ReleaseTemporary (renderBuffer);
		} 
		else {
			Graphics.Blit (sourceTexture, destTexture);
		}
	}
}

