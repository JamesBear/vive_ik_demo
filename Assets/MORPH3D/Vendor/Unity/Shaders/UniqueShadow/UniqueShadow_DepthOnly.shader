Shader "Hidden/Volund/Unique Shadow Depth Only" {

CGINCLUDE
	#pragma only_renderers d3d11 d3d9 opengl
	#pragma fragmentoption ARB_precision_hint_fastest
	
	uniform float4		_MainTex_ST;
	uniform fixed		_Cutoff;
	uniform sampler2D	_MainTex;

	struct a2v {
		float4	vertex	: POSITION;
		float2	uv		: TEXCOORD0;
	};
	
	struct v2f {
		float4	pos	: SV_POSITION;
		half2	uv	: TEXCOORD0;
	};

	v2f vert(a2v v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.pos.z = max(UNITY_NEAR_CLIP_VALUE, o.pos.z);
		o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
		return o;
	}

	void HandleClip(half2 uv, fixed cutoff) {
		fixed a = tex2D(_MainTex, uv).a;
		clip(a - cutoff);
	}
	
	fixed4 frag(v2f i) : COLOR {
#if defined(_ALPHATEST_ON)
		HandleClip(i.uv, _Cutoff);
#endif
		return 0;
	}
	
	fixed4 fragT(v2f i) : COLOR {
#if defined(_ALPHATEST_ON)
		HandleClip(i.uv, max(_Cutoff, 0.5f));
#else
		HandleClip(i.uv, 0.5f);
#endif
		return 0;
	}
ENDCG

SubShader {
	Tags { "RenderType"="Opaque" }
	
	Cull Back
	ColorMask 0
	Offset 1, 0

	Pass {
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _ALPHATEST_ON
		ENDCG
	}
}

SubShader {
	Tags { "RenderType"="Transparent" }
	
	Cull Off
	ColorMask 0
	Offset 1, 0

	Pass {
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragT
			#pragma multi_compile _ _ALPHATEST_ON
			#define _ALPHABLEND_ON
		ENDCG
	}
}
}
