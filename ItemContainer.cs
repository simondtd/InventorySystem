using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ItemContainer
{
    private Inventory _inventory;
    private List<Item> _items;

    public Inventory Inventory => _inventory;
    public List<Item> Items => _items;
    public Item Item => _items.Count > 0 ? _items[_items.Count - 1] : null;
    public int Quantity => _items.Count;

    public delegate void ItemContainerChanged(Item item);
    [field: System.NonSerialized] public event ItemContainerChanged OnItemContainerChanged;

    public delegate void ItemQuantityChanged(int change);
    [field: System.NonSerialized] public event ItemQuantityChanged OnItemQuantityChanged;

    public void SetItems(List<Item> items)
    {
        _items = items;

        OnItemQuantityChanged?.Invoke(items.Count);
        OnItemContainerChanged?.Invoke(Item);
    }

    public ItemContainer(Inventory inventory)
    {
        _inventory = inventory;
        _items = new List<Item>();
    }

    public void TriggerOnChangeEvent() => OnItemContainerChanged?.Invoke(Item);

    public void RemoveItemInstance(Item item)
    {
        int index = -1;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Guid.Equals(item.Guid))
                index = i;
        }

        if (index != -1)
        {
            _items.RemoveAt(index);
            OnItemContainerChanged?.Invoke(item);
            OnItemQuantityChanged?.Invoke(-1);
        }

    }

    public List<Item> RemoveItems(int quantity)
    {
        Item item = Item;

        quantity = System.Math.Clamp(quantity, 0, Quantity);
        List<Item> items = new List<Item>();

        for (int i = 0; i < quantity; i++)
        {
            items.Add(_items[_items.Count - 1]);
            _items.RemoveAt(_items.Count - 1);
        }

        OnItemContainerChanged?.Invoke(item);
        OnItemQuantityChanged?.Invoke(items.Count);

        return items;
    }

    public bool AddItems(List<Item> items)
    {
        foreach (Item item in items)
        {
            if (!AddItem(item))
                return false;
        }

        return true;
    }

    public bool AddItem(Item item)
    {
        bool addedItem = false;
        if (GetRoomForItem(item) > 0)
        {
            _items.Add(item);
            addedItem = true;
        }

        if (addedItem)
        {
            OnItemQuantityChanged?.Invoke(1);
            OnItemContainerChanged?.Invoke(Item);
        }


        return addedItem;
    }

    public int GetRoomForItem(Item item)
    {
        int room = 0;

        if (Item == null)
            room = item.MaxStack;
        else if (Item.Equals(item))
            room = item.MaxStack - Quantity;

        return room;
    }

    public bool HasItem(Item item)
    {
        return (_items.Where(x => x.Guid.Equals(item.Guid)).Count() == 1);
    }

    public void Clear()
    {
        Item item = Item;
        int quantity = Quantity;
        _items.Clear();
        OnItemQuantityChanged?.Invoke(quantity);
        OnItemContainerChanged?.Invoke(item);
    }
}