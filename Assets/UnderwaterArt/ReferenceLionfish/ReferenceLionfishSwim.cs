using UnityEngine;

// Observes travel owned by navigation; never translates or rotates the actor.
public sealed class ReferenceLionfishSwim : MonoBehaviour
{
    public Transform motionRoot;
    [Min(.01f)] public float cruiseSpeed = .2f;
    Animation player;
    Vector3 previous;
    float speed;

    void OnEnable()
    {
        player = GetComponentInChildren<Animation>();
        if (!player || !player["Swim"] || !player["Idle"] || !player["Breath"]) { enabled = false; return; }
        foreach (string clip in new[] { "Idle", "Swim", "Breath" }) {
            var state = player[clip]; state.wrapMode = WrapMode.Loop; state.enabled = true;
        }
        var breath = player["Breath"]; breath.layer = 1; breath.weight = 1;
        foreach (Transform bone in player.GetComponentsInChildren<Transform>())
            if (bone.name == "Jaw" || bone.name == "GillL" || bone.name == "GillR") breath.AddMixingTransform(bone, false);
        previous = motionRoot ? motionRoot.position : transform.position;
        player["Swim"].weight = motionRoot ? 0 : 1;
        player["Idle"].weight = motionRoot ? 1 : 0;
    }

    void Update()
    {
        if (!motionRoot || Time.deltaTime <= 0) return;
        Vector3 position = motionRoot.position;
        float measured = Vector3.Distance(position, previous) / Time.deltaTime; previous = position;
        // Ignore teleports and smooth measured travel to prevent fin-rate jitter.
        if (measured > cruiseSpeed * 15) measured = 0;
        speed = Mathf.Lerp(speed, measured, 1 - Mathf.Exp(-Time.deltaTime * 7));
        float blend = Mathf.Clamp01(speed / (cruiseSpeed * .45f));
        player["Swim"].weight = blend; player["Idle"].weight = 1 - blend;
        player["Swim"].speed = Mathf.Clamp(speed / cruiseSpeed, .35f, 1.8f);
        player["Breath"].speed = 1;
    }
}
