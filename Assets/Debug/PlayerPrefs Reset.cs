using UnityEngine;

public class PlayerPrefsReset : MonoBehaviour
{
    public bool resetSavedDataOnGameLaunch = false;

    public void ResetSavedData()
    {
        if (!resetSavedDataOnGameLaunch)
        {
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("Player Data has been deleted.");
    }
}
