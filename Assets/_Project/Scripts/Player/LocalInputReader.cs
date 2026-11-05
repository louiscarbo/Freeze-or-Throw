using UnityEngine;

// Reads player input every frame and packages it in a PlayerInputData struct
public class LocalInputReader : MonoBehaviour
{
    public PlayerInputData CurrentInput {get; private set;}
}
