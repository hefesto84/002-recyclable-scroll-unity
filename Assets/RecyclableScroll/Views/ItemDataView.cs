using RecyclableScroll.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace RecyclableScroll.Views
{
    public class ItemDataView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        public void Init()
        {
            
        }

        public void SetData(ItemData data)
        {
            text.text = $"ID : {data.Id}";
            name = $"Item - {data.Id}";
        }
    }
}