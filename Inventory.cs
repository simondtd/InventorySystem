using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Inventory
{
    private int _capacity;
    private List<ItemContainer> _itemContainers;

    public List<ItemContainer> ItemContainers => _itemContainers;
    public int Capacity => _capacity;

    public delegate void InventoryChanged(Inventory inventory = null);
    [field: System.NonSerialized] public event InventoryChanged OnInventoryChanged;

    public Inventory()
    {
        _itemContainers = new List<ItemContainer>();
    }

    public Inventory(int capacity) : this()
    {
        _capacity = capacity;
        _itemContainers = new List<ItemContainer>();

        for (int i = 0; i < _capacity; i++)
        {
            _itemContainers.Add(new ItemContainer(this));
        }
    }

    public bool IsEmpty() => _itemContainers.Where(x => x.Quantity > 0).Count() == 0;

    public bool CanRemoveItemContainers(int quantity)
    {
        for (int i = _itemContainers.Count - 1; i >= _itemContainers.Count - quantity; i--)
        {
            if (_itemContainers[i].Item != null)
            {
                return false;
            }

        }

        return true;
    }

    public bool RemoveItemContainers(int quantity)
    {

        if (!CanRemoveItemContainers(quantity))
            return false;

        int max = _itemContainers.Count - quantity;
        for (int i = _itemContainers.Count - 1; i >= max; i--)
        {
            _itemContainers.RemoveAt(i);
        }

        _capacity = _itemContainers.Count;
        OnInventoryChanged?.Invoke(this);
        return true;
    }

    public void AddItemContainers(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            _itemContainers.Add(new ItemContainer(this));
        }

        _capacity = _itemContainers.Count;
        OnInventoryChanged?.Invoke(this);
    }

    public List<Item> GetAllItems()
    {
        List<Item> items = new List<Item>();

        foreach (ItemContainer itemContainer in _itemContainers)
        {
            items.AddRange(itemContainer.Items);
        }

        return items;
    }

    public int GetNumberOfItem(Item item)
    {
        int num = 0;

        foreach (ItemContainer itemContainer in _itemContainers)
        {
            if (itemContainer.Item != null && itemContainer.Item.Equals(item))
                num += itemContainer.Quantity;
        }

        return num;
    }

    public int GetRoomForItem(Item item)
    {
        int room = 0;

        foreach (ItemContainer itemContainer in _itemContainers)
        {
            room += itemContainer.GetRoomForItem(item);
        }

        return room;
    }

    public void RemoveItemInstance(Item item)
    {
        foreach (ItemContainer itemContainer in _itemContainers)
        {
            if (itemContainer.HasItem(item))
            {
                itemContainer.RemoveItemInstance(item);
                OnInventoryChanged?.Invoke(this);
            }

        }
    }

    public bool HasItem(Item item)
    {
        bool hasItem = false;
        foreach (ItemContainer itemContainer in _itemContainers)
        {
            if (itemContainer.HasItem(item))
            {
                hasItem = true;
                break;
            }
        }
        return hasItem;
    }

    public void CopyContents(Inventory inventory)
    {
        if (_itemContainers.Count != inventory.ItemContainers.Count)
        {
            return;
        }

        for (int i = 0; i < _itemContainers.Count; i++)
        {
            _itemContainers[i].Clear();
            _itemContainers[i].AddItems(inventory.ItemContainers[i].Items);
        }
    }

    public bool AddItem(Item item)
    {
        bool canAddItem = GetRoomForItem(item) > 0;

        if (canAddItem)
        {
            List<ItemContainer> itemContainersWithItem = _itemContainers.Where(x => x.Item != null && x.Item.Equals(item) && x.GetRoomForItem(item) > 0).ToList();
            if (itemContainersWithItem.Count > 0)
            {
                foreach (ItemContainer itemContainer in itemContainersWithItem)
                {
                    if (itemContainer.GetRoomForItem(item) > 0)
                    {
                        itemContainer.AddItem(item);
                        OnInventoryChanged?.Invoke(this);
                        break;
                    }
                }
            }
            else
            {
                foreach (ItemContainer itemContainer in _itemContainers.Where(x => x.Item == null))
                {
                    itemContainer.AddItem(item);
                    OnInventoryChanged?.Invoke(this);
                    break;
                }
            }

        }
        return canAddItem;
    }

    public bool AddItems(List<Item> items)
    {
        bool canAddItems = true;

        foreach (Item item in items)
        {
            if (GetRoomForItem(item) == 0)
            {
                canAddItems = false;
                break;
            }
        }

        if (canAddItems)
            items.ForEach(x => AddItem(x));

        OnInventoryChanged?.Invoke(this);

        return canAddItems;
    }

    public int RemoveItems(Item item, int quantity = 1)
    {
        int removed = 0;
        foreach (ItemContainer itemContainer in _itemContainers.Where(x => x.Item != null && x.Item.Equals(item)))
        {
            removed += itemContainer.RemoveItems(quantity).Count();
            break;
        }

        OnInventoryChanged?.Invoke(this);

        return removed;
    }

    public override string ToString()
    {
        string s = "";
        foreach (ItemContainer container in _itemContainers)
        {
            string itemName = container.Item != null ? container.Item.ItemName : "NULL";
            s += $"ITEM {_itemContainers.IndexOf(container)}: {itemName}\n";
        }
        return s;
    }

    public static void Merge(ItemContainer from, ItemContainer to, int quantity)
    {
        if (from == to)
        {
            return;
        }

        //Move from from to to
        else if (from.Item.Equals(to.Item) && to.GetRoomForItem(from.Item) > 0)
        {
            int takes = System.Math.Clamp(quantity, 0, to.GetRoomForItem(from.Item));
            List<Item> items = from.RemoveItems(takes);
            to.AddItems(items);
        }
        //Swapping items
        else if (!from.Item.Equals(to.Item) || to.GetRoomForItem(from.Item) == 0)
        {
            List<Item> toItems = to.Items;
            List<Item> fromItems = from.Items;

            to.SetItems(fromItems);
            from.SetItems(toItems);
        }

        if (from.Inventory == to.Inventory)
        {
            from.Inventory.OnInventoryChanged?.Invoke(from.Inventory);
        }
        else
        {
            from.Inventory.OnInventoryChanged?.Invoke(from.Inventory);
            to.Inventory.OnInventoryChanged?.Invoke(to.Inventory);
        }

    }

}

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