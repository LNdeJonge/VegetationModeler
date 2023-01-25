using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class LPTree : ShapeInfo
{
    public List<BranchInformation> _branches = new();

    public int BranchCount = 3;

    public float BranchLength = 1f;
    public float BranchLengthMul = 0.8f;
    public float BranchWidth = 0.5f;
    public float BranchWidthMul = 0.5f;
    public int BranchGroupCount = 3;


    public float TrunkWidthBottom = 1f;
    public float TrunkWidthTop = 0.5f;

    public float TrunkLength = 4;

    // public int LeaveCount = 5;

    public Color LeafColor = new Color(1, 1, 1);

    public LPTree()
    {
        Name = "Simple Tree";
        ShapeType = 9;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 2;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;
        Data[7] = new float3(LeafColor.r, LeafColor.g, LeafColor.b);

        InitGeneralData();

        Generate();
    }

    public LPTree(Vector3 pos, Quaternion rot, Vector3 scale) : base(pos, rot, scale)
    {
        Name = "Simple Tree";
        ShapeType = 9;
        // Data
        // 6 - standard + custom
        DataLength = 6 + 2;
        Data = new float3[DataLength];

        Data[6] = _branches.Count;
        Data[7] = new float3(LeafColor.r, LeafColor.g, LeafColor.b);

        InitGeneralData();

        _position = pos;
        _rotation = rot;
        _scale = scale;

        Transform = Matrix4x4.TRS(_position, _rotation, Vector3.one).inverse;
        Data[5] = _scale;

        Generate();
    }

    public void Generate()
    {
        _branches?.Clear();

        BranchInformation trunk = SetBranchInfo(0.2f, 0, TrunkWidthBottom, TrunkWidthTop, new float3(0, 0, 0), new float3(0, TrunkLength, 0));
        _branches.Add(trunk);

        float branchStep = TrunkLength / BranchCount;

        float bL = BranchLength;

        for (int i = 1; i < BranchCount; i++)
        {
            float3 bBranch = new Vector3(0, branchStep * i, 0);
            float3 eBranch = bBranch + new float3(1, 0, 0) * (bL);

            bL *= BranchLengthMul;

            Quaternion rRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 275f), Vector3.up);


            float angle = 360f / BranchGroupCount;
            float3 neBranch = Quaternion.AngleAxis(angle * i, Vector3.up) * eBranch;

            neBranch = rRotation * neBranch;

            BranchInformation newB = SetBranchInfo(0.2f, 0, BranchWidth, BranchWidth * BranchWidthMul, bBranch, neBranch);
            _branches.Add(newB);

            float3 dir = Vector3.Normalize(neBranch - bBranch);



        }

        Data[6] = _branches.Count;
    }

    public override void Draw()
    {
    }

    public override void InspectorGUI()
    {
        base.InspectorGUI();

        GUILayout.BeginVertical("box");

        GUILayout.Label(Name + " Settings", GUIInfo.TitleSmall);

        if (GUILayout.Button("Update"))
        {
            Generate();
        }

        //EditorGUI.BeginChangeCheck();
        //int leafCount = (int)EditorGUILayout.Slider(_LeafCount, 0, 8);

        // Shape Name
        GUILayout.Label("Branch Settings", GUIInfo.TitleSmall);

        EditorGUILayout.Space();

        GUILayout.Label("Branch Width");
        float bWidth = EditorGUILayout.Slider(BranchWidth, 0f, 2f);

        GUILayout.Label("Branch Width Multi");
        float bWidthMul = EditorGUILayout.Slider(BranchWidthMul, 0f, 1f);

        GUILayout.Label("Branch Length");
        float bLength = EditorGUILayout.Slider(BranchLength, 0f, 5f);

        GUILayout.Label("Branch Count");
        int bCount = (int)EditorGUILayout.Slider(BranchCount, 0, 10);

        GUILayout.Label("Branches Per Row");
        int bpR = (int)EditorGUILayout.Slider(BranchGroupCount, 0, 10);

        Color leafC = EditorGUILayout.ColorField("Leaf Color", LeafColor);

        if (EditorGUI.EndChangeCheck())
        {
            BranchCount = bCount;
            BranchGroupCount = bpR;
            BranchWidth = bWidth;
            BranchWidthMul = bWidthMul;
            BranchLength = bLength;
            //_WidthDecrease = widthDecrease;
            //BranchLength = startLength;
            //  _LeafCount = leafCount;

            LeafColor = leafC;
            Data[7] = new float3(LeafColor.r, LeafColor.g, LeafColor.b);

        }

        EditorGUILayout.Space();

        GUILayout.Label("Trunk Settings", GUIInfo.TitleSmall);
        GUILayout.Label("Trunk Length");

        EditorGUI.BeginChangeCheck();

        float trunkLength = EditorGUILayout.Slider(TrunkLength, 0f, 10f);

        GUILayout.Label("Trunk Bottom Width");

        float trunkWidthB = EditorGUILayout.Slider(TrunkWidthBottom, 0.1f, 3f);

        GUILayout.Label("Trunk Top Width");

        float trunkWidthMulT = EditorGUILayout.Slider(TrunkWidthTop, 0.1f, 1f);

   

        // TRUNK
        if (EditorGUI.EndChangeCheck())
        {
            TrunkWidthBottom = trunkWidthB;
            TrunkWidthTop = trunkWidthMulT;

            TrunkLength = trunkLength;
        }


        GUILayout.EndVertical();

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
            Generate();
        }
    }

    public static BranchInformation SetBranchInfo(float l, float angle, float w0, float w2, float3 pos, float3 ePos)
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
}
