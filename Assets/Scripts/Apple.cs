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
        public string StableFruitId { get; private set; }

        public void Initialize(int index)
        {
            StableFruitId = "fruit_" + index.ToString("D4");
            // AddComponent<Apple>() invokes Awake before the tree generator
            // finishes registering the fruit. Capture its configured position
            // here, after the generator sets local position, scale and rotation.
            CaptureRestPose();
        }

        public void RestorePicked()
        {
            StopAllCoroutines();
            IsHarvested = true;
            GetComponent<Collider>().enabled = false;
            gameObject.SetActive(false);
        }
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Quaternion originalRotation;

        private void Awake()
        {
            CaptureRestPose();
        }

        private void CaptureRestPose()
        {
            originalPosition = transform.localPosition;
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
        }

        public void ResetApple()
        {
            StopAllCoroutines();
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;
            gameObject.SetActive(true);
            GetComponent<Collider>().enabled = true;
            IsHarvested = false;
        }

        /// <summary>
        /// A new harvest round is already saved before this visual transition.
        /// Collider stays disabled until the fruit reaches its full size.
        /// </summary>
        public void BeginRebloom()
        {
            ResetApple();
            GetComponent<Collider>().enabled = false;
            transform.localScale = Vector3.zero;
        }

        public void GrowRebloom(float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            transform.localScale = originalScale * t;
            if (t >= 1f)
                GetComponent<Collider>().enabled = true;
        }

        public void Pick()
        {
            // Harvesting is disallowed while a newly blossomed fruit is growing.
            // Guard the public method as well as the physics raycast pathway.
            if (IsHarvested || !gameObject.activeInHierarchy ||
                !GetComponent<Collider>().enabled) return;
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
