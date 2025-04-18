using UnityEngine;

public class ParentDebugger : MonoBehaviour
{
    void Start()
    {
        Transform parent = transform.parent;
        Transform root = transform.root;

        if (parent != null)
        {
            Debug.Log($"{gameObject.name} → Родитель: {parent.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name} → Родителя нет");
        }

        if (root != null)
        {
            Debug.Log($"{gameObject.name} → Root (самый верхний): {root.name}");
        }
    }
}
