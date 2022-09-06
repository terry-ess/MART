# Room Survey Skill
The room survey skill explores what it takes to effectively learn new rooms. It gathers all the information necessary to create a new room's data base and 2D map.  The current implementation uses an off-line program to manually combine all the gathered information into a usable map but it could be accomplished by the robot itself.

The operation is controlled by the SurveyPrepThread() and  in WorkAssist.cs.  It executes the following steps:

1. The SurveyPrepThread() obtains the information necessary to execute the survery.
2. The SurveyExecutionThread() executes the survey.  It takes the following steps.

   - Moves to the entry point for the survey using a "go to" command.
   - Determines an entry strategy.
   - Attempts to execute the strategy, which includes both LIDAR and KINECT based information gathering.
   - Returns to the entry point.
   - Returns to its start point.
