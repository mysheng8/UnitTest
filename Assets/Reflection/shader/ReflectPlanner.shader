Shader "Reflection/ReflectPlanner"
{
	Properties
	{
		_Scale("Reflect Power", Range(0,1))=0.5
		_MirrorTex("_Mirror Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 100
		ZWrite Off
		Blend SrcAlpha One
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 proj:TEXCOORD0;
				float4 vertex : SV_POSITION;

			};

			sampler2D _MirrorTex;
			float _Scale;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.proj = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 projUV=float4(0,0,0,0);
				projUV.xy = i.proj.xy / i.proj.w;
				projUV.w=3;
				// sample the texture
				fixed4 col = tex2D(_MirrorTex, projUV.xy);
				fixed4 colblur = tex2Dbias(_MirrorTex, projUV);

				col = lerp(col,colblur,1-col.a);
				col.a*=_Scale;
				return col;
			}
			ENDCG
		}
	}
}
