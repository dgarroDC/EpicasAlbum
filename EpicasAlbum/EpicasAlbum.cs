using System;
using System.IO;
using EpicasAlbum.CustomShipLogModes;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class EpicasAlbum : ModBehaviour
{
    public static EpicasAlbum Instance;
    public int texturesLoadedThisFrame; // TODO: Move to AlbumStore?

    private bool _setupDone;
    private ScreenPrompt _uploadPrompt;
    private AlbumStore _store;

    private void Start()
    {
        Instance = this;
        ModHelper.HarmonyHelper.AddPostfix<ShipLogController>("LateInitialize", typeof(EpicasAlbum), nameof(SetupPatch));
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) => _setupDone = false;
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
        EpicasAlbumMode epicasAlbumMode = albumGo.AddComponent<EpicasAlbumMode>();
        epicasAlbumMode.Store = _store;
        epicasAlbumMode.gameObject.name = nameof(EpicasAlbumMode);
        customShipLogModesAPI.AddMode(epicasAlbumMode, () => true, () => EpicasAlbumMode.Name);
    }

    private void Update()
    {
        if (!_setupDone) return;
 
        // TODO: Any probe launcher?
        ProbeLauncher probeLauncher = Locator.GetToolModeSwapper().GetProbeLauncher();
        Image image = probeLauncher._launcherUIs[1]._image; // 0 other?
        _uploadPrompt.SetVisibility(image.enabled);
        if (image.enabled) // TODO: Not on map and menu/paused? Prevent retake? Lockon?
        {
            if (OWInput.IsNewlyPressed(InputLibrary.lockOn))
            {
                _store.Save(probeLauncher._lastSnapshot.ToTexture2D());
                // TODO: Translation
                NotificationData notification = new NotificationData(NotificationTarget.Player, "SNAPSHOT UPLOADED");
                NotificationManager.SharedInstance.PostNotification(notification);
            }
        }
    }

    private void LateUpdate()
    {
        texturesLoadedThisFrame = 0;
    }
}
