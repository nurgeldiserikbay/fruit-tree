using UnityEngine;

namespace FruitTree
{
    public sealed class ParentPurchasePanel : MonoBehaviour
    {
        public GameObject panel;

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
