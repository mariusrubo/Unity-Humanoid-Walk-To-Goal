/***************************************************
 * Written By: Marius Rubo
 * This script lets characters walk to specific goals, while avoiding obstacles on the way.
 * This requires: 
 * 1. a humanoid agent with a controller called 'Locomotion' which can be steered using 'speed' and 'direction' (as in 'Mecanim Locomotion Starter Kit', see Asset Store).
 * 2. At least one object dragged onto 'goal1', 'goal2' and/or 'goal3'
 * 3. Regardless if you have obstacles in the scene or not, do bake the navigation mesh first. Otherwise Unity will not be able to find the path. Click on 'Navigation', 
 * then on the plane that the character stands on in the scene view, then on 'Bake'. For more details see https://docs.unity3d.com/Manual/nav-BuildingNavMesh.html.
****************************************************/

/*
ideas for improvements:
1. When a new goal is chosen, the 'CurrentGoal' is also detected to be moving, causing NavMesh to be called twice. Change this if drops in fps are detected. 
2. When a new goal is chosen while the character is still running, the character first slows down, then targets the new goal. Skipping this step leads to a
   jittering animation (I am not sure about the cause for this, since the animator has no trouble processing a sudden change in direction when a new goal is 
   called while standing). Smoothing the direction variable, however, leads to strange walking behaviors such as walking S-curves. If the character should 
   switch between goals seemlessly, perhaps consider modelling the path dynamically using splines. 
3. If your character has to walk narrow curves, consider reducing the speed depending on direction.
*/

using UnityEngine;
using System;
using System.Collections;

// [RequireComponent(typeof(Animator))] // creates animator if none has been set up yet. Quite pointless, since the referred locomotion controller won't be found if there's no animator.

public class WalkTo : MonoBehaviour {

    [Tooltip("Insert object 1 that should be walked to.")]
    public Transform goal1;
    [Tooltip("Insert object 2 that should be walked to.")]
    public Transform goal2;
    [Tooltip("Insert object 3 that should be walked to.")]
    public Transform goal3;
    [Tooltip("Click to have agent walk to goal1.")]
    public bool WalkTo1;
    [Tooltip("Click to have agent walk to goal2.")]
    public bool WalkTo2;
    [Tooltip("Click to have agent walk to goal3.")]
    public bool WalkTo3;

    [Tooltip("Enable this to draw path as red line.")]
    public bool DrawPath = false;
    [Tooltip("Maximum speed of agent to walk towards goal. Insert value between 0 and 1.")]
    public float maxspeed = 0.5f;
    [Tooltip("The minimum distance in units that the agent should respect to its goal. 0 means agent gets so close that it touches the goal. Default: 1.")]
    public float mindist = 1f;
    [Tooltip("If the angle along the path exceeds this value, agent turns around standing instead of walking a curve. Default: 120.")]
    public float anglecut = 120;

    protected Animator animator;
    private Locomotion locomotion = null; // connect to locomotion controller
    private Transform CurrentGoal;
    Collider CurrentGoalCollider; //Collider of goal. Called so that agent stops in front of object, not in front of its center.
    
    Vector3[] PointsOnPath; //all points on smoothed out path
    int whereOnPath; // position of targetSub in PointsOnPath list
    int nWaypoints = 0; // total number of points on path
    Vector3 targetSub; // the point on the path that is currently being steered to
    Vector3 GoalPosition = Vector3.zero; // to track if goal is being moved

    bool GoalHasChanged = false; // detects new click
    bool SlowingDown = false; // brings agent to halt before targeting new goal
    float speed = 0; 
    float direction = 0;

    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>(); // get this agent's animator
        locomotion = new Locomotion(animator); // get this agent's locomotion script
    }
	
	// Update is called once per frame
	void Update () {

        DetermineCurrentGoal(out GoalHasChanged); // Check if new goal is being clicked
        
        // Before walking to new goal, slow down. Start walking for new goal once movement is stopped (or, if already standing, immediately).
        if (GoalHasChanged) { SlowingDown = true; }
        if (SlowingDown)
        {
            speed = speed * 0.95f; // slow down: The closer the value gets to 1, the softer the slowing down. 
            direction = 0;

            if (speed < 0.02) // only continue walking to new target when speed is almost 0. (Value will be below 0.02 after 63 frames when speed was 0.5 and slow-down factor is .95.)
            {
                SlowingDown = false;
                PointsOnPath = FindPathToGoal(transform.position, CurrentGoal.position);
                whereOnPath = 50; // set first subgoal 50cm away from agent. Otherwise the first subgoal will be exactly at the agent's position, possibly causing confusion.
                nWaypoints = PointsOnPath.GetLength(0);
            }
        }

        // if goal moves, continuously look for path
        if (CurrentGoal != null)
        { 
            if (Vector3.Magnitude(GoalPosition - CurrentGoal.position) > .01f)

                {
                    PointsOnPath = FindPathToGoal(transform.position, CurrentGoal.position);
                    whereOnPath = 50;
                    nWaypoints = PointsOnPath.GetLength(0);
                }
            GoalPosition = CurrentGoal.position;
        }
        

        // draw path as red line (in scene view, not in game view)
        if (DrawPath == true && nWaypoints > 0) 
        {
            for (int i = 1; i < nWaypoints - 1; i++) { Debug.DrawLine(PointsOnPath[i], PointsOnPath[i + 1], Color.red); }
        }

        // Compute speed and direction according to path to goal and distance to goal
        if (CurrentGoal != null && SlowingDown == false) 
        {
            DetermineSpeedAndDirection(out speed, out direction);
        }

        // transfer speed and direction to animator component
        if (animator && CurrentGoal != null) 
        {
            locomotion.Do(speed * 6, direction);
        }

        }

    /// <summary>
    /// Determine current goal: leave if nothing is clicked, change to new Goal on click
    /// <param name="GoalHasChanged">States whether there's a click on a valid transform which was not clicked the frame before.</param>
    /// </summary>
    private void DetermineCurrentGoal(out bool GoalHasChanged)
    {
        GoalHasChanged = false; // first assume goal remained the same

        if (WalkTo1 == true) // if click on this bool...
        {
            if (CurrentGoal != goal1 && goal1 != null) // only if CurrentGoal was not goal1 in the last frame...
            {
                GoalHasChanged = true; // can you call it a change...
                CurrentGoal = goal1; // and insert correct CurrentGoal and Collider
                CurrentGoalCollider = CurrentGoal.GetComponent<Collider>();
            }
            WalkTo1 = false; // always set variable back to false
        }

        if (WalkTo2 == true)
        {
            if (CurrentGoal != goal2 && goal2 != null)
            {
                GoalHasChanged = true;
                CurrentGoal = goal2;
                CurrentGoalCollider = CurrentGoal.GetComponent<Collider>();
            }
            WalkTo2 = false;
        }

        if (WalkTo3 == true)
        {
            if (CurrentGoal != goal3 && goal3 != null)
            {
                GoalHasChanged = true;
                CurrentGoal = goal3;
                CurrentGoalCollider = CurrentGoal.GetComponent<Collider>();
            }
            WalkTo3 = false;
        }
    }

    /// <summary>
    /// This uses Unity's own Navigation Mesh to find a path to the goal. 
    /// This is done using an A* algorithm that comes with Unity. There exist much more elaborate versions of this algorithm on 
    /// the asset store that can cope with three-dimensional scenes and lots of moving objects. For simple purposes on flat scenes, 
    /// however, the version used here should be fine. 
    /// <param name="origin">Beginning point of the path. Here the agent itself, transform.position.</param>
    /// <param name="goal">The goal to which we need to find a path. Here the position of the CurrentGoal.</param>
    /// </summary>
    private Vector3[] FindPathToGoal(Vector3 origin, Vector3 goal)
    {
        Vector3[] PathCorners; // will be the corners of the path
        Vector3[] PointsOnPath; // all points on smoothed out path
        UnityEngine.AI.NavMeshPath path;
        path = new UnityEngine.AI.NavMeshPath();
        UnityEngine.AI.NavMesh.CalculatePath(origin, goal, UnityEngine.AI.NavMesh.AllAreas, path);
        PathCorners = path.corners; // this only returns a list of vectors describing the corner points of the found path
        PointsOnPath = FindSmoothPathBetweenPoints(PathCorners, 60); //Return points on smoothed out path between corner points. Smoothing = 60.
        return PointsOnPath;
    }

    /// <summary>
    /// Easy way to find a smoothed out path, when only corner points are given.
    /// First, the corner points are ammended with points between the corner points, creating a jagged path.
    /// Next, the jaggs are cut off two create a path which is still jagged, but a little less so. This can easily be repeated to get a smoother path.
    /// The procedure should be computationally cheap and robust, while being smooth enough for this purpose. 
    /// If the path should be actually smooth, I suggeset looking into these two possibilites: 
    /// a) splines from a tweening engine (may however lead to strange results if two corner points are close to each other) or 
    /// b) bezier curves (somewhat more complicated to set up).
    /// <param name="CornerPoints">The corner points defining a jagged path. Like the result from NavMesh.CalculatePath.</param>
    /// <param name="smoothing">The size of the cut from these corners. The larger, the "smoother" the path will be.</param>
    /// </summary>
    private Vector3[] FindSmoothPathBetweenPoints(Vector3[] CornerPoints, int smoothing)
    {
        // First get length of the path to determine how many points we will need to store. 
        int pathlength = 0;
        for (int i = 0; i < CornerPoints.Length - 1; i++)
        {
            Vector3 OnePointToNext = (CornerPoints[i + 1] - CornerPoints[i]); //for each vector between corner points...
            pathlength += (int)Math.Floor((OnePointToNext.magnitude) * 100); //get the distance in 1/100 units
        }
        Vector3[] PointsOnPath = new Vector3[pathlength];


        // Next fill results[] with many points forming the jagged path between the cornerpoints, with no smoothing applied yet
        int counter = 0; // counts points in result
        for (int i = 0; i < CornerPoints.Length - 1; i++) 
        {
            Vector3 OnePointToNext = CornerPoints[i + 1] - CornerPoints[i]; //for each vector between corner points...
            int LengthThisPart = (int)Math.Floor((OnePointToNext.magnitude) * 100); //get the distance in 1/100 units
            for (int j = 0; j < LengthThisPart; j++)
            {
                Vector3 ThisPoint;
                ThisPoint = Vector3.Lerp(CornerPoints[i], CornerPoints[i + 1], (float)j / LengthThisPart); //go from one point to next stepwise
                PointsOnPath[counter] = ThisPoint; // results[counter] can easily move from one CornerPoint to another, whereas result[j] would have to find the transition
                counter++;
            }
        }
        // Smoothing: replace each point with the mean of two points that are a certain distance ahead of behind in list
        Vector3[] PointsOnPath2 = PointsOnPath; 
        for (int i = smoothing; i < counter - smoothing; i++)
        {
            PointsOnPath2[i] = (PointsOnPath[i - smoothing] + PointsOnPath[i + smoothing]) / 2;
        }
        //Vector3[] result = PointsOnPath2;
        return PointsOnPath2;
    }

    private void DetermineSpeedAndDirection(out float speed, out float direction)
    {
        // Determine speed according to distance to goal
        Vector3 VectorToTarget;
        VectorToTarget = transform.position - CurrentGoalCollider.ClosestPointOnBounds(transform.position); //get distance not to goal, but to its outer boundary
        VectorToTarget.y = 0; // don't look at y difference
        float DistanceToTarget;
        DistanceToTarget = VectorToTarget.magnitude;
        speed = 0; // first set 0 to be robust in cases when speed is not changed in following lines
        if (DistanceToTarget > mindist) { speed = DistanceToTarget * 0.2f; }
        if (speed > maxspeed) { speed = maxspeed; } // clamp speed at maximum speed as set in inspector.                                
        if (DistanceToTarget <= mindist) { speed = 0f; } // possible bug: make smaller than mindist
        // get distance to targetSub (= point on the path that is currently being steered to), and change targetSub when it comes close
        // leave Pathfinding mode when close to target
        if (whereOnPath < nWaypoints) { targetSub = PointsOnPath[whereOnPath]; } // get new subgoal only if on path
        Vector3 VectorToTargetSub;
        VectorToTargetSub = transform.position - targetSub;
        VectorToTargetSub.y = 0; // don't look at y difference
        float DistanceToTargetSub;
        DistanceToTargetSub = VectorToTargetSub.magnitude;

        // Switch to next targetSub
        if (DistanceToTargetSub < 0.5f + speed*2) // moving targetSub further away as speed increases reduces overshoots in angular adaption to path (running of S-curves). 
        {
            whereOnPath = whereOnPath + 10; // 10 should work as long as character is slower than 10/100 units per frame (6 units/sec)
        }
        if (whereOnPath > nWaypoints)
        {
            speed = 0;
        }
        
        // determine angle to targetSub
        Vector3 directionTotargetSub;
        directionTotargetSub = transform.position - targetSub;
        directionTotargetSub = transform.InverseTransformDirection(directionTotargetSub);
        float angle;
        angle = Mathf.Atan2(directionTotargetSub.z, directionTotargetSub.x) * Mathf.Rad2Deg; // turn vector into angle
        angle = angle + 90f; // turn round 90°
        if (angle >= 180f) { angle = angle - 360f; } // bring to a value between -180 and 180
        if (angle <= -180f) { angle = angle + 360f; }

        // if in doubt, turn round to the left. This is important when the agent has to turn around
        // by about 180 degrees after changing the goal. Without this adaption, the angle may jump 
        // between e.g. -175 and +175 for some frames, causing a jittering animation. 
        if (angle < -150 || angle > 150) { angle = 150; } 

        // transfer angle to direction
        direction = -angle;
    }

}
