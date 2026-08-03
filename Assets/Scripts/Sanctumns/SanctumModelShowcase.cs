using System.Collections.Generic;
using UnityEngine;

public class SanctumModelShowcase : MonoBehaviour
{
    [Header("Showcase Settings")]
    [Tooltip("Drag existing GameObjects from your Hierarchy here.")]
    [SerializeField] private List<GameObject> modelsToShowcase = new List<GameObject>();
    
    [Tooltip("The radius of the circular arch. Higher numbers make a flatter curve.")]
    [SerializeField] private float radius = 10.0f;

    [Tooltip("The total angle spread of the arch in degrees.")]
    [Range(10f, 180f)]
    [SerializeField] private float arcAngle = 60.0f;

    void Start()
    {
        ArrangeModelsInArch();
    }

    private void ArrangeModelsInArch()
    {
        modelsToShowcase.RemoveAll(item => item == null);

        int count = modelsToShowcase.Count;
        if (count == 0) return;

        if (count == 1)
        {
            modelsToShowcase[0].transform.SetParent(transform);
            modelsToShowcase[0].transform.localPosition = new Vector3(0, 0, radius);
            modelsToShowcase[0].transform.localRotation = Quaternion.identity;
            return;
        }

        float angleStep = arcAngle / (count - 1);
        float startAngle = 90f - (arcAngle / 2f);

        for (int i = 0; i < count; i++)
        {
            GameObject model = modelsToShowcase[i];
            model.transform.SetParent(transform);

            float currentAngle = startAngle + (i * angleStep);
            float radians = currentAngle * Mathf.Deg2Rad;

            float posX = Mathf.Cos(radians) * radius;
            float posZ = Mathf.Sin(radians) * radius;

            float centerOffsetZ = radius; 
            Vector3 localTargetPos = new Vector3(posX, 0f, posZ - centerOffsetZ);

            model.transform.localPosition = localTargetPos;
            model.transform.localRotation = Quaternion.LookRotation(-localTargetPos.normalized, Vector3.up);
        }
    }
}