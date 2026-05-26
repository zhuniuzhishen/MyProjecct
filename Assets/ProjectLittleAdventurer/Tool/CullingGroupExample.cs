using UnityEngine;

namespace UnityLibrary
{
    /// <summary>
    /// Unity CullingGroup API 示例：生成大量物体并用距离带着色，演示 onStateChanged。
    /// 与正式关卡逻辑无关，可作学习或删除。
    /// </summary>
    public class CullingGroupExample : MonoBehaviour
    {
        [Tooltip("用于批量生成的预制体，需带 Renderer 与 MeshFilter（示例用球体）")]
        public GameObject prefab;

        [Tooltip("距离参考点（本物体）在此距离内为距离带 0")]
        public float searchDistance = 3;

        public bool colorInvisibleObjects = false;

        int objectCount = 5000;

        Renderer[] objects;
        CullingGroup cullGroup;
        BoundingSphere[] bounds;

        void Start()
        {
            // 创建剔除组并绑定主摄像机
            cullGroup = new CullingGroup();
            cullGroup.targetCamera = Camera.main;

            // measure distance to our transform
            cullGroup.SetDistanceReferencePoint(transform);

            // search distance "bands" starts from 0, so index=0 is from 0 to searchDistance
            cullGroup.SetBoundingDistances(new float[] { searchDistance, float.PositiveInfinity });

            bounds = new BoundingSphere[objectCount];

            // 随机生成大量物体并记录 Renderer 与包围球
            objects = new Renderer[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                var pos = Random.insideUnitCircle * 30;
                var go = Instantiate(prefab, pos, Quaternion.identity);
                objects[i] = go.GetComponent<Renderer>();

                // collect bounds for objects
                var b = new BoundingSphere();
                b.position = go.transform.position;

                // get simple radius..works for our sphere
                b.radius = go.GetComponent<MeshFilter>().mesh.bounds.extents.x;
                bounds[i] = b;
            }

            // set bounds that we track
            cullGroup.SetBoundingSpheres(bounds);
            cullGroup.SetBoundingSphereCount(objects.Length);

            cullGroup.onStateChanged += StateChanged;
        }

        /// <summary>单个物体可见性或距离带变化时的回调。</summary>
        void StateChanged(CullingGroupEvent e)
        {
            Debug.Log("Called");
            if (colorInvisibleObjects == true && e.isVisible == false)
            {
                objects[e.index].material.color = Color.gray;
                return;
            }

            // if we are in distance band index 0, that is between 0 to searchDistance
            if (e.currentDistance == 0)
            {
                objects[e.index].material.color = Color.green;
            }
            else // too far, set color to red
            {
                objects[e.index].material.color = Color.red;
            }
        }

        /// <summary>场景卸载时取消订阅并释放 CullingGroup 原生资源。</summary>
        private void OnDestroy()
        {
            cullGroup.onStateChanged -= StateChanged;
            cullGroup.Dispose();
            cullGroup = null;
        }

    }
}