using System;
using UnityEngine;

namespace FruitTree
{
    public sealed class Apple : MonoBehaviour
    {
        public event Action<Apple> Harvested;
        public bool IsHarvested { get; private set; }
        public void Pick()
        {
            if (IsHarvested) return;
            IsHarvested = true;
            Harvested?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
