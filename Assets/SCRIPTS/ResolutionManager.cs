using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndApply();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetResolution(int width, int height)
    {
        if (Screen.width == width && Screen.height == height)
            return;

        // Если мы в полноэкранном режиме, растягиваем картинку на весь экран (без полей)
        // Используем FullScreenMode.FullScreenWindow (безрамочное окно на весь экран)
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);

        PlayerPrefs.SetInt("Res_Width", width);
        PlayerPrefs.SetInt("Res_Height", height);
        PlayerPrefs.Save();
    }

    private void LoadAndApply()
    {
        int width = PlayerPrefs.GetInt("Res_Width", 1920);
        int height = PlayerPrefs.GetInt("Res_Height", 1080);

        if (Screen.width != width || Screen.height != height)
        {
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        }
    }
}