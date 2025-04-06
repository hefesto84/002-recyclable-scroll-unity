using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RecyclableScroll.Components
{
     public class RecycleItemsComponent<T> where T : Component
	{
		private T[] Items { get; set; }
		
		private readonly ScrollRect _scrollRect;
		private readonly Action<T, int> _onUpdate;
		private readonly T _basePrefab;
		
		private LayoutGroup _layoutGrp;
		private ContentSizeFitter _contentSizeFitter;
		private Vector2 _itemSize, _itemSpace;
		
		private int[] _indexArray;
		private int _listItemsCount;
		private int _itemsCount;
		private int _itemsPerEntry;
		private float _itemsSize;
		private float _viewportSize;
		
		public RecycleItemsComponent(T basePrefab, ScrollRect scrollRect, Action<T, int> onItemInitialized, Action<T, int> onUpdate)
		{
			_basePrefab = basePrefab;
			_scrollRect = scrollRect;
			_onUpdate = onUpdate;

			ResolveDependencies();
			
			SetListeners();
		
			UpdateGridLayout(_layoutGrp as GridLayoutGroup);

			InstantiateItems(onItemInitialized);
		}

		/// <summary>
		/// This method is in charge of the instantiation of the prefabs. Notice that never instantiates as many prefabs as elements we have in the list, just
		/// the minimum to fill the viewport and give the feeling that all elements are there.
		/// </summary>
		/// <param name="onItemInitialized"></param>
		private void InstantiateItems(Action<T, int> onItemInitialized)
		{
			Items = new T[_listItemsCount];
			_indexArray = new int[_listItemsCount];
			
			var startFromIndex = 0;
			
			if (_basePrefab.transform.parent == _scrollRect.content)
			{
				Items[0] = _basePrefab;
				_indexArray[0] = 0;
				startFromIndex = 1;
			}
			
			for (var i = startFromIndex; i < _listItemsCount; ++i)
			{
				Items[i] = Object.Instantiate(_basePrefab, _scrollRect.content);
				
				_indexArray[i] = i;

				if(onItemInitialized == null) continue;
				
				onItemInitialized(Items[i], i);
			}
		}
		
		/// <summary>
		/// This method checks if the dependencies are correct. In our case, we will need a LayoutGroup and a ContentSizeFitter.
		/// Is not possible to deal with the recycling of the elements without a ContentSizeFitter because we need to know
		/// how big is the content of the scroll
		/// </summary>
		/// <exception cref="Exception"></exception>
		private void ResolveDependencies()
		{
			var root = _scrollRect.content;
			
			_layoutGrp = root.GetComponent<LayoutGroup>();
			_contentSizeFitter = root.GetComponent<ContentSizeFitter>();

			if (_layoutGrp == null || _contentSizeFitter == null)
				throw new Exception("LayoutGroup or ContentSizeFitter weren't bind.");
		}
		
		/// <summary>
		/// We are attached to the OnValueChanged which is raised when the scroll position is modified.
		/// </summary>
		private void SetListeners() => _scrollRect.onValueChanged.AddListener(OnScroll);
		
		/// <summary>
		/// We need to detach us from the OnValueChanged to avoid having a non-valid method assigned to an event
		/// In certain cases we may get a Null Reference Exception if we don't do this.
		///
		/// Of course, all the elements have to been destroyed.
		/// </summary>
		public void Unload()
		{
			_scrollRect.onValueChanged.RemoveListener(OnScroll);
			
			for (var i = Items.Length - 1; i >= 0; i--)
				Object.Destroy(Items[i]);
		}

		/// <summary>
		/// This helps us to determine the size of the content, based on how many items we have.
		/// This value, should be set by the user of this script.
		/// </summary>
		/// <param name="maxItemsCount"></param>
		public void SetItemsCount(int maxItemsCount)
		{
			UpdateGridLayout(_layoutGrp as GridLayoutGroup);

			_itemsCount = maxItemsCount;
			_scrollRect.normalizedPosition = _scrollRect.content.pivot;

			for (int i = 0, count = Items.Length; i < count; i++)
				Items[i].gameObject.SetActive(false);

			_contentSizeFitter.enabled = _layoutGrp.enabled = true;

			for (var i = 0; i < _listItemsCount; ++i)
			{
				var show = (i < _itemsCount);

				Items[i].gameObject.SetActive(show);
				_indexArray[i] = i;
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
			_contentSizeFitter.enabled = _layoutGrp.enabled = false;

			ModifyContentSize(_scrollRect);
			
			UpdateItems(true);
		}

		/// <summary>
		/// Sets or modify the size of the content based on the items we have in the list.
		/// </summary>
		/// <param name="scrollRect"></param>
		private void ModifyContentSize(ScrollRect scrollRect)
		{
			var contentSize = new Vector2(_layoutGrp.padding.left + _layoutGrp.padding.right, _layoutGrp.padding.top + _layoutGrp.padding.bottom);
			var itemsPerEntry = Mathf.CeilToInt((float)_itemsCount/_itemsPerEntry);

			if (scrollRect.horizontal)
			{
				contentSize.x += (_itemSize.x*itemsPerEntry) + (_itemSpace.x*(itemsPerEntry - 1));

				if (_layoutGrp is GridLayoutGroup)
					contentSize.y += (_itemSize.y*_itemsPerEntry) + (_itemSpace.y*(_itemsPerEntry -1));
			}
			else
			{
				contentSize.y += (_itemSize.y*itemsPerEntry) + (_itemSpace.y*(itemsPerEntry - 1));

				if (_layoutGrp is GridLayoutGroup)
					contentSize.x += (_itemSize.x*_itemsPerEntry) + (_itemSpace.x*(_itemsPerEntry - 1));
			}

			scrollRect.content.sizeDelta = contentSize;
		}
		
		/// <summary>
		/// As we are modifying the position of the items to "fake" where are positioned, we need to update the layout manually
		/// </summary>
		/// <param name="gridLayout"></param>
		private void UpdateGridLayout(GridLayoutGroup gridLayout)
		{
			var itemSizeWithSpacing = 0f;

			if (gridLayout != null)
			{
				_itemSize = gridLayout.cellSize;
				_itemSpace = gridLayout.spacing;

				_scrollRect.content.anchorMax = _scrollRect.content.anchorMin = (_scrollRect.content.anchorMax + _scrollRect.content.anchorMin) * 0.5f;
			}

			if (_layoutGrp is HorizontalOrVerticalLayoutGroup group)
			{
				_itemSize = ((RectTransform) _basePrefab.transform).rect.size;
				_itemSpace = Vector2.zero;

				if (_scrollRect.horizontal)
					_itemSpace.x = group.spacing;
				else
					_itemSpace.y = group.spacing;
			}

			var viewportRect = _scrollRect.viewport.rect;
			
			itemSizeWithSpacing = _scrollRect.horizontal ? _itemSize.x + _itemSpace.x : _itemSize.y + _itemSpace.y;
			
			_viewportSize = _scrollRect.horizontal ? viewportRect.width : viewportRect.height;

			_listItemsCount = Mathf.CeilToInt(_viewportSize / itemSizeWithSpacing) + 1;

			_itemsSize = itemSizeWithSpacing * _listItemsCount;

			SetItemsPerEntryBasedOnGridLayout(gridLayout);

			_listItemsCount *= _itemsPerEntry;
		}

		private void SetItemsPerEntryBasedOnGridLayout(GridLayoutGroup gridLayout)
		{
			if (gridLayout != null)
			{
				if (gridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
				{
					var secondaryItemSizeWithSpacing = _scrollRect.horizontal
						? (gridLayout.cellSize.y + gridLayout.spacing.y)
						: (gridLayout.cellSize.x + gridLayout.spacing.x);
					var viewportOtherSize = 0f;

					if (_scrollRect.horizontal)
					{
						viewportOtherSize = _scrollRect.viewport.rect.height;
						gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
					}
					else
					{
						viewportOtherSize = _scrollRect.viewport.rect.width;
						gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
					}

					_itemsPerEntry = gridLayout.constraintCount =
						Mathf.FloorToInt(viewportOtherSize / secondaryItemSizeWithSpacing);
				}
				else
				{
					_itemsPerEntry = gridLayout.constraintCount;
				}
			}
			else
				_itemsPerEntry = 1;
		}
		
		private void OnScroll(Vector2 v) => UpdateItems(false);

		/// <summary>
		/// We use the clamped movements to know which is the correct position of each element of the scroll.
		/// This method is hard to simplify but eventually, we can create a different update method depending
		/// on the type of scroll we have (horizontal or vertical).
		/// </summary>
		/// <param name="isForced"></param>
		private void UpdateItems(bool isForced)
		{
			for (var i = 0; i < _listItemsCount; ++i)
			{
				var item = Items[i];

				if (!item.gameObject.activeSelf)
					return;

				var itemRect = item.transform as RectTransform;
				
				if(itemRect == null)
					continue;

				var prevIndex = _indexArray[i];
				var newIndex = prevIndex;
				var itemPosition = _scrollRect.viewport.InverseTransformPoint(itemRect.position);
				var movesCount = 0;

				if (_scrollRect.horizontal)
					movesCount = Mathf.RoundToInt(Mathf.Abs(Mathf.Abs(itemPosition.x))/_itemsSize)*Math.Sign(itemPosition.x);
				else
					movesCount = -Mathf.RoundToInt(Mathf.Abs(Mathf.Abs(itemPosition.y))/_itemsSize)*Math.Sign(itemPosition.y);

				if (!isForced && movesCount == 0)
					continue;

				movesCount = GetClampedMovesCount(newIndex, movesCount);
				newIndex -= (movesCount * _listItemsCount);

				if (!isForced && prevIndex == newIndex)
					continue;

				itemPosition = itemRect.localPosition;
				
				if (_scrollRect.horizontal)
					itemPosition.x -= (movesCount * _itemsSize);
				else
					itemPosition.y += (movesCount * _itemsSize);
				
				itemRect.localPosition = itemPosition;

				_indexArray[i] = newIndex;

				_onUpdate?.Invoke(item, newIndex);
			}
		}

		private int GetClampedMovesCount(int index, int moves)
		{
			index -= (moves*_listItemsCount);

			if (index < 0)
				return (moves - Mathf.CeilToInt(Mathf.Abs((float)index)/_listItemsCount));

			if (index >= _itemsCount)
				return (moves + Mathf.CeilToInt((float)(index - _itemsCount + 1)/_listItemsCount));

			return moves;
		}
	}
}
