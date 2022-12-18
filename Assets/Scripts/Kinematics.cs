using UnityEngine;


public class Kinematics : MonoBehaviour
{
    private SingleDegreeJoint[] joints;
    public float speed;
    public float[] solution;

    public float LearningRate;
    public float SamplingDistance;
    public float DistanceThreshold;

    public GameObject target;
    public GameObject actor;

    // Start is called before the first frame update
    void Start()
    {
        joints = GetComponentsInChildren<SingleDegreeJoint>();
        solution = new float[joints.Length];
        for (var i = 0; i < joints.Length; i++)
        {
            solution[i] = joints[i].GetValue();
        }
        LearningRate = 5;
        SamplingDistance = 2;
        DistanceThreshold = 0;
        speed = 5;
    }

    // Update is called once per frame
    void Update()
    {
        Solve();
        for (var i = 0; i < solution.Length; i++)
        {
            joints[i].SetValue(solution[i]);
        }
    }

    private void Solve()
    {
        var delta = Time.deltaTime * speed;
        for (var i = 0; i < solution.Length; i++)
        {
            // solution[i] = InRange(solution[i] + (i+1)*delta);
            InverseKinematics(target.transform.position, solution);
        }
    }

    public Vector3 ForwardKinematics(float[] angles)
    {
        Vector3 prevPoint = joints[0].transform.position;
        Quaternion rotation = Quaternion.identity;
        for (int i = 1; i < joints.Length; i++)
        {
            // Rotates around a new axis
            rotation *= Quaternion.AngleAxis(angles[i - 1], joints[i - 1].Axis);
            Vector3 nextPoint = prevPoint + rotation * joints[i].StartOffset;
            Debug.DrawLine(prevPoint, nextPoint, Color.green);

            prevPoint = nextPoint;
        }
        rotation *= Quaternion.AngleAxis(angles[joints.Length - 1], joints[joints.Length - 1].Axis);
        Debug.DrawLine(target.transform.position, prevPoint, Color.red);
        return prevPoint + rotation * joints[0].StartOffset * 3;
        //return actor.transform.position;
    }

    public float DistanceFromTarget(Vector3 target, float[] angles)
    {
        Vector3 point = ForwardKinematics(angles);
        Debug.DrawLine(target, point, Color.cyan);
        // Debug.DrawLine(target, actor.transform.position, Color.blue);
        return Vector3.Distance(point, target);
    }

    private float lastDistance;
    private float TimeInterval;
    void LateUpdate()
    {
        // ones per in seconds
        TimeInterval += Time.deltaTime;
        float dist = 0;
        if (TimeInterval >= 5)
        {
            dist = DistanceFromTarget(target.transform.position, solution);
            TimeInterval = 0;
            if (lastDistance - dist < 0.00001 && dist > 0.7)
                for (var i = 0; i < joints.Length; i++)
                    solution[i] = 0;
            lastDistance = dist;
        }
    }
    public float PartialGradient(Vector3 target, float[] angles, int i)
    {
        // Saves the angle,
        // it will be restored later
        float angle = angles[i];
        // Gradient : [F(x+SamplingDistance) - F(x)] / h
        float f_x = DistanceFromTarget(target, angles);

            angles[i] += SamplingDistance;
        float f_x_plus_d = DistanceFromTarget(target, angles);
        float gradient = (f_x_plus_d - f_x) / SamplingDistance;
        // Restores
        angles[i] = angle;
        return gradient;
    }
    public void InverseKinematics(Vector3 target, float[] angles)
    {
        if (DistanceFromTarget(target, angles) < DistanceThreshold)
            return;
        for (int i = joints.Length - 1; i >= 0; i--)
        {
            // Gradient descent
            // Update : Solution -= LearningRate * Gradient
            float gradient = PartialGradient(target, angles, i);
            angles[i] -= LearningRate * gradient;

            // Clamp
            angles[i] = Mathf.Clamp(angles[i], joints[i].MinAngle, joints[i].MaxAngle);
            //angles[i] = InRange(angles[i]);

            // Early termination
            if (DistanceFromTarget(target, angles) < DistanceThreshold)
                return;
        }
    }

    private float InRange(float value)
    {
        while (value < 0)
        {
            value += 360;
        }

        while (value > 360)
        {
            value -= 360;
        }

        return value;
    }
}
