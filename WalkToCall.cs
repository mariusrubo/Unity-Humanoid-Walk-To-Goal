using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkToCall : MonoBehaviour {

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

    private bool WalkTo1;
    private bool WalkTo2;
    private bool WalkTo3;

    // Character
    public Transform Character;
    private WalkToGoal CharacterWalkToGoal = null;
    public bool CharacterHasReachedGoal; // important return value if you need to 

    // Use this for initialization
    void Start () {
        CharacterWalkToGoal = Character.GetComponent<WalkToGoal>();
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Walk to Goal 1")) { WalkTo1 = true; }
        if (GUILayout.Button("Walk to Goal 2")) { WalkTo2 = true; }
        if (GUILayout.Button("Walk to Goal 3")) { WalkTo3 = true; }
    }
        
        // Update is called once per frame
        void Update ()
    {
        DetermineCurrentGoal();
        CharacterHasReachedGoal = CharacterWalkToGoal.WalkTo(CurrentGoal, drawPath, MaxSpeed, 0.3f, 120); //public void WalkTo(Transform CurrentGoal, bool DrawPath, float maxspeed, float mindist, float anglecut)
    }

    /// <summary>
    /// Determine current goal. 
    /// This function is designed to simplify switching of goals. You simply have a boolean for each goal and can switch each 
    /// to "true" individually without causing confusion with the other goals' booleans. 
    /// </summary>
    private void DetermineCurrentGoal()
    {
        if (WalkTo1 == true) // if click on this bool...
        {
            if (CurrentGoal != goal1 && goal1 != null) // only if CurrentGoal was not goal1 in the last frame...
            {
                CurrentGoal = goal1; // and insert correct CurrentGoal and Collider
            }
            WalkTo1 = false; // always set variable back to false so you can click again
        }

        if (WalkTo2 == true)
        {
            if (CurrentGoal != goal2 && goal2 != null)
            {
                CurrentGoal = goal2;
            }
            WalkTo2 = false;
        }

        if (WalkTo3 == true)
        {
            if (CurrentGoal != goal3 && goal3 != null)
            {
                CurrentGoal = goal3;
            }
            WalkTo3 = false;
        }
    }
}
