using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// A soft rustle when the camera moves through or right against the leaves.
    /// Silent when still or away from the crown; follows the Effects volume.
    /// </summary>
    public sealed class FoliageRustle : MonoBehaviour
    {
        private const float CheckInterval = .12f, ProbeRadius = .75f;
        private const float MaxVolume = .13f, SpeedForFullRustle = 2.4f;
        private readonly Collider[] hits = new Collider[8];
        private AudioSource source;
        private AudioClip clip;
        private OrchardAudio orchardAudio;
        private Transform eye;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private float nearLeaves, nextCheck, volume;

        // Only the real game scene, not bare test rigs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var scenery = FindFirstObjectByType<EnvironmentController>();
            if (scenery == null || scenery.skyRenderer == null) return;
            new GameObject("Foliage Rustle").AddComponent<FoliageRustle>();
        }

        private void Start()
        {
            orchardAudio = FindFirstObjectByType<OrchardAudio>();
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        /// <summary>How loud the rustle should be, 0..1, from camera motion and leaf contact.</summary>
        public static float Intensity(float metresPerSecond, float degreesPerSecond, float contact)
        {
            float motion = metresPerSecond + degreesPerSecond * .02f;
            return Mathf.Clamp01(motion / SpeedForFullRustle) * Mathf.Clamp01(contact);
        }

        private void Update()
        {
            if (eye == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                eye = main.transform;
                lastPosition = eye.position;
                lastRotation = eye.rotation;
            }
            float delta = Mathf.Max(Time.unscaledDeltaTime, .0001f);
            float speed = Vector3.Distance(eye.position, lastPosition) / delta;
            float turn = Quaternion.Angle(eye.rotation, lastRotation) / delta;
            lastPosition = eye.position;
            lastRotation = eye.rotation;

            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + CheckInterval;
                nearLeaves = LeafContact(eye.position);
            }
            float target = Intensity(speed, turn, nearLeaves) * MaxVolume *
                AudioLevels.Gain(AudioChannel.Effects);
            bool muted = orchardAudio != null && orchardAudio.IsMuted;
            if (muted || Time.timeScale <= 0f) target = 0f;
            // Quick to start, slow to settle, like leaves brushing past.
            float rate = target > volume ? 10f : 2.5f;
            volume = Mathf.MoveTowards(volume, target, rate * MaxVolume * delta);
            source.volume = volume;
            if (volume > .001f && !source.isPlaying)
            {
                if (clip == null) clip = CreateRustle();
                source.clip = clip;
                source.time = Random.Range(0f, clip.length * .9f);
                source.Play();
            }
            else if (volume <= .001f && source.isPlaying) source.Stop();
        }

        /// <summary>1 inside the crown's leaves, 0 away from them.</summary>
        private float LeafContact(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, ProbeRadius, hits,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (hits[i] != null && hits[i].name == "Merged Leaves") return 1f;
            return 0f;
        }

        /// <summary>A loop of soft, uneven leaf noise with no repeating pattern to notice.</summary>
        public static AudioClip CreateRustle()
        {
            const int rate = 22050, count = rate * 4, overlap = rate / 3;
            var random = new System.Random(9157);
            var data = new float[count + overlap];
            float low = 0f, band = 0f, swell = 0f, target = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                if (i % 900 == 0) target = (float)random.NextDouble();
                swell += .004f * (target - swell);
                float white = (float)random.NextDouble() * 2f - 1f;
                low += .35f * (white - low);
                band += .6f * (low - band);
                // Mid-high band noise with gentle swells; occasional leaf ticks.
                float tick = random.NextDouble() < .002 ? white * .6f : 0f;
                data[i] = ((low - band) * (.35f + .65f * swell) + tick * .2f) * .6f;
            }
            var loop = new float[count];
            for (int i = 0; i < count; i++)
            {
                float sample = data[i];
                if (i < overlap)
                {
                    float t = i / (float)overlap;
                    sample = data[i] * Mathf.Sqrt(t) + data[count + i] * Mathf.Sqrt(1f - t);
                }
                loop[i] = Mathf.Clamp(sample, -.8f, .8f);
            }
            var result = AudioClip.Create("Leaf rustle", count, 1, rate, false);
            result.SetData(loop, 0);
            return result;
        }

        private void OnDestroy()
        {
            if (clip != null) Destroy(clip);
        }
    }
}
