#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class WindowEditor : EditorWindow
{
    private static WindowEditor _window;

    private Vector2 _scrollPosition;
    private Vector2 _editorScrollPosition;

    private Texture2D _selectedBackround;
    //private List<ShapeLogic> _shapes = new();
    private Effect _effectInfo = new();

    private int _currentSelectedShape = -1;

    private bool _showShapeOptions = false;

    /// <summary>
    /// Creates a vegetation window if non-existent
    /// </summary>
    [MenuItem("Window/Vegetation Tool")]
    public static void ShowWindow()
    {
        if (HasOpenInstances<WindowEditor>()) return;

        // if gameobject sdfrenderer doesnt exist create
    //    FindObjectOfType<SDFRenderer> 

        _window = CreateWindow<WindowEditor>("Vegetation Tool");
    }

    public void OnEnable()
    {
        GUIInfo.Init();

        _selectedBackround = GUITools.CreateBackground(new Color(68f / 255f, 100f / 255f, 124f / 255f));
        _currentSelectedShape = 0;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    

    public void OnSceneGUI(SceneView sceneView)
    {

        foreach (ShapeGroup shape in SDF.ShapeGroups)
        {
            for (int i = 0; i < shape.Shapes.Count; i++)
            {
                shape.Shapes[i].Draw();
            }

        }

         ShortCutEvents();
        if (SDF.ShapeGroups.Count < 1 || _currentSelectedShape < 0) return;

        SDF.ShapeGroups[_currentSelectedShape].GUI();

    }

    private Vector2 _scrollPos;

    private void EffectGUI()
    {
        EditorGUILayout.BeginVertical("box");        

        GUIStyle _btnSkin = new GUIStyle(GUI.skin.button);

        _btnSkin.normal.background = (_currentSelectedShape == -2) ? _selectedBackround : null;

        if (GUILayout.Button("Effect Settings", _btnSkin))
        {
            _currentSelectedShape = (_currentSelectedShape == -2) ? -1 : -2;
        }

        EditorGUILayout.EndVertical();

    }

    public void OnGUI()
    {
        _editorScrollPosition = EditorGUILayout.BeginScrollView(_editorScrollPosition, GUILayout.ExpandHeight(true));

        EffectGUI();

        EditorGUILayout.BeginVertical("box");
        _showShapeOptions = EditorGUILayout.Foldout(_showShapeOptions, "Shape Group Options");

        if (_showShapeOptions)
        {
            GUILayout.Label("Shape Groups", GUIInfo.TitleSmall);

            _scrollPosition = EditorGUILayout.BeginScrollView(
            _scrollPosition, GUILayout.Height(200));

            if (SDF.ShapeGroups.Count > 0)
            {
                GUIStyle btn = new GUIStyle(GUI.skin.button);

                for (int i = 0; i < SDF.ShapeGroups.Count; i++)
                {
                    btn.normal.background = (_currentSelectedShape == i) ? _selectedBackround : null;

                    if (GUILayout.Button(SDF.ShapeGroups[i].Name, btn))
                    {
                        _currentSelectedShape = (_currentSelectedShape == i) ? -1 : i;
                    }
                }
            }

            // End the scrollview we began above.
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Up"))
            {
                if (_currentSelectedShape > 0)
                {
                    SDF.MoveUp(_currentSelectedShape);
                    _currentSelectedShape = _currentSelectedShape - 1;
                }
            }

            if (GUILayout.Button("Move Down"))
            {
                if (_currentSelectedShape < SDF.ShapeGroups.Count - 1)
                {
                    SDF.MoveDown(_currentSelectedShape);
                    _currentSelectedShape = _currentSelectedShape + 1;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Add Shape
            if (GUILayout.Button("Add Shape"))
            {
                SDF.Add(SDF.ShapeGroups.Count, new ShapeGroup(new Box()));
                _currentSelectedShape = SDF.ShapeGroups.Count - 1;
            }

            // Delete Shape
            if (GUILayout.Button("Delete Shape"))
            {
                _currentSelectedShape = SDF.Delete(_currentSelectedShape);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Merge Up"))
            {
                _currentSelectedShape = SDF.MergeUp(_currentSelectedShape);
                //_currentSelectedShape 
            }

            if (GUILayout.Button("Merge Down"))
            {
                _currentSelectedShape = SDF.MergeDown(_currentSelectedShape);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        if (_currentSelectedShape == -2) _effectInfo.InspectorGUI();

        if (_currentSelectedShape >= 0 && SDF.ShapeGroups.Count > 0)
        {
            SDF.ShapeGroups[_currentSelectedShape].InspectorGUI();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool ctrl = false;

    private void ShortCutEvents()
    {
        // Return if no object is selected
        if (_currentSelectedShape < 0) return;

        // Ctrl C + V
        if (Event.current.type != EventType.KeyUp) return;
        switch (Event.current.keyCode)
        {
            case KeyCode.F:
                if(SDF.ShapeGroups[_currentSelectedShape].Shapes.Count > 1)
                SceneView.lastActiveSceneView.LookAt(SDF.ShapeGroups[_currentSelectedShape]._position);

                else SceneView.lastActiveSceneView.LookAt(SDF.ShapeGroups[_currentSelectedShape].Shapes[0]._position);

                break;

        }

        Repaint();
    }


}



#endif