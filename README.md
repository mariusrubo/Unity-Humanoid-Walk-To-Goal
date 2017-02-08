# Purpose
These scripts allow Unity 3D beginners to make a humanoid character walk to a goal in a natural way. The character will:
* first turn round if the goal is located behind the character
* walk to the goal at a specified speed, smoothly avoiding obstacles specified in the Navigation Mesh
* slow down when approaching the goal, and stop in front of it at a specified distance

# Installation
* The character must have an animator and a controller which is called "Locomotion" and can be steered with a "speed" and a "direction" variable (I recommend simply using the controller from "Mecanim Locomotion Starter Kit" in the AssetStore)
* The objects that serve as goals should have a (box) collider
* Attach "WalkToGoal.cs" to the character. You do not need to manipulate this script.
* Attach "WalkToGoalInterface.cs" to any object (possibly, but not necessarily the character). This script will call the actual walking function inside "WalkToGoal.cs". Calling it from outside "WalkToGoal.cs" allows you to centralise all character activities in one script (e.g. walking, looking or grasping behavior of various characters at the same time)
* Drag the character's and the goals' transforms to the appropriate places in the inspector's view on "WalkToGoalInterface.cs"
* Press play, click on GUI buttons in the game view

# License
These scripts run under the GPLv3 license. See the comments inside the scripts for more details and ideas on how to adapt them for your own projects.

