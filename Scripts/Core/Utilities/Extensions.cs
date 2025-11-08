using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Extension methods for common Unity types and operations
    /// </summary>
    public static class Extensions
    {
        // Vector3 Extensions
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        public static Vector3 Flat(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        public static Vector2 ToVector2XZ(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        public static float DistanceXZ(this Vector3 from, Vector3 to)
        {
            return Vector3.Distance(from.Flat(), to.Flat());
        }

        // Transform Extensions
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static void DestroyChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        // GameObject Extensions
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        // LayerMask Extensions
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        // Collider Extensions
        public static bool IsInLayerMask(this Collider collider, LayerMask layerMask)
        {
            return ((1 << collider.gameObject.layer) & layerMask.value) != 0;
        }

        // List Extensions
        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
                return default(T);

            return list[Random.Range(0, list.Count)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // Float Extensions
        public static bool Approximately(this float value, float target, float threshold = 0.01f)
        {
            return Mathf.Abs(value - target) < threshold;
        }

        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        // Color Extensions
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // Quaternion Extensions
        public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime)
        {
            Vector3 c = current.eulerAngles;
            Vector3 t = target.eulerAngles;
            return Quaternion.Euler(
                Mathf.SmoothDampAngle(c.x, t.x, ref velocity.eulerAngles.x, smoothTime),
                Mathf.SmoothDampAngle(c.y, t.y, ref velocity.eulerAngles.y, smoothTime),
                Mathf.SmoothDampAngle(c.z, t.z, ref velocity.eulerAngles.z, smoothTime)
            );
        }

        // RectTransform Extensions
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        // String Extensions
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        // Camera Extensions
        public static bool IsInView(this Camera camera, Vector3 worldPosition)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                   viewportPoint.z > 0;
        }

        // Rigidbody Extensions
        public static void SetVelocityX(this Rigidbody rb, float x)
        {
            rb.velocity = rb.velocity.With(x: x);
        }

        public static void SetVelocityY(this Rigidbody rb, float y)
        {
            rb.velocity = rb.velocity.With(y: y);
        }

        public static void SetVelocityZ(this Rigidbody rb, float z)
        {
            rb.velocity = rb.velocity.With(z: z);
        }

        // Renderer Extensions
        public static void SetAlpha(this Renderer renderer, float alpha)
        {
            foreach (Material material in renderer.materials)
            {
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        // Canvas Group Extensions
        public static void SetVisibility(this CanvasGroup canvasGroup, bool visible, bool interactable = true)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible && interactable;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
