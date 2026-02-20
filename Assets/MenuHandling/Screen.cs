using UnityEngine;

public class Screens : MonoBehaviour
{
    public MenuHandler menuHandler;

    internal virtual void RemoveListeners()
    {
        
    }

    internal virtual void AddListeners()
    {
        
    }

    internal virtual void Enable()
    {
        AddListeners();
        gameObject.SetActive(true);
    }

    internal virtual void Disable()
    {
        RemoveListeners();
        gameObject.SetActive(false);
    }
}