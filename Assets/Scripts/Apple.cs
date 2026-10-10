using System;
using System.Collections;
using UnityEngine;

namespace FruitTree
{
    [RequireComponent(typeof(Collider))]
    public sealed class Apple : MonoBehaviour
    {
        public event Action<Apple> Harvested;
        public bool IsHarvested { get; private set; }

        public void Pick()
        {
            if (IsHarvested) return;
            IsHarvested = true;
            GetComponent<Collider>().enabled = false;
            Harvested?.Invoke(this);
            StartCoroutine(PickAnimation());
        }

        private IEnumerator PickAnimation()
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;
            float duration = .18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Clamp01(t / duration);
                transform.localScale = startScale * (1f - .88f * alpha);
                transform.localPosition = startPosition + Vector3.up * (.14f * alpha);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
