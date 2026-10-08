Shader "VRShooting/UI/DroneMonitor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Live video", 2D) = "black" {}
        _Color ("Color", Color) = (1,1,1,1)
        _MonitorTint ("Monitor tint", Color) = (.78,.88,.78,1)
        _Saturation ("Saturation", Range(0,1)) = .18
        _Noise ("Noise", Range(0,.15)) = .012
        _Scanlines ("Scanlines", Range(0,.3)) = .035
        _Vignette ("Vignette", Range(0,.5)) = .16
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Stencil write mask", Float) = 255
        _StencilReadMask ("Stencil read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _Color,_MonitorTint,_ClipRect;
            float _Saturation,_Noise,_Scanlines,_Vignette;
            v2f vert(appdata v)
            {v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                float gray=dot(c.rgb,float3(.2126,.7152,.0722));
                c.rgb=lerp(gray.xxx,c.rgb,_Saturation)*_MonitorTint.rgb;
                float grain=frac(sin(dot(floor(i.uv*float2(1024,576)),float2(12.9898,78.233))+floor(_Time.y*12))*43758.5453)-.5;
                float scan=.5+.5*cos(i.uv.y*576*3.14159265);
                float2 edge=(i.uv-.5)*2;
                c.rgb=c.rgb*(1-_Scanlines*scan)*(1-_Vignette*dot(edge,edge)*.5)+grain*_Noise;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
