[System.Serializable]
public class Item
{
    protected SerializableGuid _guid;
    protected int _id = 0;
    protected string _itemName;
    protected int _maxStack = 5;
    protected string _description;

    public string ItemName =>_itemName;
    public int MaxStack => _maxStack;
    public int Id => _id;
    public SerializableGuid Guid => _guid;

    public Item(int id, string itemName, int maxStack, string description)
    {
        _guid = System.Guid.NewGuid();

        _id = id;
        _itemName = itemName;
        _maxStack = maxStack;
        _description = description;
    }


}
