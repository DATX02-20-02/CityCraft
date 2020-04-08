Shader "Unlit/GhostShader"
{
	Properties
	{
		_MainColor("MainColor", Color) = (1,1,1,1)
		_Fresnel("Fresnel Intensity", Range(0,200)) = 3.0
		_IntersectionThreshold("Highlight of intersection threshold", range(0,100)) = .1 //Max difference for intersections
	}
	SubShader
	{
		Tags{ "Queue" = "Overlay" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		GrabPass{ "_GrabTexture" }
		Pass
		{
			Lighting Off ZWrite Off
			Blend One One
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				fixed4 vertex : POSITION;
				fixed4 normal: NORMAL;
				fixed3 uv : TEXCOORD0;
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;
				fixed4 screenPos: TEXCOORD2;
			};

			sampler2D _CameraDepthTexture, _GrabTexture;
			fixed4 _MainTex_ST,_MainColor,_GrabTexture_ST, _GrabTexture_TexelSize, _Mask_ST;
			fixed _Fresnel, _IntersectionThreshold;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				//fresnel
				fixed3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
				fixed dotProduct = 1 - saturate(dot(v.normal, viewDir));
				o.screenPos = ComputeScreenPos(o.vertex);
				COMPUTE_EYEDEPTH(o.screenPos.z);//eye space depth of the vertex
				return o;
			}

			fixed4 frag (v2f i,fixed face : VFACE) : SV_Target
			{
				//intersection
				fixed intersect = saturate(
					(abs(LinearEyeDepth(tex2Dproj(_CameraDepthTexture,i.screenPos).r) - i.screenPos.z)) / _IntersectionThreshold);

				fixed3 main = fixed3(0, 0, 0);
				main = (1 - intersect) * 0.01 * _MainColor * _Fresnel;

				return fixed4(main, 0.9) * _MainColor.a;
			}
			ENDCG
		}
	}
}
