using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
// Stride = sizeof(float) * 7
public struct BranchInformation
{
    public float l;
    public float angle;
    public float w0;
    public float w2;
    public float3 pos;
    public float3 ePos;
}

public enum TreeType
{
    Default,
    Pine
}

public class Tree : ShapeInfo
{
    //Leaves
    public List<BranchInformation> _branches = new();

    public Tree(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Tree";
        ShapeType = 1;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 1;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;

        _leaves = new();

        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;

        UpdateBranchData();
    }
    private Leaves _leaves;

    // 0: Height
    // 1: Max Leaves
    private float _trunkLength = 2f;
    private float _trunkWidth = .5f;
    private float _trunkWidthMul = .5f;
    private float _branchLength = 2f;

    private float _WidthDecrease = 0.4f;

    private float _Angle = 165f;

    private float _LeafCount = 8;

    public Tree()
    {
        Name = "Tree";
        ShapeType = 1;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 1;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;

        _leaves = new();

        InitGeneralData();

        UpdateBranchData();
    }

    public override void GUI()
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

    private void EditTransformGUI<T>(ref T oldValue, T newValue)
    {
        if (EditorGUI.EndChangeCheck())
        {
            oldValue = newValue;
            Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
            UpdateBranchData();
        }
    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        GUILayout.BeginVertical("box");

        GUILayout.Label(Name + " Settings", GUIInfo.TitleSmall);

        if (GUILayout.Button("Update"))
        {
            UpdateBranchData();
        }

        GUILayout.Label("Leaf Count");

        EditorGUI.BeginChangeCheck();
        int leafCount = (int)EditorGUILayout.Slider(_LeafCount, 0, 8);

        // Shape Name
        GUILayout.Label("Branch Settings", GUIInfo.TitleSmall);

        EditorGUILayout.Space();

        GUILayout.Label("Branch Start Length");
        float startLength = EditorGUILayout.Slider(_branchLength, 0f, 6f);

        GUILayout.Label("Angle");
        float angle = EditorGUILayout.Slider(_Angle, 150f, 180f);

        GUILayout.Label("Width Decrease");
        float widthDecrease = EditorGUILayout.Slider(_WidthDecrease, 0.1f, 1.0f);

        if (EditorGUI.EndChangeCheck())
        {
          //  Data[7] = newMaxLeafCount;
            _Angle = angle;
            _WidthDecrease = widthDecrease;
            _branchLength = startLength;

            _LeafCount = leafCount;

        }

        EditorGUILayout.Space();

        GUILayout.Label("Trunk Settings", GUIInfo.TitleSmall);
        GUILayout.Label("Trunk Length");

        EditorGUI.BeginChangeCheck();

        float trunkLength = EditorGUILayout.Slider(_trunkLength, 0f, 6f);

        GUILayout.Label("Trunk Start Width");

        float trunkWidth = EditorGUILayout.Slider(_trunkWidth, 0.1f, 3f);

        GUILayout.Label("Trunk Width Multiplation");

        float trunkWidthMul = EditorGUILayout.Slider(_trunkWidthMul, 0.1f, 1f);

        // TRUNK
        if (EditorGUI.EndChangeCheck())
        {
            _trunkLength = trunkLength;

            _trunkWidth = trunkWidth;

            _trunkWidthMul = trunkWidthMul;

           // UpdateBranchData();

        }


        GUILayout.EndVertical();

        _leaves.InspectorGUI();
    }


    private void UpdateBranchData()
    {
        _leaves.Transforms.Clear();
        _branches.Clear();
        BranchInformation trunk = SetBranchInfo(0.2f, 0, _trunkWidth, _trunkWidth * _trunkWidthMul, new float3(0, 0, 0), new float3(0, _trunkLength, 0));

        _branches.Add(trunk);
        GenerateBranch_Recursive(1f, _branchLength, _trunkWidth * _trunkWidthMul, new float3(0, _trunkLength, 0), new float3(0, _trunkLength + 1f, 0));

        Data[6] = _branches.Count;
    }

    void GenerateBranch_Recursive(float ls, float le, float w, float3 end, float3 dir)
    {
        if (le < 0.2f)
        {
            
            return;
        }
        dir = Vector3.Normalize(dir);

        float length = le;

        length *= UnityEngine.Random.Range(.3f, .8f);

        float3 nPos = end - dir * length;

        float wB = w;
        w = w * _WidthDecrease;// * _RandomWidth;

        nPos = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(40, 260), -_Angle)) * (nPos - end) + (Vector3)end;

        float leafStep = length / _LeafCount;

        ls *= .4f;
        // Add leaves
        for (int i = 1; i < _LeafCount; i++)
        {
            Matrix4x4 t = (Matrix4x4)Transform;
            Matrix4x4 newLeaf = t.inverse * Matrix4x4.TRS(end + ((float3)Vector3.Normalize(nPos - end) * (leafStep * i)), Quaternion.AngleAxis(UnityEngine.Random.Range(20, 270), (float3)Vector3.Normalize(nPos - end)) * Quaternion.identity, Vector3.one * ls);
          //  newLeaf = Matrix4x4.Inverse(newLeaf);
            _leaves.Transforms.Add(newLeaf);
        }
        BranchInformation b1 = SetBranchInfo(length, 0, wB, w, end, nPos);
        _branches.Add(b1);

        GenerateBranch_Recursive(ls, length, w, nPos, Vector3.Normalize(nPos - end));

        length *= .6f;
        float3 nPos2 = end - dir * length;

        nPos2 = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(40, 80), _Angle)) * (nPos2 - end) + (Vector3)end;
        BranchInformation b2 = SetBranchInfo(length, 0, wB, w, end, nPos2);

        _branches.Add(b2);

        GenerateBranch_Recursive(ls, length, w, nPos2, Vector3.Normalize(nPos2 - end));

    }

    public override void Draw()
    {
        _leaves.Draw();
    }

    BranchInformation SetBranchInfo(float l, float angle, float w0, float w2, float3 pos, float3 ePos)
    {
        BranchInformation b = new();
        b.l = l;
        b.angle = angle;
        b.w0 = w0;
        b.w2 = w2;
        b.pos = pos;
        b.ePos = ePos;

        return b;
    }

    public override ShapeInfo Clone()
    {
        return DeepClone(this);
    }

}
