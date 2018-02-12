// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ShadowMap/FastBlur"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		ZWrite Off
		Blend Off

		//Pass 0: Vertical Pass
		Pass
		{
			ZTest Always
			Cull Off

			CGPROGRAM

			#pragma vertex vert_BlurVertical
			#pragma fragment frag_Blur

			ENDCG
		}

		//Pass 1: Horizontal Pass
		Pass
		{
			ZTest Always
			Cull Off

			CGPROGRAM

			#pragma vertex vert_BlurHorizontal
			#pragma fragment frag_Blur

			ENDCG
		}
	}



	CGINCLUDE


	#include "UnityCG.cginc"


	sampler2D _MainTex;
	uniform float4 _MainTex_TexelSize;
	uniform float _BlurSpreadSize;
	static const half4 GaussWeight[7] =
	{
		half4(0.0205,0.0205,0.0205,0),
		half4(0.0855,0.0855,0.0855,0),
		half4(0.232,0.232,0.232,0),
		half4(0.324,0.324,0.324,1),
		half4(0.232,0.232,0.232,0),
		half4(0.0855,0.0855,0.0855,0),
		half4(0.0205,0.0205,0.0205,0)
	};




	struct VertexInput
	{
		float4 vertex:POSITION;
		half2 texcoord:TEXCOORD0;

	};

	struct VertexOutput_Blur
	{

		float4 pos : SV_POSITION;
		half4 uv : TEXCOORD0;
		half2 offset : TEXCOORD1;
	};

	VertexOutput_Blur vert_BlurHorizontal(VertexInput v)
	{
		VertexOutput_Blur o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = half4(v.texcoord.xy, 1, 1);
		o.offset = _MainTex_TexelSize.xy * half2(1.0, 0.0)*_BlurSpreadSize;

		return o;
	}


	VertexOutput_Blur vert_BlurVertical(VertexInput v)
	{

		VertexOutput_Blur o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = half4(v.texcoord.xy, 1, 1);
		o.offset = _MainTex_TexelSize.xy * half2(0.0, 1.0)*_BlurSpreadSize;

		return o;
	}


	half4 frag_Blur(VertexOutput_Blur i) : SV_Target
	{

		half2 uv = i.uv.xy;
		half2 OffsetWidth = i.offset;
		half2 uv_withOffset = uv - OffsetWidth * 3.0;

		half4 color = 0;
		float depth = 0;
		float depth2 = 0;
		for (int j = 0; j< 7; j++)
		{
			half4 texCol = tex2D(_MainTex, uv_withOffset);
			depth += DecodeFloatRGBA(texCol) * GaussWeight[j];
			uv_withOffset += OffsetWidth;
		}
		color = EncodeFloatRGBA(depth);
		//color.rg = (tex2D(_MainTex, uv));
		return color;
	}
	ENDCG

	FallBack Off
}