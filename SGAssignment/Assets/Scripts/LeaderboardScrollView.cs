using System.Collections.Generic;
using UnityEngine;

public class LeaderboardScrollView : MonoBehaviour
{
    [SerializeField]
    private LeaderboardFetcher _leaderboardFetcher;

    [SerializeField]
    private float _elementHeight;
    [SerializeField]
    private float _elementPadding;
    [SerializeField]
    private int _amountOfVisibleElements;

    [SerializeField]
    private LeaderboardScrollElement _element;
    [SerializeField]
    private RectTransform _holder;

    private int _lastKnownMaxRange = -1;
    private int _lastTopIndex = -1;
    private List<LeaderboardScrollElement> _pool = new();

    private void Awake()
    {
        // Based on the amount of visible objects, we create a pool
        for (var i = 0; i < _amountOfVisibleElements; i++)
        {
            var element = Instantiate(_element, _holder);
            _pool.Add(element);

            element.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Get the current position Y
        var currentY = _holder.localPosition.y;

        // Calculate what the top visible index is based on this y position
        var topIndex = Mathf.Max(1, Mathf.FloorToInt((currentY + _elementHeight) / (_elementPadding + _elementHeight)));

        // If this is the same as we've seen before, no need to do change anything
        if (topIndex == _lastTopIndex
            && _lastKnownMaxRange == _leaderboardFetcher.MaxKnownRank)
        {
            return;
        }

        // Remember top index and max known rank
        _lastTopIndex = topIndex;
        _lastKnownMaxRange = _leaderboardFetcher.MaxKnownRank;

        // Update holder size for scrolling based on max possible rank
        _holder.sizeDelta = new Vector2(_holder.sizeDelta.x, GetOffset(_leaderboardFetcher.MaxKnownRank + 1));

        // Calculate most bottom index 
        var botIndex = Mathf.Min(_leaderboardFetcher.MaxKnownRank, topIndex + _amountOfVisibleElements - 1);

        // Iterate over all elements
        var poolId = 0;
        for (var i = topIndex; i <= botIndex; i++)
        {
            // Get entry
            var entry = _leaderboardFetcher.GetEntry(i);
            if (entry == null)
                break;

            // Load image if we haven't already
            entry.LoadImage();

            // Calculate offset
            var offset = GetOffset(i);

            // Setup next entry from the pool
            var element = _pool[poolId++];
            element.gameObject.SetActive(true);
            element.transform.localPosition = Vector3.down * offset;
            element.Inject(entry);
        }
    }

    private float GetOffset(int index)
    {
        return index * _elementPadding
            + (index - 1) * _elementHeight;
    }
}
