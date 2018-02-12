Shader "ShadowMap/ShadowCaster"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		//Offset 1 1
		//Cull Back
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
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float depth = i.vertex.z*i.vertex.w;
				//depth=1-depth;
				#if defined(UNITY_REVERSED_Z)
					depth=1-depth;
				#endif
				//float depth2 = depth*depth;

				fixed4 result;
				//result.rg=EncodeFloatRG(depth);
				//result.ba=EncodeFloatRG(depth2);
				float k=20.0;
				//float z=exp(k*depth);
				result=EncodeFloatRGBA(exp(k*(depth-1)));
				//result.rg=EncodeFloatRG(depth);
				//result.ba=EncodeFloatRG(depth);
				//result=fixed4(depth,depth2,depth,depth2);
				//result=depth;
				return result;
			}
			ENDCG
		}
	}
}
