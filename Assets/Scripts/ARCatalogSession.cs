using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keep tracking, placed objects and the selected target alive while configuring.
public sealed class ARCatalogSession : MonoBehaviour
{
    private static ARCatalogSession active;
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private Scene arScene;
    private ARPlacementController placement;
    private bool opening;
    private bool returning;

    public static void Open(ARPlacementController controller)
    {
        if (controller == null || active != null) return;
        var session = controller.GetComponent<ARCatalogSession>();
        if (session == null) session = controller.gameObject.AddComponent<ARCatalogSession>();
        session.placement = controller;
        session.arScene = controller.gameObject.scene;
        active = session;
        session.returning = false;
        CatalogSelectionSession.ReturnToAR = true;
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(
            CatalogSelectionSession.CurrentConfiguration ?? CurtainViewSettingsStore.LoadLast());
        foreach (GameObject root in session.arScene.GetRootGameObjects())
            foreach (Behaviour component in root.GetComponentsInChildren<Behaviour>(true))
                if (component.enabled && (component is Canvas || component is GraphicRaycaster ||
                    component is ARPlacementController || component is ARObjectManipulator))
                {
                    session.suspended.Add(component);
                    component.enabled = false;
                }
        session.opening = true;
        try
        {
            SceneManager.LoadSceneAsync("Catalog", LoadSceneMode.Additive).completed += _ =>
            {
                session.opening = false;
                Scene catalog = SceneManager.GetSceneByName("Catalog");
                if (catalog.IsValid() && catalog.isLoaded) SceneManager.SetActiveScene(catalog);
            };
        }
        catch
        {
            session.opening = false;
            session.Restore();
            throw;
        }
    }

    public static bool ReturnToAR()
    {
        if (active == null || active.opening || !active.arScene.isLoaded) return false;
        if (active.returning) return true;
        ARCatalogSession session = active;
        session.returning = true;
        Scene catalog = SceneManager.GetSceneByName("Catalog");
        // Finish cleanup/persistence before accepting another AR touch.
        session.placement.ApplyCatalogSelection();
        SceneManager.SetActiveScene(session.arScene);
        CatalogSelectionSession.ReturnToAR = false;
        if (catalog.IsValid() && catalog.isLoaded)
            SceneManager.UnloadSceneAsync(catalog).completed += _ => session.Restore();
        else session.Restore();
        return true;
    }

    private void Restore()
    {
        foreach (Behaviour component in suspended)
            if (component != null) component.enabled = true;
        suspended.Clear();
        if (arScene.IsValid() && arScene.isLoaded)
            foreach (GameObject root in arScene.GetRootGameObjects())
                foreach (ARSurfaceUI surfaces in root.GetComponentsInChildren<ARSurfaceUI>(true)) surfaces.RefreshVisibility();
        active = null;
        CatalogSelectionSession.ReturnToAR = false;
    }

    private void OnDestroy() { if (active == this) active = null; }
}
