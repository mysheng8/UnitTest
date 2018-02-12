using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tile
{
	public Vector2 Pos{ get; set;}
	public Vector2 Size{ get; set;}
	public List<GameObject> Objs;
	public tile(Vector2 p, Vector2 s)
	{
		Pos = p;
		Size = s;
		Objs = new List<GameObject>();
	}
	public bool Contain(Vector2 oPos, Vector2 oSize)
	{
		Rect self = new Rect(Pos,Size);
		Rect obj = new Rect (oPos, oSize);
		return self.Overlaps (obj);
	}
}


public class shadowMapRenderer
{
	public bool enable=true;
	public RenderTexture shadowMap = null;
	public Camera shadowCamera = null;
	public int m_TextureSize = 1024;
	public LayerMask m_ShadowLayers = -1;
	private bool s_InsideRendering = false;
	private string ShadowShaderName = "ShadowMap/ShadowCaster";
	private blurImageEffect _fx;

	public bool UseBlurShadowMap = false;
	public float BlurSpreadSize = 1.2f;
	public int BlurIterations=2;


	public shadowMapRenderer (int textureSize,LayerMask shadowLayers){
		m_ShadowLayers = shadowLayers;
		m_TextureSize = textureSize;
	}

	public void InitializeRenderer()
	{
		if (!shadowMap) {
			shadowMap = RenderTexture.GetTemporary (m_TextureSize, m_TextureSize, 16);
			shadowMap.name = "__ShadowMap";
			shadowMap.isPowerOfTwo = true;
			shadowMap.hideFlags = HideFlags.DontSave;

			if (SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.ARGB32))
				shadowMap.format = RenderTextureFormat.ARGB32;
			shadowMap.filterMode = FilterMode.Bilinear;
			//shadowMap.useMipMap = true;
		}

		if (!shadowCamera) {
			GameObject go = new GameObject ("__ShadowMap Camera", typeof(Camera));
			shadowCamera = go.GetComponent<Camera> ();
			shadowCamera.enabled = false;
			shadowCamera.gameObject.AddComponent<FlareLayer> ();
			shadowCamera.cullingMask = ~(1 << 4) & m_ShadowLayers.value;
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
	}

	public void UpdateCamera(Vector3 lookat, float distance, Vector3 target,float size, float depth)
	{
		if (!shadowCamera)
			return;
		Vector3 center = target;
		shadowCamera.orthographic = true;
		shadowCamera.orthographicSize = size;
		shadowCamera.transform.position = center - lookat * distance;
		shadowCamera.transform.rotation = Quaternion.LookRotation(lookat);
		shadowCamera.farClipPlane = distance + depth;
		shadowCamera.nearClipPlane = distance - depth;
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
			if (_fx)
				_fx.enabled = false;
		}
	}

	public void RenderShadow()
	{
		if (!enable)
			return;
		if (!shadowCamera)
			return;
		if (s_InsideRendering)
			return;
		s_InsideRendering = true;
		shadowCamera.SetReplacementShader(Shader.Find(ShadowShaderName),"RenderType");

		shadowCamera.Render();
		s_InsideRendering = false;
	}

	~shadowMapRenderer()
	{
		if (shadowMap)
		{
			RenderTexture.ReleaseTemporary(shadowMap);
			shadowMap = null;
		}

		if (shadowCamera)
		{
			GameObject.DestroyImmediate(shadowCamera.gameObject);
			shadowCamera = null;
		}
	}
}


public static class yMathf
{
	public static bool intersect(Vector2 _centor, float _radius, Rect _rect)
	{
		if (_rect.Contains (_centor))
			return true;
		if (intersectCircle (_centor, _radius, _rect.xMin, _rect.yMin))
			return true;
		if (intersectCircle (_centor, _radius, _rect.xMax, _rect.yMin))
			return true;
		if (intersectCircle (_centor, _radius, _rect.xMin, _rect.yMax))
			return true;
		if (intersectCircle (_centor, _radius, _rect.xMax, _rect.yMax))
			return true;
		if(_centor.y<_rect.yMax&&_centor.y>_rect.yMin)
		{
			if ((_centor.x - _rect.xMax) < _radius || (_rect.xMin - _centor.x) < _radius)
				return true;
		}
		if(_centor.x<_rect.xMax&&_centor.x>_rect.xMin)
		{
			if ((_centor.y - _rect.yMax) < _radius || (_rect.yMin - _centor.y) < _radius)
				return true;
		}
		return false;
	} 

	public static bool intersectCircle(Vector2 _centor, float _radius, float _x, float _y)
	{
		Vector2 point = new Vector2 (_x, _y);
		float dis = (_centor - point).sqrMagnitude;
		return dis < _radius * _radius;	
	}
}


public class tiledShadowCache: MonoBehaviour 
{

	public Vector2 tileSize;
	public LayerMask m_ShadowLayers = -1;
	public Transform target;
	public float radius;
	public int m_TextureSize = 1024;
	public float focusDistance = 150;
	public float depth = 150;
	public float updateIntervalTime = 5;

	private int _numX;
	private int _numY;
	private float _startX;
	private float _startY;
	private float _height;
	private List<tile> _tiles;
	private List<shadowMapRenderer> _renderers;
	private Dictionary<int, int> _curTilesRenderers;
	private float _currentTime = 0;

	void initialize()
	{
		Light l = GetComponent<Light> ();
		if (!l)
			return;
		if (l.type != LightType.Directional)
			return;


		_tiles = new List<tile> ();
		_renderers = new List<shadowMapRenderer> ();
		_curTilesRenderers = new Dictionary<int, int> ();


		Bounds bb = GetBoundingBoxofSelectedObjects ();
		_numX = Mathf.CeilToInt (bb.extents.x*2 / tileSize.x);
		_numY = Mathf.CeilToInt (bb.extents.z*2 / tileSize.y);

		_startX = (float)(bb.center.x - tileSize.x * 0.5 * _numX);
		_startY = (float)(bb.center.z - tileSize.y * 0.5 * _numY);

		_height = (float)bb.center.y;
		//initialize tiles
		for (int j = 0; j < _numY; ++j) {
			for (int i = 0; i < _numX; ++i) {
				float posX = (float)(_startX + tileSize.x * 0.5 * (i + 1));
				float posY = (float)(_startY + tileSize.y * 0.5 * (j + 1));
				Vector2 pos = new Vector2(posX, posY);
				tile t = new tile (pos,tileSize);
				_tiles.Add (t);
			}
		}
		//initialize tiles objects list
		MeshRenderer[] renderers = FindObjectsOfType(typeof(MeshRenderer))as MeshRenderer[];
		for (int i = 0; i < renderers.Length; ++i) {
			if (m_ShadowLayers==(m_ShadowLayers|1<<renderers[i].gameObject.layer))
			{
				Bounds b = renderers[i].bounds;
				Vector2 opos = new Vector2 (b.center.x, b.center.z);
				Vector2 osize = new Vector2 (b.extents.x * 2, b.extents.z * 2);
				foreach (tile it in _tiles)
				{
					if (it.Contain (opos, osize))
						it.Objs.Add (renderers [i].gameObject);
				}
			}
		}
	}

	void UpdateCurrentList()
	{
		Vector2 pos = new Vector2 (target.transform.position.x, target.transform.position.y);

		foreach (int k in _curTilesRenderers.Keys) {
			Rect r = new Rect (_tiles[k].Pos, _tiles[k].Size);
			if (!yMathf.intersect (pos, radius, r)) {
				int rindex = _curTilesRenderers [k];
				_renderers [rindex].enable = false;
				_curTilesRenderers.Remove(k);
			}
		}


		for(int i=0;i<_tiles.Count;++i)
		{
			
			Rect r = new Rect (_tiles[i].Pos, _tiles[i].Size);
			if (_curTilesRenderers.ContainsKey (i))
				continue;
			if(yMathf.intersect(pos,radius,r))
			{
				int newindex = -1;
				for(int j=0;j< _renderers.Count;++j)
				{
					
					if (_renderers [j].enable == false) {
						newindex = j;
						break;
					}
				}
				Vector3 target = new Vector3 (_tiles [i].Pos.x, _height, _tiles [i].Pos.y);
				if(newindex<0)
				{
					shadowMapRenderer renderer = new shadowMapRenderer (m_TextureSize,m_ShadowLayers);
					renderer.InitializeRenderer ();
					_renderers [newindex].UpdateCamera (transform.forward, focusDistance, target,tileSize.magnitude, depth);
					renderer.RenderShadow ();
				}
				else{
					_curTilesRenderers.Add (i, newindex);
					_renderers [newindex].enable = true;
					_renderers [newindex].UpdateCamera (transform.forward, focusDistance, target,tileSize.magnitude, depth);
					_renderers [newindex].RenderShadow ();
				}
			}
		}
	}

	void Update()
	{
		_currentTime += Time.deltaTime;
		if (_currentTime > updateIntervalTime) {
			_currentTime = 0;
			UpdateCurrentList ();
		}
		foreach(int k in _curTilesRenderers.Keys) {
			int rindex = _curTilesRenderers [k];
			UpdateMaterials (_tiles[k], _renderers[rindex]);
		}
	}

	public tile GetTile(int x, int y)
	{
		return _tiles[x+y*_numX];
	}

	public tile GetTileFromPos(float x, float y)
	{
		int indexX = Mathf.CeilToInt ((x-_startX)/tileSize.x);
		int indexY = Mathf.CeilToInt ((y-_startY)/tileSize.y);
		return GetTile (indexX, indexY);
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

	private void UpdateMaterials(tile t, shadowMapRenderer r)
	{
		foreach (GameObject o in t.Objs)
		{
			MeshRenderer renderer = o.GetComponent<Renderer>() as MeshRenderer;
			if (renderer) {
				Material[] materials = renderer.sharedMaterials;
				foreach (Material mat in materials) {
					if (mat.HasProperty ("_ShadowMap")) {
						//Debug.Log(mrenderer.ReflectionTexture);
						mat.SetTexture ("_ShadowMap", r.shadowMap);
					}
					Matrix4x4 shadowMatrix = r.shadowCamera.projectionMatrix;

					if (SystemInfo.usesReversedZBuffer) {
						shadowMatrix [2, 0] = -shadowMatrix [2, 0];
						shadowMatrix [2, 1] = -shadowMatrix [2, 1];
						shadowMatrix [2, 2] = -shadowMatrix [2, 2];
						shadowMatrix [2, 3] = -shadowMatrix [2, 3];
					}
					shadowMatrix *= r.shadowCamera.worldToCameraMatrix;
					mat.SetMatrix ("_shadowMatrix", shadowMatrix);
				}
			}
		}
	}
}

