using UnityEngine;

// HingeJoint2D ile birbirine bagli tahtalardan asma kopru.
// Tahtalar kanyon duvarlarina takilmasin diye arazi carpismasindan muaf tutulur.
public class Kopru : MonoBehaviour
{
    public Collider2D arazi;

    void Start()
    {
        if (arazi == null) return;
        foreach (var c in GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(c, arazi);
    }
}
