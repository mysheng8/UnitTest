using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class daylight : MonoBehaviour {

	public AnimationCurve dayIntensity;
	public Gradient dayColor;
	public float daytime = 3;
	public Vector3 northDir;
	private bool isNight=false;
	private bool lightswitch=false;
	private bool change=false;
	private float time = 0;
	// Update is called once per frame
	Light light;
	private Quaternion rotation;

	void Start()
	{
		light = GetComponent<Light> ();
		if (light) {
			Vector3 up = new Vector3 (0, 1, 0);
			Vector3 lookat = -(Vector3.Normalize(Vector3.Cross (northDir,up)));
			rotation = Quaternion.LookRotation(lookat);

		}
		SetDayNight (1);
	}
	void SetDayNight(float v)
	{
		MeshRenderer[] renderers = FindObjectsOfType (typeof(MeshRenderer))as MeshRenderer[];
		for (int i = 0; i < renderers.Length; ++i) {
			Material[] materials = renderers [i].sharedMaterials;
			foreach (Material mat in materials) {
				if(mat.HasProperty("_DayNight"))
					mat.SetFloat("_DayNight", v);
			}
		}

	}

	void Update () {
		if (light) {
			time += Time.deltaTime ;
			float t=time / daytime;
			float v = dayIntensity.Evaluate (t );
			light.intensity = v;
			Color c = dayColor.Evaluate (t %1);
			light.color = c;
			Quaternion rot = Quaternion.AngleAxis ( -t*360, northDir);
			transform.rotation = rot*rotation;

			if ((t % 1) > 0.5f) {
				isNight = true;
			} else {
				isNight = false;
			}

			if (v < 0.9f) {
				if (!lightswitch)
					change = true;
				else
					change = false;
				lightswitch = true;
			} else {
				if (lightswitch)
					change = true;
				else
					change = false;
				lightswitch = false;
			}

			ShadowCache renderer = GetComponent (typeof(ShadowCache)) as ShadowCache;
			if (renderer) {
				if (isNight)
					renderer.AutoRefresh = false;
				else
					renderer.AutoRefresh = true;
			}
			if (change) {
				if (lightswitch) {
					SetDayNight (1);
				} else {
					SetDayNight (0);
				}
			}
		}
	}
}
