using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ProceduralNoise.Editor
{
    public class GenerationWindow : EditorWindow
    {
        private SettingsEditor _settingsEditor;
        private GUIStyle _pathStyle;

        private bool _is3D;
        private Vector3Int _resolution;
        private float _rotation;
        private string _path;
        private string _name;
        private bool _autoName;
        private bool _preserveState;
        private RenderTexture _generated;
        private RenderTexture _preview;

        private Channel _visibleChannels = Channel.RGB;
        private int _depth;
        private Material _material;

        private Random.State _randomState;

        private Vector2 _scrollView;

        private static readonly int Mask = Shader.PropertyToID("_Mask");

        private const string Is3DKey = "ProcNoise3DKey";
        private const string ResolutionKeyX = "ProcNoiseResolutionX";
        private const string ResolutionKeyY = "ProcNoiseResolutionY";
        private const string ResolutionKeyZ = "ProcNoiseResolutionZ";
        private const string RotationKey = "ProcNoiseRotation";
        private const string PathKey = "ProcNoisePath";
        private const string FileNameKey = "ProcNoiseFileName";
        private const string AutoNameKey = "ProcNoiseAutoName";
        private const string PreserveStateKey = "ProcNoisePreserveState";
        private const string VisibleChannelsKey = "ProcNoiseVisibleChannels";
        
        private string Path => Application.dataPath + "/" + _path + "/";
        private string FileName
        {
            get
            {
                if (!_autoName)
                    return _name;
                
                string fileName = _settingsEditor.SelectedNoise + "_";
                Vector3 cellCount;
                switch (_settingsEditor.SelectedNoise)
                {
                    case SettingsEditor.NoiseType.Perlin:
                        fileName += _resolution.x + "x" + _resolution.y + "x" + _resolution.z + "_";
                        cellCount = Settings.PerlinParameters.cellCount;
                        fileName += cellCount.x + "x" + cellCount.y + "x" + cellCount.z;
                        break;
                    
                    case SettingsEditor.NoiseType.Voronoi:
                        break;
                    
                    case SettingsEditor.NoiseType.Worley:
                        fileName += _resolution.x + "x" + _resolution.y + "x" + _resolution.z + "_";
                        cellCount = Settings.WorleyParameters.cellCount;
                        fileName += cellCount.x + "x" + cellCount.y + "x" + cellCount.z;
                        break;
                }

                return fileName;
            }
        }

        private Material Material
        {
            get
            {
                if (_material)
                    return _material;

                const string path = "Packages/com.jordantama.procedural-noise/Shaders/NoiseGeneratorPreview.shader";
                _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(path));
                _material.SetInt(Mask, (int) _visibleChannels);
                
                return _material;
            }
        }
        
        [MenuItem("JordanTama/Procedural Noise/Noise Generator")]
        private static void ShowWindow()
        {
            GenerationWindow window = GetWindow<GenerationWindow>();
            window.titleContent = new GUIContent(
                "Noise Generator",
                EditorGUIUtility.IconContent("d_Texture Icon").image
                );
            
            window.Show();
        }

        private void OnEnable()
        {
            _settingsEditor = UnityEditor.Editor.CreateEditor(Settings.Instance) as SettingsEditor;
            _pathStyle = new GUIStyle
            {
                fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState {textColor = GUI.contentColor}
            };

            LoadSettings();
            RegenerateTexture();
        }
        
        private void OnDisable() => SaveSettings();

        private void OnGUI()
        {
            if (!_generated)
                RegenerateTexture();

            _scrollView = EditorGUILayout.BeginScrollView(_scrollView);
            
            #region Generation Settings

            // Draw settings editor
            EditorGUI.BeginChangeCheck();
            _settingsEditor.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1.5f), Color.grey);
            EditorGUILayout.Space();

            // 3D field
            _is3D = EditorGUILayout.Toggle("Is 3D", _is3D);
            
            // Resolution field
            _resolution = Vector3Int.Max(Vector3Int.one, EditorGUILayout.Vector3IntField("Resolution", _resolution));

            // Rotation field
            _rotation = EditorGUILayout.FloatField("Rotation", _rotation);
            
            if (EditorGUI.EndChangeCheck())
                RegenerateTexture();
            
            #endregion

            #region File Settings
            
            // Path and filename fields
            _path = EditorGUILayout.TextField("Path", _path);
            GUI.enabled = !_autoName;
            _name = EditorGUILayout.TextField("Name", _name);
            GUI.enabled = true;
            _autoName = EditorGUILayout.Toggle("Auto Name", _autoName);
            
            // Preserve state field
            _preserveState = EditorGUILayout.Toggle("Preserve State", _preserveState);
            EditorGUILayout.Space();
            
            // Path preview
            EditorGUILayout.LabelField("/Assets/" + _path + "/" + FileName + ".png", _pathStyle);
            EditorGUILayout.Space();
            
            // File exists warning
            if (!string.IsNullOrEmpty(Path) && File.Exists(Path + FileName + ".png"))
            {
                IParameters parameters = _settingsEditor.SelectedNoise switch
                {
                    SettingsEditor.NoiseType.Perlin => Settings.PerlinParameters,
                    SettingsEditor.NoiseType.Voronoi => Settings.VoronoiParameters,
                    SettingsEditor.NoiseType.Worley => Settings.WorleyParameters,
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                string channelString = "";
                if (parameters.RedSettings.writeType != WriteType.Keep)
                    channelString += "R";
                if (parameters.GreenSettings.writeType != WriteType.Keep)
                    channelString += channelString == "" ? "G" : ", G";
                if (parameters.BlueSettings.writeType != WriteType.Keep)
                    channelString += channelString == "" ? "B" : ", B";
                if (parameters.AlphaSettings.writeType != WriteType.Keep)
                    channelString += channelString == "" ? "A" : ", A";

                EditorGUILayout.HelpBox($"This will overwrite the file with the same name ({channelString})!",
                    MessageType.Warning);
            }
            
            #endregion

            #region Saving To File
            
            GUI.enabled = !string.IsNullOrEmpty(_path) && (!string.IsNullOrEmpty(_name) || _autoName);
            // Generate and save to file button
            if (GUILayout.Button("Generate"))
            {
                Object obj;
                if (_is3D)
                {
                    Texture3D texture = ToTexture3D(_generated);
                    Directory.CreateDirectory(Path);
                    string assetPath = "Assets/" + _path + "/" + FileName + ".asset";
                    AssetDatabase.CreateAsset(texture, assetPath);
                    AssetDatabase.Refresh();
                    obj = AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath);
                }
                else
                {
                    Texture2D texture = ToTexture2D(_generated);
                    byte[] bytes = texture.EncodeToPNG();
                    Directory.CreateDirectory(Path);

                    File.WriteAllBytes(Path + FileName + ".png", bytes);

                    AssetDatabase.Refresh();
                    obj =
                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + _path + "/" + FileName +
                                                                 ".png");
                }
                
                EditorGUIUtility.PingObject(obj);
            }
            
            #endregion

            #region Output Preview

            const float padding = 10;
            Rect previewRect = EditorGUILayout.GetControlRect(false, position.width - padding);
            previewRect.x += padding / 2.0f;
            previewRect.width -= padding;
            EditorGUI.DrawPreviewTexture(previewRect, _preview, Material);
            EditorGUILayout.Space();
            
            #endregion
            
            #region Output Settings
            
            EditorGUI.BeginChangeCheck();
            
            if (_is3D)
                _depth = EditorGUILayout.IntSlider("Depth", _depth, 0, _generated.volumeDepth - 1);
            
            Channel oldChannels = _visibleChannels;
            _visibleChannels = (Channel) EditorGUILayout.EnumFlagsField("View Channels", _visibleChannels);
            Channel changedChannels = oldChannels ^ _visibleChannels;
            if ((changedChannels & Channel.Alpha) == Channel.Alpha)
                _visibleChannels = Channel.Alpha;
            else if ((_visibleChannels & Channel.Alpha) == Channel.Alpha && changedChannels != 0)
                _visibleChannels &= ~Channel.Alpha;
            
            if (EditorGUI.EndChangeCheck())
                RegeneratePreview();

            #endregion
            
            EditorGUILayout.EndScrollView();
        }

        private static Texture3D ToTexture3D(RenderTexture renderTexture)
        {
            Texture3D texture = new(renderTexture.width, renderTexture.height, renderTexture.volumeDepth,
                GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Texture2D[] slices = new Texture2D[renderTexture.volumeDepth];
            for (int i = 0; i < slices.Length; i++)
                slices[i] = ToTexture2D(CopySlice(renderTexture, i));

            Color[] pixels = texture.GetPixels();
            for (int z = 0; z < renderTexture.volumeDepth; z++)
            {
                for (int y = 0; y < renderTexture.height; y++)
                {
                    for (int x = 0; x < renderTexture.width; x++)
                    {
                        int index = x + renderTexture.width * y + renderTexture.width * renderTexture.height * z;
                        pixels[index] = slices[z].GetPixel(x, y);
                    }
                }
            }

            texture.SetPixels(pixels);
            return texture;
        }

        private static RenderTexture CopySlice(RenderTexture texture, int layer)
        {
            RenderTexture output = new(texture.width, texture.height, 24)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGB32,
                graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat
            };
            output.Create();

            ComputeShader shader =
                AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jordantama.procedural-noise/Compute Shaders/GenerationWindow.compute");

            shader.SetInt("layer", layer);
            
            shader.SetTexture(0, "input_3d", texture);
            shader.SetTexture(0, "output_2d", output);

            shader.Dispatch(0, Mathf.Max(1, Mathf.CeilToInt(output.width / 8f)),
                Mathf.Max(1, Mathf.CeilToInt(output.height / 8f)), 1);

            return output;
        }

        private static Texture2D ToTexture2D(RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            Texture2D texture = new(renderTexture.width, renderTexture.height, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Rect region = new(0, 0, renderTexture.width, renderTexture.height);
            texture.ReadPixels(region, 0, 0);
            RenderTexture.active = null;
            return texture;
        }

        private static void Copy(Texture3D source, RenderTexture destination)
        {
            ComputeShader shader =
                AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Compute Shaders/GenerationWindow.compute");
            
            shader.SetTexture(1, "input_3d", source);
            shader.SetTexture(1, "output_3d", destination);

            shader.Dispatch(1,
                Mathf.Max(1, Mathf.CeilToInt(destination.width / 8f)),
                Mathf.Max(1, Mathf.CeilToInt(destination.height / 8f)),
                Mathf.Max(1, Mathf.CeilToInt(destination.volumeDepth / 8f))
            );
        }

        private void RegenerateTexture()
        {
            if (_resolution.x <= 0)
                _resolution.x = Mathf.Max(_resolution.x, 128);
            if (_resolution.y <= 0)
                _resolution.y = Mathf.Max(_resolution.y, 128);

            if (_generated)
                DestroyImmediate(_generated);
            
            _generated = new RenderTexture(_resolution.x, _resolution.y, _is3D ? 0 : 24)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                format = RenderTextureFormat.ARGB32,
                graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
                volumeDepth = _resolution.z,
                dimension = _is3D ? TextureDimension.Tex3D : TextureDimension.Tex2D
            };
            if (!_generated.IsCreated())
                _generated.Create();
            
            if (_is3D)
            {
                string assetPath = "Assets/" + _path + "/" + FileName + ".asset";
                Texture3D existing;
                if ((existing = AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath)) != null)
                    Copy(existing, _generated);
            }
            else
            {
                string assetPath = "Assets/" + _path + "/" + FileName + ".png";
                Texture2D existing;
                if ((existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)) != null)
                    Graphics.Blit(existing, _generated);
            }

            if (_preserveState)
                Random.state = _randomState;

            switch (_settingsEditor.SelectedNoise)
            {
                case SettingsEditor.NoiseType.Perlin:
                    Perlin.Generate(_generated, Settings.PerlinParameters, _rotation);
                    break;
                
                case SettingsEditor.NoiseType.Voronoi:
                    Voronoi.Generate(_generated, Settings.VoronoiParameters);
                    break;
                
                case SettingsEditor.NoiseType.Worley:
                    Worley.Generate(_generated, Settings.WorleyParameters);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!_preserveState)
                _randomState = Random.state;

            RegeneratePreview();
        }

        private void RegeneratePreview()
        {
            if (_preview)
                DestroyImmediate(_preview);

            if (_generated.dimension == TextureDimension.Tex3D)
            {
                _preview = CopySlice(_generated, _depth);
            }
            else
            {
                _preview = new RenderTexture(_generated);
                Graphics.Blit(_generated, _preview);
            }

            Material.SetInt(Mask, (int) _visibleChannels);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetBool(Is3DKey, _is3D);
            EditorPrefs.SetInt(ResolutionKeyX, _resolution.x);
            EditorPrefs.SetInt(ResolutionKeyY, _resolution.y);
            EditorPrefs.SetInt(ResolutionKeyZ, _resolution.z);
            EditorPrefs.SetFloat(RotationKey, _rotation);
            EditorPrefs.SetString(PathKey, _path);
            EditorPrefs.SetString(FileNameKey, _name);
            EditorPrefs.SetBool(AutoNameKey, _autoName);
            EditorPrefs.SetBool(PreserveStateKey, _preserveState);
            EditorPrefs.SetInt(VisibleChannelsKey, (int) _visibleChannels);
        }

        private void LoadSettings()
        {
            _is3D = EditorPrefs.GetBool(Is3DKey);

            _resolution = new Vector3Int(EditorPrefs.GetInt(ResolutionKeyX), EditorPrefs.GetInt(ResolutionKeyY), EditorPrefs.GetInt(ResolutionKeyZ));
            _resolution = Vector3Int.Max(Vector3Int.one, _resolution);

            _rotation = EditorPrefs.GetFloat(RotationKey);
            
            _path = EditorPrefs.GetString(PathKey);
            _name = EditorPrefs.GetString(FileNameKey);
            _autoName = EditorPrefs.GetBool(AutoNameKey);
            _preserveState = EditorPrefs.GetBool(PreserveStateKey);
            _visibleChannels = (Channel) EditorPrefs.GetInt(VisibleChannelsKey);
        }

        [Serializable, Flags]
        private enum Channel
        {
            [InspectorName(null)] Nothing = 0,
            RGB = Red | Green | Blue,
            Red = 1<<0,
            Green = 1<<1,
            Blue = 1<<2,
            Alpha = 1<<3,
            [InspectorName(null)] Everything = ~0
        }
    }
}
