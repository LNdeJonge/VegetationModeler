struct FrenetFrame
{
    float3 origin;
    float3 tangent;
    float3 normal;
    float3 binormal;
};

#include "SDFFunctions.cginc"

// returns xyz = position, w = spline position (t)
float4 sdBezierExtrude(float3 pos, float3 A, float3 B, float3 C)
{
    // first, calc curve T value
    float3 a = B - A;
    float3 b = A - 2.0 * B + C;
    float3 c = a * 2.0;
    float3 d = A - pos;

    float kk = 1.0 / dot(b, b);
    float kx = kk * dot(a, b);
    float ky = kk * (2.0 * dot(a, a) + dot(d, b)) / 3.0;
    float kz = kk * dot(d, a);

    float p = ky - kx * kx;
    float p3 = p * p * p;
    float q = kx * (2.0 * kx * kx - 3.0 * ky) + kz;
    float h = q * q + 4.0 * p3;
    float t;

    if (h >= 0.0)
    {
        h = sqrt(h);
        float2 x = (float2(h, -h) - q) / 2.0;
        float2 uv = sign(x) * pow(abs(x), float2(1.0 / 3.0, 1.0 / 3.0));
        t = clamp(uv.x + uv.y - kx, 0.0, 1.0);
        // 1 root
    }
    else
    {
        float z = sqrt(-p);
        float v = acos(q / (p * z * 2.0)) / 3.0;
        float m = cos(v);
        float n = sin(v) * 1.732050808;
        float3 _t = clamp(float3(m + m, -n - m, n - m) * z - kx, 0.0, 1.0);
        // 3 roots, but only need two
        float3 r1 = d + (c + b * _t.x) * _t.x;
        float3 r2 = d + (c + b * _t.y) * _t.y;
        //t = length(r2.xyz) < length(r1.xyz) ? _t.y : _t.x;
        t = dot(r2, r2) < dot(r1, r1) ? _t.y : _t.x; // quicker

    }


    // now we have t, calculate splineposition and orient to spline tangent
    //t = clamp(t,0.1,0.9); // clamp spline start/end

    // BEZIER FUNCTION, USE FRENET FRAME!!!!
    float3 _tan = normalize((2.0 - 2.0 * t) * (B - A) + 2.0 * t * (C - B));  // spline tangent
    float3 up = float3(0.0, 1.0, 0.0);
    float3 binormal = normalize(cross(up, _tan));
    float3 _normal = cross(_tan, binormal);
    float3 t1 = normalize(cross(_normal, _tan));
    // float3 t1 = cross(_normal, _tan); // no need to normalize this?
    float3x3 mm = float3x3(t1, cross(_tan, t1), _tan);
    pos.xyz = lerp(lerp(A, B, t), lerp(B, C, t), t) - pos; // spline position
    return float4(mul(mm, pos.xyz), t);
}

float BezierBox(float3 p, float3 p0, float3 p1, float3 p2)
{
    float4 bz = sdBezierExtrude(p, p0, p1, p2);
    float dm = length(p - p1);

    float de = sdBox(bz.xyz, float3(0.8, 0.5, 0.2));

    return de * 0.5f;
}

float cubic_root(float x)
{
    if (x < 0.0) return -pow(-x, 1.0 / 3.0);
    return pow(x, 1.0 / 3.0);
}

float3 cubic_solve(in float a, in float b, in float c)
{
    float  p = b - a * a / 3.0;
    float  q = a * (2.0 * a * a - 9.0 * b) / 27.0 + c;
    float p3 = p * p * p;
    float  d = q * q + 4.0 * p3 / 27.0;
    float offset = -a / 3.0;
    if (d >= 0.0)
    {
        float z = sqrt(d);
        float u = (-q + z) / 2.0;
        float v = (-q - z) / 2.0;
        u = cubic_root(u);
        v = cubic_root(v);
        return float3(offset + u + v, 0.0, 0.0);
    }
    float u = sqrt(-p / 3.0);
    float v = acos(-sqrt(-27.0 / p3) * q / 2.0) / 3.0;
    float m = cos(v), n = sin(v) * 1.732050808;
    float r0 = offset + u * (m + m);
    float r1 = offset - u * (n + m);
    float r2 = offset + u * (n - m);
    return float3(r0, r1, r2);
}


float3 opBezier(float2 p, float2 a, float2 b, float2 c)
{
    b = lerp(b + float2(1e-4, 1e-4), b, abs(sign(b * 2.0 - a - c)));
    float2 A = b - a, B = a - b * 2.0 + c, C = A * 2.0, D = a - p;
    float3 k = float3(3. * dot(A, B), 2. * dot(A, A) + dot(D, B), dot(D, A)) / dot(B, B);
    float3 t = clamp(cubic_solve(k.x, k.y, k.z), 0.0, 1.0);
    float2 dp1 = D + (C + B * t.x) * t.x;
    float2 dp2 = D + (C + B * t.y) * t.y;
    float d1 = dot(dp1, dp1);
    float d2 = dot(dp2, dp2);
    float2 h = (d1 < d2) ? float2(d1, t.x) : float2(d2, t.y);
    float2 g = normalize(2. * B * h.y + C);
    p -= lerp(lerp(a, b, h.y), lerp(b, c, h.y), h.y);
    float y = g.x * p.y - g.y * p.x;
    float x = sqrt(max(0.0, h.x - y * y)) * sign(h.y - 0.5);


    return float3(x, y, h.y);
}

float4 opBezier(in float3 p, in float3 a, in float3 b, in float3 c)
{
    float3 A = float3(0, 0, 0); float3 B = b - a; float3 C = c - a;

    float3 w = normalize(cross(C - B, A - B));
    float3 u = normalize(C - B);
    float3 v = normalize(cross(w, u));
    float3x3 m = float3x3(u, v, w);

    A = mul(m, A), B = mul(m, B), C = mul(m, C); p = mul(m, p);

    float3 bX = opBezier(p.xy, A.xy, B.xy, C.xy);


    return float4(bX.xy, p.z, bX.z);
}
