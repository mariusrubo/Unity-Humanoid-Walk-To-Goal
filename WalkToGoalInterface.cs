using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkToGoalInterface : MonoBehaviour {

    Transform CurrentGoal;
    [Tooltip("Manually drag goal 1 here.")]
    public Transform goal1;
    [Tooltip("Manually drag goal 2 here.")]
    public Transform goal2;
    [Tooltip("Manually drag goal 3 here.")]
    public Transform goal3;

    [Tooltip("Enable to show path in scene view.")]
    public bool drawPath = false;
    [Tooltip("Set maximum speed. 1 is full running, 0.5 fast walking.")]
    public float MaxSpeed = 0.4f;

    // Character
    [Tooltip("Manually drag character here.")]
    public Transform Character;
    private WalkToGoal CharacterWalkToGoal = null;
    public bool CharacterHasReachedGoal; //use this to trigger cascades of behavior (e.g. walk to goal, then grab it etc.)

    // Use this for initialization
    void Start () {
        CharacterWalkToGoal = Character.GetComponent<WalkToGoal>();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(120, 10, 100, 200)); // You can change position of Interface here. This is designed so that all my interface scripts can run together. 
        if (GUILayout.Button("Walk to Goal 1")) { CurrentGoal = goal1; }
        if (GUILayout.Button("Walk to Goal 2")) { CurrentGoal = goal2; }
        if (GUILayout.Button("Walk to Goal 3")) { CurrentGoal = goal3; }
        GUILayout.EndArea();
    }
        
        // Update is called once per frame
        void Update ()
    {
        // calling the actual walking function on the character
        CharacterHasReachedGoal = CharacterWalkToGoal.WalkTo(CurrentGoal, drawPath, MaxSpeed, 0.3f, 120f); 
    }

}
