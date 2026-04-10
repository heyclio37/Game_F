using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemData")]
public class ItemData :ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public Sprite icon;
}
