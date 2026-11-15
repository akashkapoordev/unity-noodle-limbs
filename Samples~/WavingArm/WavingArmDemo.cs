using NoodleLimbs;
using UnityEngine;

/// <summary>
/// Minimal NoodleLimb demo: drop on an empty GameObject in a URP 2D scene and press Play. Builds a shoulder,
/// a hand and one limb in code, then swings the hand in a loop while the limb bows, so you can see the tube
/// bend and stretch like a rubber hose. Scale this GameObject to make the arm bigger.
/// </summary>
public class WavingArmDemo : MonoBehaviour
{
    [SerializeField] private float _reach = 1.2f;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _maxBend = 0.25f;
    [SerializeField] private Color _sleeve = new(0.29f, 0.2f, 0.15f);

    private Transform _hand;
    private NoodleLimb _limb;

    private void Awake()
    {
        var shoulder = new GameObject("Shoulder").transform;
        shoulder.SetParent(transform, false);
        _hand = new GameObject("Hand").transform;
        _hand.SetParent(transform, false);

        // The SpriteRenderer is only a template for the material and sorting layer; NoodleLimb disables it.
        // Shader.Find only works in builds if the shader is included (e.g. referenced by a material asset).
        var arm = new GameObject("Arm");
        arm.transform.SetParent(transform, false);
        arm.AddComponent<SpriteRenderer>().sharedMaterial = new Material(Shader.Find("NoodleLimbs/NoodleLimb"));
        _limb = arm.AddComponent<NoodleLimb>();
        _limb.SetJoints(shoulder, _hand);
        _limb.Width = 0.18f;
        _limb.Outline = 0.05f;
        _limb.Fill = _sleeve;
    }

    private void Update()
    {
        float t = Time.time * _speed;
        _hand.localPosition = new Vector3(_reach * (0.6f + 0.4f * Mathf.Cos(t)), _reach * 0.5f * Mathf.Sin(t), 0f);
        _limb.Bend = Mathf.Sin(t * 1.3f) * _maxBend;
    }
}
