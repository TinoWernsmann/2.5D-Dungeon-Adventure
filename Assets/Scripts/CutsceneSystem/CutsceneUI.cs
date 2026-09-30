using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneUI : MonoBehaviour
{
    [SerializeField] private Image _dialogueImage;
    [SerializeField] private RawImage _videoImage;
    [SerializeField] private VideoPlayer _vplayer;

    private RenderTexture _renderTexture;

    public VideoPlayer SetVideo(VideoClip video)
    {
        if (video == null) return null;

        if (_renderTexture != null) _renderTexture.Release();
        _renderTexture = new RenderTexture((int)video.width, (int)video.height, 0);

        _vplayer.source = VideoSource.VideoClip;
        _vplayer.renderMode = VideoRenderMode.RenderTexture;
        _vplayer.targetTexture = _renderTexture;
        _vplayer.playOnAwake = false;
        _vplayer.clip = video;

        _videoImage.texture = _renderTexture;
        _videoImage.gameObject.SetActive(true);
        _dialogueImage.gameObject.SetActive(false);

        return _vplayer;
    }

    public void HideVideo()
    {
        _videoImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
    }

    public void SetDialogueImage(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("No Image provided!");
            return;
        }
        _dialogueImage.sprite = sprite;
    }
}
