using System;
using System.IO;
using System.Linq;
using EpicasAlbum.API;
using EpicasAlbum.CustomShipLogModes;
using OWML.ModHelper;
using UnityEngine;

namespace EpicasAlbum;

public class EpicasAlbum : ModBehaviour
{
    public static EpicasAlbum Instance;
    public int texturesLoadedThisFrame; // TODO: Move to AlbumStore?

    private bool _setupDone;
    private ScreenPrompt _uploadPrompt;
    private AlbumStore _store;
    private EpicasAlbumMode _epicasAlbumMode;

    private void Start()
    {
        Instance = this;
        ModHelper.HarmonyHelper.AddPostfix<ShipLogController>("LateInitialize", typeof(EpicasAlbum), nameof(SetupPatch));
        LoadManager.OnCompleteSceneLoad += (_, _) => _setupDone = false;
        GlobalMessenger<ProbeCamera>.AddListener("ProbeSnapshot", OnProbeSnapshot);
    }

    private void OnProbeSnapshot(ProbeCamera camera)
    {
        ModHelper.Console.WriteLine($"ON SNAPSHOT={GetGameObjectPath(camera.gameObject)}");

        Transform probeTransform = camera.transform;
        SurveyorProbe probe = null;
        while (!probeTransform.TryGetComponent<SurveyorProbe>(out probe))
        {
            probeTransform = probeTransform.parent;
        }

        Sector sector = probe.GetSectorDetector().GetLastEnteredSector();
        ModHelper.Console.WriteLine($"ON SNAPSHOT={GetGameObjectPath(camera.gameObject)},{GetGameObjectPath(sector.gameObject)},{GetGameObjectPath(sector.GetOWRigidbody().gameObject)}");
    }

    
    public static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }
    public override object GetApi() {
        return new EpicasAlbumAPI();
    }
 
    private static void SetupPatch() {
        Instance.Setup();
    }

    private void Setup()
    {
        // TODO: Translation
        _uploadPrompt = new ScreenPrompt(InputLibrary.lockOn, "Upload Snapshot");
        Locator.GetPromptManager().AddScreenPrompt(_uploadPrompt, PromptPosition.UpperRight);
        // Same Gamepass profile name as New Horizons
        string profileName = StandaloneProfileManager.SharedInstance?.currentProfile?.profileName ?? "XboxGamepassDefaultProfile";
        _store = new AlbumStore(Path.Combine(ModHelper.Manifest.ModFolderPath, "snapshots", profileName));
        CreateMode();
        _setupDone = true;
    }
    
    public void CreateMode()
    {
        ICustomShipLogModesAPI customShipLogModesAPI = ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
        
        GameObject shipLogCanvas = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas/");
        GameObject albumGo = new GameObject(nameof(EpicasAlbumMode));
        albumGo.transform.SetParent(shipLogCanvas.transform);
        _epicasAlbumMode = albumGo.AddComponent<EpicasAlbumMode>();
        _epicasAlbumMode.Store = _store;
        customShipLogModesAPI.ItemListMake(true, false, itemList =>
        {
            itemList.name = "EpicasAlbumList";
            _epicasAlbumMode.ItemList = new ItemListWrapper(customShipLogModesAPI, itemList);
            customShipLogModesAPI.AddMode(_epicasAlbumMode, () => true, () => EpicasAlbumMode.Name);
        });
    }

    private void Update()
    {
        if (!_setupDone) return;

        UpdateSnapshotUpload();
 
        if (_epicasAlbumMode.IsActiveButNotCurrent())
        {
            _epicasAlbumMode.UpdateMode();
        }
    }

    private void UpdateSnapshotUpload()
    {
        bool enabled = false;
        ProbeLauncher activeLauncher = null;

        ToolModeSwapper toolModeSwapper = Locator.GetToolModeSwapper();
        if (toolModeSwapper.IsInToolMode(ToolMode.Probe))
        {
            // This could be either the player one or the ship one
            activeLauncher = toolModeSwapper._equippedTool as ProbeLauncher;
            if (activeLauncher.AllowInput())
            {
                enabled = activeLauncher.AllowInput() && activeLauncher._launcherUIs.Any(ui => ui._image.enabled);
            }
        }

        _uploadPrompt.SetVisibility(enabled);
        if (enabled)
        {
            // Conflict with lock om. So it goes.
            if (OWInput.IsNewlyPressed(InputLibrary.lockOn))
            {
                _store.Save(activeLauncher._lastSnapshot.ToTexture2D());
                // TODO: Translation
                NotificationData notification = new NotificationData(NotificationTarget.All, "SNAPSHOT UPLOADED");
                NotificationManager.SharedInstance.PostNotification(notification);
            }
        }
    }

    private void LateUpdate()
    {
        texturesLoadedThisFrame = 0;
    }

    public void OpenSnapshotChooserDialog(string defaultSnapshotName, Action<string> selectedSnapshotNameConsumer)
    {
        _epicasAlbumMode.OpenSnapshotChooserDialog(defaultSnapshotName, selectedSnapshotNameConsumer);
    }

    public Sprite GetSnapshotSprite(string snapshotName)
    {
        // TODO: Add a way to no bypass it? What null would mean if not found?
        return _store.GetSprite(snapshotName, true);
    }
}
