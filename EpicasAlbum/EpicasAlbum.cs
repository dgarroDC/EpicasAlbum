using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicasAlbum.CustomShipLogModes;
using OWML.ModHelper;
using Starfield;
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

    private List<Tuple<Vector3, RectTransform, GameObject>> _stars = new();

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
        customShipLogModesAPI.ItemListMake(true, false, itemList =>
        {
            itemList.name = "EpicasAlbumList";
            epicasAlbumMode.ItemList = new ItemListWrapper(customShipLogModesAPI, itemList);
            customShipLogModesAPI.AddMode(epicasAlbumMode, () => true, () => EpicasAlbumMode.Name);
        });

        StarfieldController starfieldController = FindObjectOfType<StarfieldController>();
        StarGroup[] groups = starfieldController._starfieldData.starGroups;
        StarInstance[] stars = groups[5].stars;
        Canvas canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        foreach (StarInstance starInstance in stars)
        {
            GameObject star = new GameObject("Star", typeof(Image));
            RectTransform rect = star.GetComponent<RectTransform>();
            rect.transform.parent = canvas.transform;
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = starfieldController.transform;
            _stars.Add(new Tuple<Vector3, RectTransform, GameObject>(starInstance.position, rect, sphere));
        }
    }

    private void Update()
    {
        if (!_setupDone) return;

        Camera cam = Locator.GetActiveCamera().mainCamera;
        bool p = OWInput.IsNewlyPressed(InputLibrary.autopilot);
        Locator.GetCenterOfTheUniverse().RecenterUniverseAroundPlayer();
        foreach (var tuple in _stars)
        {
            Vector3 pos = cam.WorldToViewportPoint(tuple.Item1);
            tuple.Item2.anchoredPosition = new Vector2(1920 * pos.x, 1080 * pos.y);
            tuple.Item3.transform.position = tuple.Item1;// - Locator.GetPlayerTransform().position;
            if (p)
            {
                tuple.Item3.SetActive(!tuple.Item3.activeSelf);
            }
        }
        
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
}
