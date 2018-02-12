
Shader "ShadowMap/Object"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ShadowMap("Shadow Map", 2D) = "white" {}
		_DayNight("DayNight",Range(0,1))=1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{"LightMode"="ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON//for lightmap
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#define UNITY_NO_RGBM
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 normal: NORMAL;
				#ifndef LIGHTMAP_OFF
				float2 uv2 : TEXCOORD1;//used for lightmap UV
				#endif
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;//
				float4 shadowCoord: TEXCOORD1;//used for shadowmap
				float4 vertex : SV_POSITION;
				float3 normal:TEXCOORD2;
				float4 wpos: TEXCOORD3;
				#ifndef LIGHTMAP_OFF
				float2 uvLM: TEXCOORD4;//used for lightmap UV
				#endif
				UNITY_FOG_COORDS(5)

			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _ShadowMap;//used for shadowmap
			uniform float4x4 _shadowMatrix;//used for shadowmap
			float _DayNight;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.wpos = mul(unity_ObjectToWorld, v.vertex);
				o.shadowCoord = mul(_shadowMatrix,o.wpos);//used for shadowmap
				o.normal = UnityObjectToWorldNormal(v.normal);
				#ifndef LIGHTMAP_OFF
				o.uvLM=v.uv2*unity_LightmapST.xy+unity_LightmapST.zw;//for lightmap UV
				#endif
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed calculateShadow(float4 shadowCoord)
			{
				fixed2 uv =shadowCoord.xy/shadowCoord.w*float2(0.5f,-0.5f)+0.5f;
				float depth = shadowCoord.z*0.5f+0.5f;
				#if defined(UNITY_REVERSED_Z)
					depth=1-depth;
				#endif

				#if UNITY_UV_STARTS_AT_TOP
					uv.y=1-uv.y;
				#endif
				float4 shadowmap = tex2D(_ShadowMap,uv);

				//VSM
				//float shadowZ=shadowmap.r;
				//float shadowZ_x2=shadowmap.g;
				//float variance = min(max(shadowZ_x2 - shadowZ*shadowZ,0.00001f),1);
				//float mD = shadowZ-depth;
				//float p = variance/(variance+mD*mD);


				float k=20.0;
				//ESM
				float l=DecodeFloatRGBA(shadowmap)*exp(-k*(depth-1));

				l=max(l,depth);
				//fix mistake
				l=saturate((l-0.7f)*4.0f);
				return l;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				fixed3 lightDir = WorldSpaceLightDir(i.wpos);
				fixed3 lighting=0;
				fixed nl=max(0,dot(lightDir,i.normal));

				float l = calculateShadow(i.shadowCoord);
				nl*=l;
				lighting=nl*_LightColor0;
				#ifndef LIGHTMAP_OFF
				fixed3 lm=DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap,i.uvLM.xy));
				lighting+=lerp(unity_AmbientSky,lm,_DayNight);
				#endif

				col.rgb*=lighting;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
