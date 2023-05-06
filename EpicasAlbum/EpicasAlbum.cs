using System.IO;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class EpicasAlbum : ModBehaviour
{
    private static EpicasAlbum _instance;

    private bool _setupDone;
    private ScreenPrompt _uploadPrompt;
    private AlbumStore _store;

    private void Start()
    {
        _instance = this;
        ModHelper.HarmonyHelper.AddPostfix<ShipLogController>("LateInitialize", typeof(EpicasAlbum), nameof(SetupPatch));
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) => _setupDone = false;
    }
    
    private static void SetupPatch() {
        _instance.Setup();
    }

    private void Setup()
    {
        // TODO: Translation
        _uploadPrompt = new ScreenPrompt(InputLibrary.lockOn, "Upload Snapshot");
        Locator.GetPromptManager().AddScreenPrompt(_uploadPrompt, PromptPosition.UpperRight);
        // Same Gamepass profile name as New Horizons
        string profileName = StandaloneProfileManager.SharedInstance?.currentProfile?.profileName ?? "XboxGamepassDefaultProfile";
        _store = new AlbumStore(Path.Combine(ModHelper.Manifest.ModFolderPath, "snapshots", profileName));
        _setupDone = true;
    }

    private void Update()
    {
        if (!_setupDone) return;
 
        // TODO: Any probe launcher?
        ProbeLauncher probeLauncher = Locator.GetToolModeSwapper().GetProbeLauncher();
        Image image = probeLauncher._launcherUIs[1]._image; // 0 other?
        _uploadPrompt.SetVisibility(image.enabled);
        if (image.enabled)
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
}