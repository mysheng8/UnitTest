using UnityEngine;
using System.Collections;


public class MirrorRenderer : MonoBehaviour
{
    public bool IsStatic = false;
    public RenderTexture ReflectionTexture = null;
    public int m_TextureSize = 256;
    public float m_ClipPlaneOffset = -0.07f;
    public float m_Height = 0f;
    public bool IsOrthographic = false;
    public LayerMask m_ReflectLayers = -1;

    private int m_OldReflectionTextureSize = 0;
    private static bool s_InsideRendering = false;
    private Camera reflectionCamera = null;
	private Camera cam=null;

    void Awake()
    {
        cam = Camera.main;

		if (!cam) 
			return;
		
		if (!ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize)
		{
			if (ReflectionTexture)
				DestroyImmediate(ReflectionTexture);
			ReflectionTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
			ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
			ReflectionTexture.isPowerOfTwo = true;
			ReflectionTexture.hideFlags = HideFlags.DontSave;
			ReflectionTexture.useMipMap = true;
			if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565))
				ReflectionTexture.format = RenderTextureFormat.RGB565;

			m_OldReflectionTextureSize = m_TextureSize;
		}

		if (!reflectionCamera)
		{
			GameObject go = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + cam.GetInstanceID(), typeof(Camera), typeof(Skybox));
			reflectionCamera = go.GetComponent<Camera>();
			reflectionCamera.enabled = false;
			reflectionCamera.transform.position = transform.position;
			reflectionCamera.transform.rotation = transform.rotation;
			reflectionCamera.gameObject.AddComponent<FlareLayer>();
			reflectionCamera.targetTexture = ReflectionTexture;
			go.hideFlags = HideFlags.HideAndDontSave;
		}

		UpdateCameraModes(cam, reflectionCamera);

		Render ();
    }

    void Update()
    {
		if (IsStatic)
			return;
		Render ();
    }

	void Render()
	{
		if (!cam||!reflectionCamera)
			return;

		if (s_InsideRendering)
			return;
		s_InsideRendering = true;

		Vector3 normal = Vector3.up;
		Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, -m_Height);
		Matrix4x4 reflection = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflection, reflectionPlane);
		reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

		Vector4 clipPlane = CameraSpacePlane(reflectionCamera, normal, 1.0f, (m_Height + m_ClipPlaneOffset));
		Matrix4x4 projection = cam.projectionMatrix;
		CalculateObliqueMatrix(ref projection, clipPlane);
		reflectionCamera.projectionMatrix = projection;
		reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value;

		GL.invertCulling= true;
		reflectionCamera.SetReplacementShader(Shader.Find("Reflection/Reflected"),"RenderType");
		reflectionCamera.Render();
		GL.invertCulling = false;

		s_InsideRendering = false;
	}

    void OnDisable()
    {
        if (ReflectionTexture)
        {
            DestroyImmediate(ReflectionTexture);
            ReflectionTexture = null;
        }
		/*
		if (reflectionCamera)
		{
			DestroyImmediate(reflectionCamera);
			reflectionCamera = null;
		}*/
    }


    private void UpdateCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
            return;

        dest.clearFlags = CameraClearFlags.SolidColor;
		dest.backgroundColor = Color.clear;

        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
        dest.renderingPath = src.renderingPath;
    }

    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }

    private Vector4 CameraSpacePlane(Camera cam,Vector3 normal, float sideSign, float offset)
    {
        Vector3 offsetPos = normal * offset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(
            sgn(clipPlane.x),
            sgn(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));

        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}


