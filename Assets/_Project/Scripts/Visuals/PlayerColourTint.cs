using FreezeOrThrow.Networking;
using Unity.Netcode;
using UnityEngine;

namespace FreezeOrThrow.Visuals
{
    public class PlayerColourTint : MonoBehaviour
    {
        [SerializeField] Renderer[] tintedRenderers;
        [SerializeField] Color[] slotColours =
        {
            new Color32(0xC9, 0x46, 0x3D, 0xFF),
            new Color32(0x3E, 0x5B, 0xA9, 0xFF),
            new Color32(0x4E, 0x9A, 0x5B, 0xFF),
            new Color32(0xE8, 0xB6, 0x3A, 0xFF),
        };

        NetworkObject playerObject;
        PlayerList playerList;
        MaterialPropertyBlock propertyBlock;
        int baseColourProperty;

        void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            baseColourProperty = Shader.PropertyToID("_BaseColor");
        }

        void Start()
        {
            playerObject = GetComponentInParent<NetworkObject>();
            if (playerObject == null || !playerObject.IsSpawned)
            {
                return;
            }

            playerList = FindFirstObjectByType<PlayerList>();
            if (playerList != null)
            {
                playerList.PlayersChanged += ApplySlotColour;
            }

            ApplySlotColour();
        }

        void OnDestroy()
        {
            if (playerList != null)
            {
                playerList.PlayersChanged -= ApplySlotColour;
            }
        }

        public void SetColour(Color colour)
        {
            foreach (Renderer tintedRenderer in tintedRenderers)
            {
                tintedRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(baseColourProperty, colour);
                tintedRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        void ApplySlotColour()
        {
            int playerSlot = playerList != null && playerList.IsSpawned ? playerList.SlotOf(playerObject.OwnerClientId) : -1;
            if (playerSlot < 0)
            {
                playerSlot = (int)(playerObject.OwnerClientId % (ulong)slotColours.Length);
            }

            SetColour(slotColours[playerSlot % slotColours.Length]);
        }
    }
}
