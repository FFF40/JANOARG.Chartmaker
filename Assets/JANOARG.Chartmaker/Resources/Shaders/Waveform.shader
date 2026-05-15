Shader "UI/Waveform"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DarkAlpha ("Dark Alpha", Range(0,1)) = 0.4
        _Thickness ("Thickness", Range(0,0.5)) = 0.05
        _Scale ("Scale", Range(0,1)) = 0.9
        _Channels ("Channels", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float  _DarkAlpha;
            float  _Thickness;
            float  _Channels;
            float  _Scale;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Which channel band does this fragment fall in?
                float bandF   = uv.y * (_Channels + (1 - _Scale)) - (1 - _Scale) * 0.5;

                float texV    = (floor(bandF) + 0.5) / _Channels;
                float bandLocal = frac(bandF);
                float4 data = tex2D(_MainTex, float2(uv.x, texV));

                float minVal = (data.r * 2.0 - 1.0) * _Scale - _Thickness;
                float maxVal = (data.g * 2.0 - 1.0) * _Scale + _Thickness;
                float rmsVal = data.b * _Scale;

                // Remap bandLocal 0..1 to waveform space -1..1
                float yLocal = bandLocal * 2.0 - 1.0;

                float alpha = (yLocal >= minVal && yLocal <= maxVal)
                    ? ((abs(yLocal) < rmsVal) ? 0.8 : _DarkAlpha)
                    : 0;

                fixed4 color = IN.color;
                color.a *= alpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
            ENDCG
        }
    }
}
