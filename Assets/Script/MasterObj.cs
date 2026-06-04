using System.Collections.Generic;
using UnityEngine;

public class MasterObj : MonoBehaviour, IInteractable
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 1f;

    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Renderer[] cachedRenderers;

    [Header("Slave Objs")]
    [SerializeField] private List<GameObject> connectedObjects;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    public void TargetHighlight(bool isLook)
    {
        if (isLook)
        {
            ApplyHighlight();
        }
        else
        {
            RemoveHighlight();
        }
    }

    public void Interact(InteractionDetector player)
    {
        Debug.Log("스위치 작동!");

        // 연결된 모든 오브젝트의 상호작용을 대리 실행
        foreach (var obj in connectedObjects)
        {
            IInteractable target = obj.GetComponent<IInteractable>();
            if (target != null)
            {
                target.Interact(player);
            }
        }
    }

    private void ApplyHighlight()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;

            Material material = renderer.material;

            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = new Material(material);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            }
        }
    }

    private void RemoveHighlight()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;

            if (originalMaterials.ContainsKey(renderer))
            {
                Material originalMaterial = originalMaterials[renderer];
                if (originalMaterial != null)
                {
                    renderer.material = originalMaterial;
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}