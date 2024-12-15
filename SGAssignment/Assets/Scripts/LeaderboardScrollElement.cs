using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardScrollElement : MonoBehaviour
{
    [SerializeField]
    private float _spinSpeed = 360f;
    [SerializeField]
    private int _topRankThreshold = 3;

    [SerializeField]
    private Color[] _placeColors;

    [SerializeField]
    private GameObject _loaderSpinner;
    [SerializeField]
    private Image _background;
    [SerializeField]
    private SVGImage _image;
    [SerializeField]
    private TMP_Text _rankText;
    [SerializeField]
    private TMP_Text _topRankText;
    [SerializeField]
    private TMP_Text _nameText;
    [SerializeField]
    private TMP_Text _scoreText;

    private LeaderboardEntry _entry;

    public void Inject(LeaderboardEntry entry)
    {
        if (_entry != null)
            OnReset();

        _entry = entry;

        // Subscribe in case of sprite loading
        _entry.OnSpriteLoaded += OnSpriteLoaded;

        // Set color based on index
        var colorIndex = Mathf.Clamp(entry.Rank - 1, 0, _placeColors.Length - 1);
        _background.color = _placeColors[colorIndex];

        // Set texts
        var isTopRank = entry.Rank <= _topRankThreshold;

        if (isTopRank)
            _topRankText.text = entry.Rank.ToString();
        else
            _rankText.text = entry.Rank.ToString();

        _topRankText.gameObject.SetActive(isTopRank);
        _rankText.gameObject.SetActive(!isTopRank);

        _nameText.text = entry.PlayerName;
        _scoreText.text = entry.Score.ToString();

        // If we already have a sprite, call the callback
        if (_entry.Sprite != null)
            OnSpriteLoaded();
    }

    private void Update()
    {
        // If the loader spinner is active, rotate it
        if (_loaderSpinner.activeSelf)
            _loaderSpinner.transform.localEulerAngles += Vector3.forward * _spinSpeed * Time.deltaTime;
    }

    private void OnDestroy()
    {
        OnReset();
    }

    private void OnReset()
    {
        _image.sprite = null;
        _image.color = Color.clear;
        _loaderSpinner.SetActive(true);

        if (_entry != null)
            _entry.OnSpriteLoaded -= OnSpriteLoaded;

        _entry = null;
    }

    private void OnSpriteLoaded()
    {
        _loaderSpinner.SetActive(false);
        _image.sprite = _entry.Sprite;
        _image.color = Color.white;
    }
}
