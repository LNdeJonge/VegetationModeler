float rand(float2 co) 
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float rnd(float2 p)
{
    return abs(rand(p)) * 0.8 + 0.1;
}

float value(float x, float randx)
{
    float a = min(x / randx, 1.0);
    float b = min(1.0, (1.0 - x) / (1.0 - randx));
    return a + (b - 1.0);
}

float polynoise(float2 p)
{
    float2 seed = floor(p);
    float2 rndv = float2(rnd(seed.xy), rnd(seed.yx));
    float2 pt = frac(p);
    float bx = value(pt.x, rndv.x);
    float by = value(pt.y, rndv.y);
    return min(bx, by) * abs(rnd(seed.xy * 0.1));
}

float2x2 rotaty(float a) { return float2x2(cos(a), sin(a), -sin(a), cos(a)); }


float polyfbm(float2 p)
{
    float2x2 r1; float2x2 r2; float2x2 r3;

    r1 = rotaty(0.0);
    r2 = rotaty(0.0);
    r3 = rotaty(0.0);

    float2 seed = floor(p);
    float m1 = polynoise(mul(p, r2));
    m1 += polynoise(mul(r1, p));// (float2(0.8, 0.8)) + p));
    m1 += polynoise(mul(r3, p));// (float2(0.35, 0.415)) + p));
    m1 *= 1.5;

    float m2 = polynoise(mul(r3, (p * 8.)));
    m1 += m2 * 0.55; 
    return m1;
}

float2x2 rotateB(float a)
{
    float ca = cos(a); float sa = sin(a);
    return float2x2(ca, sa, -sa, ca);
}

// 'p' must be normalized
float2 compute_texcoord(float3 p)
{
    float phi = atan2(p.y, p.x);
    float theta = acos(p.z);
    float s = phi * (2.0 / (2.0 * 3.14159265));
    float t = theta * (1.0 / 3.14159265);
    return float2(s, t);
}

float4 Mozaic(int tilesX, int tilesY, in float2 uv, in sampler2D mainTex)
{
    float2 tiles = float2(tilesX, tilesY);
    float2 uv2 = floor(uv * tiles) / tiles;
    uv -= uv2;
    uv *= tiles;

    float4 col = tex2Dlod(mainTex, float4(uv2 + float2(step(1.0 - uv.y, uv.x) / (2.0 * tilesX),
    step(uv.x, uv.y) / (2.0 * tilesY)), 0., 0.));

    return col;
}

float4 OilPaintEffect(float r, float2 uv, in sampler2D mainTex, in float4 mainTex_TexelSize)
{
    float4x3 m;
    float4x3 s;

    float2 start[4] = { {-r, -r}, {-r, 0}, {0, -r}, {0, 0} };

    float2 pos;
    float3 col;
    for (int k = 0; k < 4; k++) {
        for (int i = 0; i <= r; i++) {
            for (int j = 0; j <= r; j++) {
                pos = float2(i, j) + start[k];
                col = tex2Dlod(mainTex, float4(uv + float2(pos.x * mainTex_TexelSize.x, pos.y * mainTex_TexelSize.y), 0., 0.)).rgb;
                m[k] += col;
                s[k] += col * col;
            }
        }
    }

    float s2;

    float n = pow(r + 1, 2);
    col = tex2D(mainTex, uv);
    float min = 1;

    for (int l = 0; l < 4; l++) {
        m[l] /= n;
        s[l] = abs(s[l] / n - m[l] * m[l]);
        s2 = s[l].r + s[l].g + s[l].b;

        if (s2 < min) {
            min = s2;
            col.rgb = m[l].rgb;
        }
    }

    return float4(col.rgb, 1);
}