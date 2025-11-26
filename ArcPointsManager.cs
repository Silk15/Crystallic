using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic
{
    /// <summary>
    /// Written by Phantom, gifted to me, to be cherished and misused for evil deeds such as violent shard stabbings on the streets of Byeth
    /// </summary>
    public class ArcPointsManager : MonoBehaviour
    {
        [Header("Arc Settings")]
        public float radius = 3f;
        [Range(0.0f, 360.0f)]
        public float totalAngle = 140f;
        public float startAngle = 0f;
        public int numberOfPoints = 0;
        public float defaultRadius = 0f;

        [Header("Spin")]
        public bool spin = false;
        public float currentAngle;
        public float rootSpinSpeed;

        
        [Header("Orientation")]
        public Vector3 forwardReference = Vector3.forward;
        public Quaternion originalLocalRotation;

        [Header("Visualization")]
        public GameObject pointPrefab;
        public bool autoUpdate = true;

        [Header("Animation")]
        public float lerpDuration = 0.1f;
        public bool delayEvents = false;
        public bool addAtRandom = false;
        public bool removeAtRandom = false;

        [Header("Spin Settings")]
        public float spinSpeed = 90f;

        [Header("Drift Settings")]
        public float driftAmount = 0.2f;
        public float driftSpeed = 0.5f;

        public class PointData
        {
            public Transform transform;
            // stored in manager-local space (so transform.TransformPoint(local) -> world)
            public Vector3 startLocal;
            public Vector3 targetLocal;
            public Vector3 currentDriftLocal;
            public float spinAngle;
            public float lerpTimer;
            public bool allowMove = true;
            public bool wasAllowMove = true;
        }
        
        public List<PointData> points = new List<PointData>();

        public event OnPointAction onPointCreatedEvent;
        public event OnPointAction onPointRemovedEvent;
        public event OnLerp oOnLerpStartEvent;
        public event OnLerp onLerpEndEvent;
        
        public delegate void OnPointAction(ArcPointsManager pointsManager, PointData point);
        public delegate void OnLerp();

        public List<Transform> GetPoints()
        {
            List<Transform> transforms = new List<Transform>();
            foreach (PointData p in points) transforms.Add(p.transform);
            return transforms;
        }

        public PointData GetRandomPoint()
        {
            if (points.Count == 0) return null;
            return points[Random.Range(0, points.Count)];
        }

        public void ClearPoints()
        {
            numberOfPoints = 0;
        }

        public void AddPoint() => numberOfPoints++;
        public void RemovePoint() => numberOfPoints = Mathf.Max(0, numberOfPoints - 1);

        /// <summary>
        /// Removes a specified point from the arc points list, triggers the
        /// OnPointRemovedEvent if subscribed, and destroys the corresponding game object.
        /// Ensures the point is properly disposed of and no longer part of the arc system.
        /// </summary>
        /// <param name="point">The transform of the point to be removed from the arc layout.</param>
        public void RemovePoint(PointData point)
        {
            int removeIndex = points.IndexOf(point);
            if (removeIndex < 0) return;
            PointData p = points[removeIndex];
            points.RemoveAt(removeIndex);
            RemovePoint();
            onPointRemovedEvent?.Invoke(this, p);
            Destroy(p.transform.gameObject);
        }

        /// <summary>
        /// Called once per frame to handle key updates and transformations for arc points.
        /// Manages smooth positional updates, spinning behavior, and drift application
        /// for all points within the arc. Ensures that points retain proper orientation
        /// after all transformations are applied. Invokes update functions synchronously
        /// to maintain consistency during animations and interactions when autoUpdate is enabled.
        /// </summary>
        private void Update()
        {
            if (autoUpdate) UpdatePointsSmooth();
            SpinPoints();
            ApplyDrift();
            ApplyHorizontalSpin();

            foreach (var p in points)
            {
                if (!p.allowMove) continue;
                ApplyOrientation(p);
            }
        }

        private void ApplyHorizontalSpin()
        {
            if (spin)
            {
                currentAngle += rootSpinSpeed * Time.deltaTime;
                transform.localRotation = originalLocalRotation * Quaternion.Euler(0f, currentAngle, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, originalLocalRotation, Time.deltaTime * 5f);
                currentAngle = 0f;
            }
        }

        /// <summary>
        /// Smoothly updates the position of each point along a defined arc using interpolation.
        /// Ensures consistent and controlled movement by lerping each point's position
        /// in local space over a specified duration. Updates world position accordingly
        /// while accounting for drift effects.
        /// </summary>
        private void UpdatePointsSmooth()
        {
            SyncPointCount();

            for (int i = 0; i < points.Count; i++)
            {
                PointData p = points[i];
                Vector3 desiredLocal = GetLocalPointOnArc(i, radius, totalAngle);

                if (!p.allowMove)
                {
                    p.wasAllowMove = false;
                    continue;
                }

                if (!p.wasAllowMove)
                {
                    p.startLocal = transform.InverseTransformPoint(p.transform.position) - p.currentDriftLocal;
                    p.targetLocal = desiredLocal;
                    p.lerpTimer = 0f;
                    oOnLerpStartEvent?.Invoke();
                }

                p.wasAllowMove = true;

                if ((p.targetLocal - desiredLocal).sqrMagnitude > 1e-6f)
                {
                    p.startLocal = transform.InverseTransformPoint(p.transform.position) - p.currentDriftLocal;
                    p.targetLocal = desiredLocal;
                    p.lerpTimer = 0f;
                    oOnLerpStartEvent?.Invoke();
                }

                Vector3 currentLocal;
                if (lerpDuration <= 0f)
                {
                    currentLocal = p.targetLocal;
                    p.lerpTimer = lerpDuration;
                }
                else if (p.lerpTimer < lerpDuration)
                {
                    p.lerpTimer += Time.deltaTime * (p.wasAllowMove ? 1f : 3f);
                    float t = Mathf.Clamp01(p.lerpTimer / lerpDuration);
                    currentLocal = Vector3.Lerp(p.startLocal, p.targetLocal, t);
                }
                else
                {
                    currentLocal = p.targetLocal;
                    onLerpEndEvent?.Invoke();
                }

                p.transform.position = transform.TransformPoint(currentLocal + p.currentDriftLocal);
            }
        }


        /// <summary>
        /// Rotates each point in the arc around its local forward axis.
        /// The rotation speed is determined by the spinSpeed value and is applied based on the time delta.
        /// Maintains smooth and consistent spinning behavior over time.
        /// </summary>
        private void SpinPoints()
        {
            
            foreach (PointData p in points)
            {
                if (!p.allowMove) continue;
                p.spinAngle += spinSpeed * Time.deltaTime;
            }
        }

        /// <summary>
        /// Updates the drift offset for each point in the arc based on Perlin noise.
        /// Smoothly interpolates the current drift offset towards a target drift value over time.
        /// Drift patterns are influenced by the drift speed and drift amount settings.
        /// </summary>
        private void ApplyDrift()
        {
            foreach (PointData p in points)
            {
                if (!p.allowMove) continue;
                float time = Time.time * driftSpeed;
                Vector3 targetDriftLocal = new Vector3(
                    (Mathf.PerlinNoise(p.transform.GetInstanceID(), time) - 0.5f) * 2f * driftAmount,
                    (Mathf.PerlinNoise(p.transform.GetInstanceID() + 1000, time) - 0.5f) * 2f * driftAmount,
                    (Mathf.PerlinNoise(p.transform.GetInstanceID() + 2000, time) - 0.5f) * 2f * driftAmount
                );

                p.currentDriftLocal = Vector3.Lerp(p.currentDriftLocal, targetDriftLocal, Time.deltaTime * driftSpeed);
            }
        }

        /// <summary>
        /// Ensures that the current number of points in the arc matches the desired number of points.
        /// Adds or removes points as necessary while invoking relevant events such as point creation or removal.
        /// </summary>
        private void SyncPointCount()
        {
            while (points.Count < numberOfPoints)
            {
                int newIndex = points.Count;
                GameObject obj = pointPrefab ? Instantiate(pointPrefab) : new GameObject($"ArcPoint_{newIndex}");
                // keep unparented (world object), initially place at manager origin so it appears at the manager
                obj.transform.SetPositionAndRotation(transform.position, transform.rotation);

                Vector3 localTarget = GetLocalPointOnArc(newIndex, radius, totalAngle);

                PointData p = new PointData
                {
                    transform = obj.transform,
                    // startLocal based on current world position (converted to manager local), minus drift
                    startLocal = transform.InverseTransformPoint(obj.transform.position),
                    targetLocal = localTarget,
                    spinAngle = Random.Range(0f, 360f),
                    currentDriftLocal = Vector3.zero,
                    lerpTimer = 0f
                };

                if (addAtRandom && points.Count > 0)
                {
                    int insertIndex = Random.Range(0, points.Count + 1);
                    points.Insert(insertIndex, p);
                }
                else
                {
                    points.Add(p);
                }

                if (!delayEvents)
                    onPointCreatedEvent?.Invoke(this, p);
                else
                    StartCoroutine(EventDelay(p.transform));
            }

            while (points.Count > numberOfPoints)
            {
                int removeIndex = removeAtRandom ? Random.Range(0, points.Count) : points.Count - 1;
                PointData p = points[removeIndex];
                points.RemoveAt(removeIndex);
                onPointRemovedEvent?.Invoke(this, p);
                Destroy(p.transform.gameObject);
            }
        }

        /// <summary>
        /// Delays the invocation of the OnPointCreatedEvent for the specified transform by the defined lerp duration.
        /// </summary>
        /// <param name="t">The transform of the point to delay the event for.</param>
        /// <returns>An IEnumerator to control the timing of the event execution.</returns>
        private IEnumerator EventDelay(Transform t)
        {
            yield return new WaitForSeconds(lerpDuration);
            if (t) onPointCreatedEvent?.Invoke(this, points.FirstOrDefault(p => p.transform == t));
        }
        
        /// <summary>
        /// Applies orientation to a point based on the manager's rotation, a defined forward reference, and the point's spin angle.
        /// </summary>
        /// <param name="p">The point data containing transform information and spin angle.</param>
        private void ApplyOrientation(PointData p)
        {
            if (forwardReference == Vector3.zero) return;
            
            Quaternion localBase = Quaternion.LookRotation(forwardReference.normalized, Vector3.up);
            Quaternion localSpin = Quaternion.AngleAxis(p.spinAngle, Vector3.forward);
            p.transform.rotation = transform.rotation * (localBase * localSpin);
        }
        
        /// <summary>
        /// Calculates a local position on an arc based on the given index, radius, and total angle.
        /// </summary>
        /// <param name="index">The index of the point on the arc.</param>
        /// <param name="radius">The radius of the arc.</param>
        /// <param name="totalAngle">The total angle of the arc in degrees.</param>
        /// <returns>A Vector3 representing the local position of the point on the arc relative to the origin.</returns>
        private Vector3 GetLocalPointOnArc(int index, float radius, float totalAngle)
        {
            float defaultAngle = -90f + startAngle;
            float startRad = ((totalAngle / 2f) + defaultAngle) * Mathf.Deg2Rad;
            float endRad = (-(totalAngle / 2f) + defaultAngle) * Mathf.Deg2Rad;

            Vector3 normalLocal = Vector3.forward;
            Vector3 rightLocal = Vector3.Cross(normalLocal, Vector3.up).normalized;
            if (rightLocal.sqrMagnitude < 0.001f)
                rightLocal = Vector3.Cross(normalLocal, Vector3.forward).normalized;
            Vector3 forwardLocal = Vector3.Cross(normalLocal, rightLocal).normalized;

            float angle;
            if (numberOfPoints <= 1)
            {
                // Single point sits at arc center
                angle = (startRad + endRad) / 2f;
            }
            else if (Mathf.Approximately(totalAngle % 360f, 0f))
            {
                // Full circle — evenly space points without overlap
                angle = startRad + index * ((endRad - startRad) / numberOfPoints);
            }
            else
            {
                // Partial arc — include endpoints at the arc extremes
                angle = startRad + index * ((endRad - startRad) / (numberOfPoints - 1));
            }

            Vector3 directionLocal = Mathf.Cos(angle) * rightLocal + Mathf.Sin(angle) * forwardLocal;
            return directionLocal * radius;
        }
    }
}
