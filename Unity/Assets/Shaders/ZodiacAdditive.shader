Shader "Effectronica/Zodiac Additive" {
  Properties {
     _Color ("Main Color", Color) = (1,1,1,1)     
     _ShadowTex ("Cookie", 2D) = "" { TexGen ObjectLinear }
     _FalloffTex ("FallOff", 2D) = "white" { TexGen ObjectLinear }
  }
  Subshader {
     Tags { "RenderType"="Transparent-1" }
     Pass {
        ZWrite Off
        AlphaTest Greater 0
        Offset -1, -1
        ColorMask RGB
        Blend SrcAlpha One
        Color [_Color]
        SetTexture [_ShadowTex] {
            constantColor [_Color]
            combine texture*constant
            Matrix [_Projector]
        }
        SetTexture [_FalloffTex] {
           constantColor (0,0,0,0)
           combine previous lerp (texture) constant
           Matrix [_ProjectorClip]
        }
     }
  }
}