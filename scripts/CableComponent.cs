using UnityEngine;
using System;

public class CableComponent : MonoBehaviour
{
    #region Class members
    [SerializeField] private Transform endPoint;
    [SerializeField] private Material cableMaterial;
    [SerializeField] private float cableLength = 0.5f;
    [SerializeField] private int totalSegments = 5;
    [SerializeField] private float segmentsPerUnit = 2f;
    private int segments = 0;
    [SerializeField] private float cableWidth = 0.1f;
    [SerializeField] private int verletIterations = 1;
    [SerializeField] private int solverIterations = 1;
    [SerializeField] private float stiffness = 1f;
    private LineRenderer line;
    private CableParticle[] points;
    #endregion

    #region Initial setup
    void Start()
    {
        if (endPoint == null)
        {
            Debug.LogError("endPoint не задан! Пожалуйста, установите его в инспекторе.");
            return;  // Останавливаем выполнение скрипта, если endPoint не назначен
        }

        InitCableParticles();
        InitLineRenderer();
    }

    void InitCableParticles()
    {
        if (totalSegments > 0)
            segments = totalSegments;
        else
            segments = Mathf.CeilToInt(cableLength * segmentsPerUnit);

        Vector3 cableDirection = (endPoint.position - transform.position).normalized;
        float initialSegmentLength = cableLength / segments;
        points = new CableParticle[segments + 1];
        for (int pointIdx = 0; pointIdx <= segments; pointIdx++)
        {
            Vector3 initialPosition = transform.position + (cableDirection * (initialSegmentLength * pointIdx));
            points[pointIdx] = new CableParticle(initialPosition);
        }

        CableParticle start = points[0];
        CableParticle end = points[segments];
        start.Bind(this.transform);
        end.Bind(endPoint.transform);
    }

    void InitLineRenderer()
    {
        line = this.gameObject.AddComponent<LineRenderer>();
        line.SetWidth(cableWidth, cableWidth);
        line.SetVertexCount(segments + 1);
        line.material = cableMaterial;
        line.GetComponent<Renderer>().enabled = true;
    }
    #endregion

    #region Render Pass
    void Update()
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogError("Массив точек кабеля не был инициализирован.");
            return;  // Если массив точек пуст или не инициализирован, не продолжаем выполнение
        }

        RenderCable();
    }

    void RenderCable()
    {
        for (int pointIdx = 0; pointIdx < segments + 1; pointIdx++)
        {
            if (points[pointIdx] != null)
            {
                line.SetPosition(pointIdx, points[pointIdx].Position);
            }
            else
            {
                Debug.LogError("Точка кабеля не инициализирована на индексе " + pointIdx);
            }
        }
    }
    #endregion

    #region Verlet integration & solver pass
    void FixedUpdate()
    {
        for (int verletIdx = 0; verletIdx < verletIterations; verletIdx++)
        {
            VerletIntegrate();
            SolveConstraints();
        }
    }

    void VerletIntegrate()
    {
        Vector3 gravityDisplacement = Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;
        foreach (CableParticle particle in points)
        {
            if (particle != null)
            {
                particle.UpdateVerlet(gravityDisplacement);
            }
            else
            {
                Debug.LogError("Отсутствует точка кабеля для интеграции!");
            }
        }
    }

    void SolveConstraints()
    {
        for (int iterationIdx = 0; iterationIdx < solverIterations; iterationIdx++)
        {
            SolveDistanceConstraint(); 
            SolveStiffnessConstraint();
        }
    }

    void SolveDistanceConstraint()
    {
        float segmentLength = cableLength / segments;
        for (int SegIdx = 0; SegIdx < segments; SegIdx++)
        {
            CableParticle particleA = points[SegIdx];
            CableParticle particleB = points[SegIdx + 1];
            SolveDistanceConstraint(particleA, particleB, segmentLength);
        }
    }

    void SolveDistanceConstraint(CableParticle particleA, CableParticle particleB, float segmentLength)
    {
        Vector3 delta = particleB.Position - particleA.Position;
        float currentDistance = delta.magnitude;

        if (currentDistance > segmentLength)
        {
            Vector3 correction = delta.normalized * (currentDistance - segmentLength) * 0.5f;
            particleA.Position += correction;
            particleB.Position -= correction;
        }
    }

    void SolveStiffnessConstraint()
    {
        float distance = (points[0].Position - points[segments].Position).magnitude;
        if (distance > cableLength)
        {
            foreach (CableParticle particle in points)
            {
                SolveStiffnessConstraint(particle, distance);
            }
        }
    }

    void SolveStiffnessConstraint(CableParticle cableParticle, float distance)
    {
        // Здесь может быть пустая реализация, если не нужно применять жесткость.
    }
    #endregion
}
