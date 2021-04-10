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
