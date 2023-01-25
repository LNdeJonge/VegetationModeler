using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Box : ShapeInfo
{
    // Data
    // 0: Boolean
    public Box()
    {
        Name = "Box";
        // Data
        // 0: Boolean
        DataLength = 6;
        ShapeType = 4;
        Data = new float3[DataLength];

        InitGeneralData();
    }

    public Box(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {

        Name = "Box";
        // Data
        // 0: Boolean
        DataLength = 6;
        ShapeType = 4;
        Data = new float3[DataLength];
        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;
    }

    public override void GUI()
    {
        base.GUI();
    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label(" Bend Factor ");
        EditorGUI.BeginChangeCheck();
        float bendFactor = EditorGUILayout.Slider(Data[3].x, -0.99f, 0.99f);
        if (EditorGUI.EndChangeCheck())
        {
            Data[3] = bendFactor;
        }

        GUILayout.Label(" Twist Factor ");
        EditorGUI.BeginChangeCheck();
        float twistFactor = EditorGUILayout.Slider(Data[4].x, -0.5f, 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            Data[4] = twistFactor;
        }
        EditorGUILayout.EndVertical();
    }

    public override ShapeInfo Clone()
    {
        return DeepClone(this);
    }
}


[Serializable]
public class RoundedBox : ShapeInfo
{
    private float _Roundness = 0.1f;


    public RoundedBox(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Rounded Box";
        // Data
        // 0: Boolean
        DataLength = 6 + 1;
        ShapeType = 6;
        Data = new float3[DataLength];

        Data[6] = 0.1f;
        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;
    }

    // Data
    // 0: Boolean
    public RoundedBox()
    {
        Name = "Rounded Box";
        // Data
        // 0: Boolean
        DataLength = 6 + 1;
        ShapeType = 6;
        Data = new float3[DataLength];

        Data[6] = 0.1f;
       InitGeneralData();
    }

    public override void GUI()
    {
        base.GUI();
    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();

        float Roundness = EditorGUILayout.Slider("Roundness", _Roundness, 0.1f, 2f);

        if (EditorGUI.EndChangeCheck())
        {
            _Roundness = Roundness;
            Data[6] = _Roundness;
        }

        EditorGUILayout.EndVertical();
    }

    public override ShapeInfo Clone()
    {
        return DeepClone(this);
    }
}


public class CappedCone : ShapeInfo
{
    private float _Radius = 1f;


    public CappedCone(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Capped Cone";
        // Data
        // 0: Boolean
        DataLength = 6 + 1;
        ShapeType = 7;
        Data = new float3[DataLength];

        Data[6] = _Radius;
        InitGeneralData();


        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;
    }

    // Data
    // 0: Boolean
    public CappedCone()
    {
        Name = "Capped Cone";
        // Data
        // 0: Boolean
        DataLength = 6 + 1;
        ShapeType = 7;
        Data = new float3[DataLength];

        Data[6] = _Radius;
        InitGeneralData();
    }

    public override void GUI()
    {
        base.GUI();

    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();

        float Radius = EditorGUILayout.Slider("Radius", _Radius, 0.1f, 2f);

        if (EditorGUI.EndChangeCheck())
        {
            _Radius = Radius;
            Data[6] = _Radius;
            //Data[3] = bendFactor;
        }

        EditorGUILayout.EndVertical();
    }

    public override ShapeInfo Clone()
    {
        return DeepClone(this);
    }
}

public class Sphere : ShapeInfo
{
    public Sphere()
    {
        Name = "Sphere";

        // 0: Boolean
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 3;



        InitGeneralData();
    }

    public Sphere(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Sphere";

        // 0: Boolean
        DataLength = 6;
        Data = new float3[DataLength];
        ShapeType = 3;     

        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;
    }

    public override void GUI()
    {
        base.GUI();
    }

    public override ShapeInfo Clone()
    {
        return DeepClone(this);
    }
}
