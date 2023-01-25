using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public enum ShapeType
{
    Tree = 1,
    Plane = 2,
    Box = 4,
    Sphere = 3,
    Leaves = 5,
    RoundedBox = 6,
    CappedCone = 7,
    PineTree = 8,
    SimpleTree = 9
}

public enum ShapeBoolean
{
    Add = 0,
    AddSmooth = 1,
    Subtract = 2
}

[Serializable]
public class ShapeInfo
{
    public string Name = "Default Shape";

    // Type of SDF
    public int ShapeType = 4;

    // TransformInformation
    public float4x4 Transform = float4x4.identity;

    public float4x4 SGTransform = float4x4.identity;

    // TRANSFORMS
    public Quaternion _rotation = Quaternion.identity;
    public Vector3 _position = Vector3.zero;
    public Vector3 _scale = Vector3.one;

    // COLOR
    public Color BaseColor = Color.white;

    // Data
    public int DataLength = 0;

    private ShapeBoolean _boolType = ShapeBoolean.Add;

    public ShapeInfo() { }
    public ShapeInfo(Vector3 pos, Quaternion rot, Vector3 scale)
    {
       
    }

    // 
    public int CombinedGroup = -2;

    // Common data:
    // i = 0: DataLength
    // i = 1: Boolean
    // i = 2: Color
    // i = 3: Bend
    // i = 4: Scale
    public float3[] Data;

    public void InitGeneralData()
    {
        // DataLength
        Data[0] = DataLength;
        Data[2] = new Vector3(BaseColor.r, BaseColor.b, BaseColor.g);
        Data[3] = 0;
        Data[4] = 0;
        Data[5] = Vector3.one;
    }



    public virtual void Draw(){ }

    public virtual ShapeInfo Clone()
    {
        return new ShapeInfo();
    }

    public static T DeepClone<T>(T oldItem) where T : new()// ShapeInfo
    {
        T newShape = new T();

        var returnData = JsonUtility.FromJson<T>(JsonUtility.ToJson(oldItem));  

        return returnData;
    }

    public virtual void GUI() 
    {
        EditorGUI.BeginChangeCheck();

        //_m03_m13_m23
        
        switch (Tools.current)
        {
            case Tool.Move:
                Vector3 newPosition = Handles.PositionHandle(_position, Quaternion.identity);
                EditTransformGUI(ref _position, newPosition);
                break;

            case Tool.Rotate:
                Quaternion newRotation = Handles.RotationHandle(_rotation, _position).normalized;
                EditTransformGUI(ref _rotation, newRotation);
                break;

            case Tool.Scale:
                Vector3 newScale = Handles.ScaleHandle(_scale, _position, _rotation);
                EditTransformGUI(ref _scale, newScale);
                break;
        }

    }

    public virtual void InspectorGUI()
    {
        GUILayout.BeginVertical("box");

        // Shape Name
        GUILayout.Label("General Shape Settings", GUIInfo.TitleSmall);
        Name = EditorGUILayout.TextField("Shape Name", Name);

        GUILayout.Space(10);

        // Boolean Settings
        EditorGUI.BeginChangeCheck();
        ShapeBoolean BoolType = (ShapeBoolean)EditorGUILayout.EnumPopup("Boolean", _boolType);
        if (EditorGUI.EndChangeCheck())
        {
            _boolType = BoolType;
            Data[1] = new Vector3((int)_boolType, 0, 0);
        }

        // TRANSFORMS
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = EditorGUILayout.Vector3Field("Translate", _position);
        EditTransformGUI(ref _position, newPosition);

        EditorGUI.BeginChangeCheck();
        Quaternion newRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", _rotation.eulerAngles));
        EditTransformGUI(ref _rotation, newRotation);

        EditorGUI.BeginChangeCheck();
        Vector3 newScale = EditorGUILayout.Vector3Field("Scale", _scale);
        EditTransformGUI(ref _scale, newScale);

        GUILayout.Space(10);

        // Color
        EditorGUI.BeginChangeCheck();
        Color newColor = EditorGUILayout.ColorField("Color", BaseColor);
        if (EditorGUI.EndChangeCheck())
        {
            Data[2] = new Vector3(newColor.r, newColor.g, newColor.b);
            BaseColor = newColor;
        }

        GUILayout.Space(10);

        GUILayout.EndVertical();       
    }

    private void EditTransformGUI<T>(ref T oldValue, T newValue)
    {
        if (EditorGUI.EndChangeCheck())
        {
            oldValue = newValue;
            Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
            Data[5] = _scale;
        }
    }

}
