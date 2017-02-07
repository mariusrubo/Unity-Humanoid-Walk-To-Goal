using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkToGoal : MonoBehaviour {

    protected Animator animator;
    private Locomotion locomotion = null;
    Vector3[] PointsOnPath; //all points on smoothed out path
    int whereOnPath = 0; // position of targetSub in PointsOnPath list
    int nWaypoints = 0; // total number of points on path
    Vector3 GoalPosition; // to track if goal is being moved
    Collider CurrentGoalCollider; //Collider of goal. Called so that agent stops in front of object, not in front of its center.
    Transform GoalLastFrame = null; // tracks what the goal was, in order to detect when goal is being changed
    Vector3 VectorToTarget;
    Vector3 targetSub;
    bool SlowingDown = false; // brings agent to halt before targeting new goal
    float speed = 0;
    float direction = 0;

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>(); // get this agent's animator
        locomotion = new Locomotion(animator); // get this agent's controller called locomotion
    }

    /// <summary>
    /// The actual walking function, which can be controlled from outside the script.
    /// The function must be called every frame in the Updata()-Loop in order to keep the character walking to the goal. 
    /// <param name="CurrentGoal">The Transform the character should walk to. </param>
    /// <param name="DrawPath">Bool stating whether the path should be drawn as a line in the scene view.</param>
    /// <param name="maxspeed">A value from 0 to 1 stating how fast the character should walk (1 = maximum speed, typically running).</param>
    /// <param name="mindist">How close the character should approach the goal. If character tends to walk "into" the goal, set this value a little higher.</param>
    /// <param name="anglecut">Angle in degrees at which character does not walk a curve, but turns around standing. I typically choose 120.</param>
    /// </summary>
    public bool WalkTo(Transform CurrentGoal, bool DrawPath, float maxspeed, float mindist, float anglecut)
    {
        bool GoalReached = false;
        if (CurrentGoal != GoalLastFrame)  // check automatically when goal changes, without being told from outside the function
        {
            SlowingDown = true;
            CurrentGoalCollider = CurrentGoal.GetComponent<Collider>();
        } 
        GoalLastFrame = CurrentGoal;

        // Before walking to new goal, slow down. Start walking for new goal once movement is stopped (or, if already standing, immediately).
        if (SlowingDown)
        {
            speed = speed * 0.95f; // slow down: The closer the value gets to 1, the softer the slowing down. 
            direction = 0;

            if (speed < 0.02) // only continue walking to new target when speed is almost 0. (Value will be below 0.02 after 63 frames when speed was 0.5 and slow-down factor is .95.)
            {
                SlowingDown = false;
                PointsOnPath = FindPathToGoal(transform.position, CurrentGoal.position);
                whereOnPath = 100; // set first subgoal 100cm away from agent. Otherwise the first subgoal will be exactly at the agent's position, possibly causing confusion.
                nWaypoints = PointsOnPath.GetLength(0);
            }
        }

        // if goal moves, continuously look for path
        if (CurrentGoal != null)
        {
            if (Vector3.Magnitude(GoalPosition - CurrentGoal.position) > .01f)

            {
                PointsOnPath = FindPathToGoal(transform.position, CurrentGoal.position);
                whereOnPath = 100;
                nWaypoints = PointsOnPath.GetLength(0);
            }
            GoalPosition = CurrentGoal.position;
        }


        // Compute speed and direction according to path to goal and distance to goal
        if (CurrentGoal != null && SlowingDown == false)
        {
            //DetermineSpeedAndDirection(out speed, out direction);
            //whereOnPath = DetermineSpeedAndDirection(CurrentGoal, CurrentGoalCollider, mindist, maxspeed, whereOnPath, nWaypoints, PointsOnPath, out speed, out direction);

            if (CurrentGoalCollider != null) { VectorToTarget = transform.position - CurrentGoalCollider.ClosestPointOnBounds(transform.position); } //get distance not to goal, but to its outer boundary, if it has collider
            else VectorToTarget = transform.position - CurrentGoal.position;
            VectorToTarget.y = 0; // don't look at y difference
            float DistanceToTarget;
            DistanceToTarget = VectorToTarget.magnitude;
            speed = 0; // first set 0 to be robust in cases when speed is not changed in following lines
            if (DistanceToTarget > mindist) { speed = (DistanceToTarget - mindist) * 0.2f; } // this value determines slowing down before goal
            if (speed > maxspeed) { speed = maxspeed; } // clamp speed at maximum speed as set in inspector.                                
            if (DistanceToTarget <= mindist) { speed = 0f; GoalReached = true; }// GoalReached = true; } // possible bug: make smaller than mindist
                                                                                // get distance to targetSub (= point on the path that is currently being steered to), and change targetSub when it comes close
                                                                                // leave Pathfinding mode when close to target
            if (DistanceToTarget <= mindist + .2f) { speed = 0f; GoalReached = true; } // Goal is "reached" 20cm before stopping. This is safer as stopping point varies by +-10cm and "reached"-detection should be robust.
            if (whereOnPath < nWaypoints) { targetSub = PointsOnPath[whereOnPath]; } // get new subgoal only if on path
            Vector3 VectorToTargetSub;
            VectorToTargetSub = transform.position - targetSub;
            VectorToTargetSub.y = 0; // don't look at y difference
            float DistanceToTargetSub;
            DistanceToTargetSub = VectorToTargetSub.magnitude;

            // Switch to next targetSub
            if (DistanceToTargetSub < 0.5f + speed * 2) // moving targetSub further away as speed increases reduces overshoots in angular adaption to path (running of S-curves). 
            {
                whereOnPath = whereOnPath + 10; // 10 should work as long as character is slower than 10/100 units per frame (6 units/sec)
            }
            //if (DistanceToTargetSub < 0.1f) { GoalReached = true; }

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

        // draw path as red line (in scene view, not in game view)
        if (DrawPath == true && nWaypoints > 0)
        {
            for (int i = 1; i < nWaypoints - 1; i++) { Debug.DrawLine(PointsOnPath[i], PointsOnPath[i + 1], Color.red); }
        }

        // transfer speed and direction to animator component
        if (animator && CurrentGoal != null)
        {
            locomotion.Do(speed * 6, direction);
        }
        return GoalReached; // report back if goal has been reached yet
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
            pathlength += (int)Mathf.Floor((OnePointToNext.magnitude) * 100); //get the distance in 1/100 units
        }
        Vector3[] PointsOnPath = new Vector3[pathlength];


        // Next fill results[] with many points forming the jagged path between the cornerpoints, with no smoothing applied yet
        int counter = 0; // counts points in result
        for (int i = 0; i < CornerPoints.Length - 1; i++)
        {
            Vector3 OnePointToNext = CornerPoints[i + 1] - CornerPoints[i]; //for each vector between corner points...
            int LengthThisPart = (int)Mathf.Floor((OnePointToNext.magnitude) * 100); //get the distance in 1/100 units
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
        return PointsOnPath2;
    }

}
