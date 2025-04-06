using System.Collections.Generic;
using RecyclableScroll.Components;
using RecyclableScroll.Models;
using RecyclableScroll.Views;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private Button btn1K;
    [SerializeField] private Button btn50K;
    [SerializeField] private Button btn250K;
    [SerializeField] private Button btn1M;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private ItemDataView defaultItem;
    
    private List<ItemData> _itemDataList;
    private RecycleItemsComponent<ItemDataView> _recyclableItems;
    
    private void Awake()
    {
        btn1K.onClick.AddListener( () => HydrateData(1000));
        btn50K.onClick.AddListener( () => HydrateData(50000));
        btn250K.onClick.AddListener( () => HydrateData(250000));
        btn1M.onClick.AddListener( () => HydrateData(1000000));
    }

    private void OnDestroy()
    {
        btn1K.onClick.RemoveAllListeners();
        btn50K.onClick.RemoveAllListeners();
        btn250K.onClick.RemoveAllListeners();
        btn1M.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        InitScrollView();
        HydrateData(25);
    }
    
    private void HydrateData(int data)
    {
        _itemDataList ??= new List<ItemData>();

        _itemDataList.Clear();
        
        for (var i = 0; i < data; i++)
        {
            _itemDataList.Add(new ItemData { Id = i});
        }
        
        Debug.Log($"{_itemDataList.Count} elements created and added to the ScrollView.");
        
        _recyclableItems.SetItemsCount(_itemDataList.Count);
    }

    private void InitScrollView()
    {
        _recyclableItems = new RecycleItemsComponent<ItemDataView>(defaultItem, scrollRect, 
            (item, index) => { item.Init(); }, OnUpdateScrollView);
        
        _recyclableItems.SetItemsCount(25);
    }

    private void OnUpdateScrollView(ItemDataView view, int index)
    {
        if (_itemDataList == null) return;
        
        view.SetData(_itemDataList[index]);
    }
    
}
