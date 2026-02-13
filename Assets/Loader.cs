using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArtNet;
using Klak.Spout;
using SFB;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static TextureWriter;

public class Loader : MonoBehaviour
{
    public static List<IDMXSerializer> serializers;

    //dmx generator source
    public static List<IDMXGenerator> generators;
    public static List<IExporter> exporters;
    private static List<InterfaceList> interfaceLists;
    public static SpoutReceiver spoutReceiver;
    public static SpoutSender spoutSender;
    public static TextureWriter textureWriter;
    public static TextureReader textureReader;
    public static ArtNetReceiver artNetReceiver;
    public static DMXPreviewToggle previewToggle;

    public static ShowConfiguration showconf = new ShowConfiguration();

    IDeserializer ymldeserializer;
    ISerializer ymlserializer;
    public UIController uiController;

    void Start()
    {
        //TODO: Make this configurable. This is here because even though its not resizable, unity can get in a fucked state and remember the wrong resolution
        Screen.SetResolution(1200, 600, false);
        spoutReceiver = FindObjectOfType<SpoutReceiver>();
        spoutSender = FindObjectOfType<SpoutSender>();
        artNetReceiver = FindObjectOfType<ArtNetReceiver>();
        previewToggle = FindAnyObjectByType<DMXPreviewToggle>();
        textureWriter = FindObjectOfType<TextureWriter>();
        textureReader = FindObjectOfType<TextureReader>();

        //load in all the serializers
        serializers = GetAllInterfaceImplementations<IDMXSerializer>();
        generators = GetAllInterfaceImplementations<IDMXGenerator>();
        exporters = GetAllInterfaceImplementations<IExporter>();

        //startup all interface lists
        interfaceLists = FindObjectsByType<InterfaceList>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        foreach (var interfaceList in interfaceLists)
        {
            interfaceList.Startup();
        }

        Debug.Log($"Loaded {serializers.Count} serializers");

        //default the serializers to VRSL and have transcode off
        showconf.Serializer = new VRSL();
        showconf.Deserializer = new VRSL();
        //showconf.Transcode = false;
        showconf.TranscodeUniverseCount = 3;
        showconf.SerializeUniverseCount = int.MaxValue;

        //setup framerate
        SetFramerate(showconf.TargetFramerate);

        uiController.OnSave += SaveShowConfiguration;
        uiController.OnLoad += LoadShowConfiguration;

        //select the first serializer by default
        uiController.InvalidateUIState();

        //setup yml serializer and deserializer
        ymlserializer = SetupBuilder<SerializerBuilder>().Build();
        ymldeserializer = SetupBuilder<DeserializerBuilder>().Build();

        SetupDynamicUI();
    }

    void Update()
    {
        foreach (var interfaceList in interfaceLists)
        {
            interfaceList.UpdateInterface(showconf.Generators.OfType<IUserInterface<IDMXGenerator>>().ToList());
            interfaceList.UpdateInterface(showconf.Exporters.OfType<IUserInterface<IExporter>>().ToList());
        }
    }

    private T SetupBuilder<T>() where T : BuilderSkeleton<T>, new()
    {
        var builder = new T()
            .WithNamingConvention(CamelCaseNamingConvention.Instance);

        //find ALL usages of TagMappedAttribute and add them to the builder

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => Attribute.IsDefined(p, typeof(TagMappedAttribute)));

        //now, types may just be interfaces
        //in this case, we need to find all implementations of that interface and add those too, upwards
        var additionalTypes = new List<Type>();
        foreach (var type in types)
        {
            if (type.IsInterface)
            {
                var impls = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
                additionalTypes.AddRange(impls);
            }
        }
        //combine them
        var allTypes = types.Concat(additionalTypes).Distinct().ToList();
        foreach (var type in allTypes)
        {
            Debug.Log("" + type.FullName);
            builder.WithTagMapping("!" + type.Name, type);
        }

        return builder;
    }

    private List<T> GetAllInterfaceImplementations<T>()
    {
        var type = typeof(T);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p));

        return types
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Select(t => (T)Activator.CreateInstance(t))
            .ToList();
    }

    public void SaveShowConfiguration()
    {
        var yaml = ymlserializer.Serialize(showconf);

        yaml = @"# All channel values can be represented in 2 ways
# Either as a global integer, so 0 upwards like an array
# As a direct Universe.Channel mapping, so Universe 3 channel 5 is 3.5
# Alongside this, Equations are usable. So (3 * 2).(5 * 5) works

" + yaml;

        //open save file dialog
        var extensionList = new[] {
            new ExtensionFilter("Show Configurations", "shwcfg"),
        };
        var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "NewShowConfig", extensionList);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Save cancelled or path is empty.");
            return;
        }

        //write the yaml to the file
        System.IO.File.WriteAllText(path, yaml);
    }

    public void LoadShowConfiguration()
    {
        //open a file dialog
        var extensions = new[] {
            new ExtensionFilter("Show Configurations", "shwcfg"),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length != 1)
        {
            Debug.LogError("Please select exactly one file.");
            return;
        }

        //read from the first path
        var content = System.IO.File.ReadAllText(paths[0]);

        if (string.IsNullOrEmpty(content))
        {
            Debug.LogError("File is empty or not found.");
            return;
        }

        UnloadShowConf();

        showconf = ymldeserializer.Deserialize<ShowConfiguration>(content);

        //invalidate the dropdowns and toggles
        uiController.InvalidateUIState();

        //start coroutine
        //this is stupid dumb shit but this YML library is being weird and this fixes the issue
        StartCoroutine(DeferredLoad(content));
    }

    public static void UnloadShowConf()
    {
        //deconstruct all generators before we lose references to them
        foreach (var generator in showconf.Generators)
        {
            generator.DeconstructUserInterface();
            generator.Deconstruct();
        }

        foreach (var exporter in showconf.Exporters)
        {
            exporter.DeconstructUserInterface();
            exporter.Deconstruct();
        }

        showconf.Serializer.Deconstruct();
        showconf.Deserializer.Deconstruct();
    }

    IEnumerator DeferredLoad(string content)
    {
        //returning 0 will make it wait 1 frame
        yield return new WaitForEndOfFrame();

        //yayyyyy double load to fix dumb race condition bullshit
        showconf = ymldeserializer.Deserialize<ShowConfiguration>(content);

        LoadShowConf();
    }

    public static void ReloadShowConf()
    {
        UnloadShowConf();
        LoadShowConf();
    }

    public static void LoadShowConf()
    {
        //run initialization on all generators
        foreach (var generator in showconf.Generators)
        {
            generator.Construct();
        }

        foreach (var exporter in showconf.Exporters)
        {
            exporter.Construct();
        }

        showconf.Serializer.Construct();
        showconf.Deserializer.Construct();

        //setup spout input/outputs
        spoutSender.spoutName = showconf.SpoutOutputName;
        spoutReceiver.sourceName = showconf.SpoutInputName;

        //get the input render texture from the spout receiver
        var inputTexture = spoutReceiver.targetTexture;

        //modify the render texture size
        UpdateRenderTextureSize(inputTexture, showconf.InputResolution);
        textureWriter.ChangeResolution(showconf.OutputResolution);
        textureReader.ChangeResolution(showconf.InputResolution);

        artNetReceiver.ChangePort(showconf.ArtNetPort);
        artNetReceiver.ChangeIPAddress(showconf.ArtNetAddress);

        SetFramerate(showconf.TargetFramerate);

        previewToggle.SetPreviewChromaColor(showconf.PreviewChromaColor);

        SetupDynamicUI();
    }

    private static void UpdateRenderTextureSize(RenderTexture rt, Resolution resolution)
    {
        if (rt.width != resolution.width || rt.height != resolution.height)
        {
            rt.Release();
            rt.width = resolution.width;
            rt.height = resolution.height;
            rt.Create();
        }
    }

    //TODO: Should probably be moved to UIController but eh
    private static void SetupDynamicUI()
    {
        //get all InterfaceList and initialize them
        foreach (var interfaceList in interfaceLists)
        {
            //cursed but just try to init with all of them, filter on the interfacelist side
            interfaceList.Initialize(showconf.Exporters, exporters, Delete(showconf.Exporters), Swap(showconf.Exporters), Add(showconf.Exporters));
            interfaceList.Initialize(showconf.Generators, generators, Delete(showconf.Generators), Swap(showconf.Generators), Add(showconf.Generators));
        }

        Action<Type> Add<T>(List<T> list) where T : IConstructable
        {
            return (type) =>
            {
                //add a new generator of the type
                var generator = (T)Activator.CreateInstance(type);
                generator.Construct();
                //do NOT construct the user interface here, it will be done in the InterfaceList itself
                list.Add(generator);

                //setup UI again to refresh everything
                SetupDynamicUI();
            };
        }

        Action<int, int> Swap<T>(List<T> list) where T : IConstructable
        {
            return (index1, index2) =>
            {
                //swap the two items in the list
                //funny intellisense tuple trick
                (list[index2], list[index1]) = (list[index1], list[index2]);

                //setup UI again to refresh everything
                SetupDynamicUI();
            };
        }

        Action<int> Delete<T>(List<T> list) where T : class, IConstructable, IUserInterface<T>
        {
            return (index) =>
            {
                //when called, remove the type from the show configuration
                var item = list[index];
                item.DeconstructUserInterface();
                item.Deconstruct();
                list.RemoveAt(index);

                //setup UI again to refresh everything
                SetupDynamicUI();
            };
        }
    }

    private static void SetFramerate(int targetFramerate)
    {
        //check bounds
        if (targetFramerate < 1 || targetFramerate > 60)
        {
            Debug.LogWarning($"Target framerate {targetFramerate} is out of bounds. Setting to default 60.");
            targetFramerate = 60;
        }
        //setup target framerate
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFramerate;
    }
}
