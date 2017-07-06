Shader "Hidden/LightingShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	// Structure representing a virtual point light
	struct VPL
	{
		float3 color;
		float3 position;
		float3 normal;
	};

	uniform StructuredBuffer<VPL>	_FirstVPLBuffer;
	uniform StructuredBuffer<VPL>	_SecondVPLBuffer;
	
	uniform sampler2D				_MainTex;
	uniform sampler2D				_PositionTexture;
	uniform sampler2D				_NormalTexture;
	uniform sampler2D				_ColorTexture;
	uniform sampler2D				_IndirectTexture;

	uniform float3					_CameraPosition;

	uniform float					_WorldVolumeBoundary;
	uniform float					_DirectStrength;
	uniform float					_FirstBounceStrength;
	uniform float					_SecondBounceStrength;
	
	uniform uint					_RenderSize;

	// Structure representing input to the vertex shader
	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	// Structure representing input to the fragment shader
	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	// Vertex shader for the lighting pass
	v2f vert_lighting(appdata v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.uv;
		return o;
	}

	// Fragment shader for the lighting pass
	float4 frag_lighting(v2f i) : SV_Target
	{
		float3 accumulatedColor = float3(0.0, 0.0, 0.0);

		float3 firstBounceColor = float3(0.0, 0.0, 0.0);
		float3 secondBounceColor = float3(0.0, 0.0, 0.0);

		float3 pixelPosition = tex2D(_PositionTexture, i.uv);
		float3 pixelColor = tex2D(_ColorTexture, i.uv);
		float3 pixelNormal = tex2D(_NormalTexture, i.uv);

		// Reconstruct pixel position
		pixelPosition *= (2.0 * _WorldVolumeBoundary);
		pixelPosition -= _WorldVolumeBoundary;

		// Reconstruct pixel normal
		pixelNormal -= 0.5;
		pixelNormal *= 2.0;

		// Now, traverse through the vpl buffer and calculate lighting due to all the virtual point lights
		uint numberOfVPL = (_RenderSize * _RenderSize);
		
		float3 lightPosition = float3(0.0, 0.0, 0.0);
		float3 lightColor = float3(0.0, 0.0, 0.0);

		// Compute first bounce lighting
		for (uint i1 = 0; i1 < numberOfVPL; ++i1)
		{
			lightPosition = _FirstVPLBuffer[i1].position;
			lightColor = _FirstVPLBuffer[i1].color;
			
			float3 surfaceToLight = lightPosition - pixelPosition;

			float brightness = dot(pixelNormal, surfaceToLight) / (length(surfaceToLight) * length(pixelNormal));
			brightness = clamp(brightness, 0, 1);

			firstBounceColor += (brightness * (lightColor * pixelColor));

		}

		// Compute second bounce lighting
		for (uint i2 = 0; i2 < numberOfVPL; ++i2)
		{
			lightPosition = _SecondVPLBuffer[i2].position;
			lightColor = _SecondVPLBuffer[i2].color;

			float3 surfaceToLight = lightPosition - pixelPosition;

			float brightness = dot(pixelNormal, surfaceToLight) / (length(surfaceToLight) * length(pixelNormal));
			brightness = clamp(brightness, 0, 1);

			secondBounceColor += (brightness * (lightColor * pixelColor));

		}

		accumulatedColor = ((firstBounceColor * _FirstBounceStrength) + (secondBounceColor * _SecondBounceStrength)) / (float)(numberOfVPL);
		
		return float4(accumulatedColor, 1.0);
	}

	// Vertex shader for the blending pass
	v2f vert_blending(appdata v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.uv;
		return o;
	}

	// Fragment shader for the blending pass
	float4 frag_blending(v2f i) : SV_Target
	{
		float3 direct = tex2D(_MainTex, i.uv) * _DirectStrength;
		float3 indirect = tex2D(_IndirectTexture, i.uv);
		float3 finalLighting = direct + indirect;
		return float4(finalLighting, 1.0);
	}

	ENDCG

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// 0 : Lighting Pass
		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert_lighting
			#pragma fragment frag_lighting
			
			ENDCG
		}

		// 1 : Blending Pass
		Pass
		{
			CGPROGRAM

			#pragma vertex vert_blending
			#pragma fragment frag_blending

			ENDCG
		}
	}
}