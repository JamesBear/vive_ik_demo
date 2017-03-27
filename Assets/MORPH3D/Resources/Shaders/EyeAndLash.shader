Shader "Morph3D/EyeAndLash" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Spread ("Spread", Range(0,128)) = 64
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest Less
		Tags { 
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
			"RenderType"="Transparent"
		}

		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf EyeLight fullforwardshadows alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "UnityPBSLighting.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 lightDir;
		};

		struct SurfOut {
			fixed3 Albedo;      // base (diffuse or specular) color
			fixed3 Normal;      // tangent space normal, if written
			half3 Emission;
			half Metallic;      // 0=non-metal, 1=metal
			half Smoothness;    // 0=rough, 1=smooth
			half Occlusion;     // occlusion (default 1)
			fixed Alpha;        // alpha for transparencies
			float3 lightDir;
			float3 viewDir;
			float2 uv;
		};

		half _Spread;
		fixed4 _Color;

		//our custom light function that generates a highlight on the wet part of the eye
		half4 LightingEyeLight(SurfOut s, half3 lightDir, half3 viewDir, half atten) {
			half NdotL = dot(s.Normal, lightDir);
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
			c.a = s.Alpha;

			//our eyes are to the right of this point in the uv space
			bool isEye = s.uv.x > 0.415;

			//is this our eye or eyelash?
			if (isEye) {
				float3 halfAngle = normalize((normalize(lightDir) + normalize(viewDir)));
				half diff = max(0, dot(s.Normal, lightDir));
				float nh = max(0, dot(s.Normal, halfAngle));
				float rim = pow(nh, _Spread);
				if (rim > 0.5) {
					rim *= 1.5;
				}
				rim = saturate(rim);

				c.a = rim;
			}
			return c;
		}

		void surf (Input IN, inout SurfOut o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.uv = IN.uv_MainTex;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
