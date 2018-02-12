using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShadowCache : MonoBehaviour 
{
	public bool AutoRefresh = false;
	public float AngleThreshold = 8.0f;
	private Quaternion oldrotation;

	public RenderTexture shadowMap = null;
	public int m_TextureSize = 256;
	public LayerMask m_ShadowLayers = -1;

	public Camera shadowCamera = null;
	public bool IsOrthographic = false;
	private bool s_InsideRendering = false;

	private string ShadowShaderName = "ShadowMap/ShadowCaster";
	private blurImageEffect _fx;
	public bool UseBlurShadowMap = false;
	public float BlurSpreadSize = 1.2f;
	public int BlurIterations=2;

	void Start () 
	{
		InitializeCamera ();
	}

	private void InitializeCamera()
	{
		Light l = GetComponent<Light> ();
		if (!l)
			return;
		if (l.type != LightType.Directional)
			return;

		oldrotation = transform.rotation;

		if (!shadowMap)
		{
			shadowMap = RenderTexture.GetTemporary(m_TextureSize,m_TextureSize,16);
			shadowMap.name = "__ShadowMap" + GetInstanceID();
			shadowMap.isPowerOfTwo = true;
			shadowMap.hideFlags = HideFlags.DontSave;

			if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
				shadowMap.format = RenderTextureFormat.ARGB32;
			shadowMap.filterMode = FilterMode.Bilinear;
			//shadowMap.useMipMap = true;

		}



		if (!shadowCamera)
		{
			GameObject go = new GameObject("ShadowMap Camera id" + GetInstanceID() + " for " + l.GetInstanceID(), typeof(Camera));
			shadowCamera = go.GetComponent<Camera>();
			shadowCamera.enabled = false;
			shadowCamera.transform.position = transform.position;
			shadowCamera.transform.rotation = transform.rotation;
			shadowCamera.gameObject.AddComponent<FlareLayer>();

			shadowCamera.clearFlags = CameraClearFlags.SolidColor;
			shadowCamera.backgroundColor = Color.white;
			shadowCamera.targetTexture = shadowMap;
			if (SystemInfo.supportsImageEffects && UseBlurShadowMap) {
				_fx = shadowCamera.gameObject.AddComponent (typeof(blurImageEffect)) as blurImageEffect;
				_fx.BlurIterations = BlurIterations;
				_fx.BlurSpreadSize = BlurSpreadSize;
			}
			go.hideFlags = HideFlags.HideAndDontSave;
		}

		UpdateCameraModes ();
		RenderShadow ();
		MeshRenderer[] renderers = FindObjectsOfType(typeof(MeshRenderer))as MeshRenderer[];
		for (int i = 0; i < renderers.Length; ++i) {
			if (m_ShadowLayers==(m_ShadowLayers|1<<renderers[i].gameObject.layer))
			{
				Material[] materials = renderers[i].sharedMaterials;
				foreach (Material mat in materials)
				{
					if (mat.HasProperty("_ShadowMap"))
					{
						//Debug.Log(mrenderer.ReflectionTexture);
						mat.SetTexture("_ShadowMap", shadowMap);
					}
					Matrix4x4 shadowMatrix = shadowCamera.projectionMatrix ;

					if (SystemInfo.usesReversedZBuffer) {
						shadowMatrix [2, 0] = -shadowMatrix [2, 0];
						shadowMatrix [2, 1] = -shadowMatrix [2, 1];
						shadowMatrix [2, 2] = -shadowMatrix [2, 2];
						shadowMatrix [2, 3] = -shadowMatrix [2, 3];
					}
					shadowMatrix *= shadowCamera.worldToCameraMatrix;
					mat.SetMatrix ("_shadowMatrix",shadowMatrix);
				}
			}
		}
	}

	private Bounds GetBoundingBoxofSelectedObjects()
	{
		Bounds bb=new Bounds();
		MeshRenderer[] renderers = FindObjectsOfType(typeof(MeshRenderer))as MeshRenderer[];
		List<Bounds> bounds = new List<Bounds> ();
		for (int i = 0; i < renderers.Length; ++i) {
			if (m_ShadowLayers==(m_ShadowLayers|1<<renderers[i].gameObject.layer))
			{
				Bounds b = renderers[i].bounds;
				bounds.Add (b);
			}
		}

		if (bounds.Count > 0) {
			bb = bounds[0];
			if(bounds.Count>1)
			{
				for (int j = 1; j < bounds.Count; ++j) {
					bb.Encapsulate(bounds[j]);
				}
			}
		}

		return bb;
	}

	private void UpdateCameraModes()
	{
		if (!shadowCamera)
			return;
		Bounds bb=GetBoundingBoxofSelectedObjects();
		//Debug.Log (bb);
		float maxlength = bb.extents.magnitude;
		//maxlength = Mathf.Max (Mathf.Max (bb.extents.x, bb.extents.y), bb.extents.z);
		if (maxlength == 0)
			return;
		float distance = maxlength+150;
		Vector3 center = bb.center;
		shadowCamera.orthographic = IsOrthographic;
		if(IsOrthographic)
			shadowCamera.orthographicSize = maxlength;
		else
			shadowCamera.fieldOfView = 50;
		shadowCamera.transform.position = center - transform.forward * distance;
		shadowCamera.transform.rotation = transform.rotation;
		//transform.position=center - shadowCamera.transform.forward * distance;
		shadowCamera.farClipPlane = distance+maxlength;
		shadowCamera.nearClipPlane = 150;
		shadowCamera.targetTexture = shadowMap;
		_fx = shadowCamera.GetComponent (typeof(blurImageEffect)) as blurImageEffect;
		if (SystemInfo.supportsImageEffects && UseBlurShadowMap) {
			if (!_fx) {
				_fx = shadowCamera.gameObject.AddComponent (typeof(blurImageEffect)) as blurImageEffect;
				_fx.BlurIterations = BlurIterations;
				_fx.BlurSpreadSize = BlurSpreadSize;
			} else
				_fx.enabled = true;
		} else {
			if(_fx)
				_fx.enabled = false;
		}
	}

	private void RenderShadow()
	{
		if (!shadowCamera)
			return;
		if (s_InsideRendering)
			return;
		s_InsideRendering = true;

		shadowCamera.SetReplacementShader(Shader.Find(ShadowShaderName),"RenderType");

		shadowCamera.cullingMask = ~(1 << 4) & m_ShadowLayers.value;
		shadowCamera.Render();
		s_InsideRendering = false;
	}

	//Manual update shader map
	public void RefreshShadowMap()
	{
		UpdateCameraModes ();
		RenderShadow ();
	}

	// Update is called once per frame
	void Update () {
		if (!shadowCamera||!shadowMap)
			InitializeCamera ();
		//Auto Update Shadow Map
		if (AutoRefresh) {
			if (AngleThreshold > 0) {
				float angle = Quaternion.Angle (transform.rotation, oldrotation);
				if (AngleThreshold < angle) {
					UpdateCameraModes ();
					RenderShadow ();
					oldrotation = transform.rotation;
				}
			} else {
				UpdateCameraModes ();
				RenderShadow ();
			}
		}
		if (shadowCamera) {
			MeshRenderer[] renderers = FindObjectsOfType (typeof(MeshRenderer))as MeshRenderer[];
			for (int i = 0; i < renderers.Length; ++i) {
				if (m_ShadowLayers == (m_ShadowLayers | 1 << renderers [i].gameObject.layer)) {
					Material[] materials = renderers [i].sharedMaterials;
					foreach (Material mat in materials) {
						Matrix4x4 shadowMatrix = shadowCamera.projectionMatrix;

						if (SystemInfo.usesReversedZBuffer) {
							shadowMatrix [2, 0] = -shadowMatrix [2, 0];
							shadowMatrix [2, 1] = -shadowMatrix [2, 1];
							shadowMatrix [2, 2] = -shadowMatrix [2, 2];
							shadowMatrix [2, 3] = -shadowMatrix [2, 3];
						}
						shadowMatrix *= shadowCamera.worldToCameraMatrix;
						mat.SetMatrix ("_shadowMatrix", shadowMatrix);
					}
				}
			}
		}

	}

	void OnDisable()
	{
		if (shadowMap)
		{
			RenderTexture.ReleaseTemporary(shadowMap);
			shadowMap = null;
		}

		if (shadowCamera)
		{
			DestroyImmediate(shadowCamera.gameObject);
			shadowCamera = null;
		}
	}


}
