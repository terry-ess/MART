# Work Assist Skill
The work assist skill is a limited exploration of close robot - human cooperation.  The robot in this implementation is effectively just a smart third arm tool.  The main reason for the skill is to see what it takes to understand work space and how to move into and out of position.  The primary assumptions of this skill are:

1. The command "come here" is used to get the robot close to the person it will assist.
2. The person is at the work location [THIS IS KEY, the person is the absolute reference in terms of location of the work area. Otherwise would need extensive room context.].
3. The orientation of the work area is parallel or perpendicular to a "prime" direction (0,90,180,270).

The operation is controlled by the WorkerThread() in WorkAssist.cs.  It executes the following steps:

1. Gathers basic work area data (WorkAreaData.cs)
2. Based on the work area robot-person arrangement, same side, opposite side or opposite side, it:
    1. Obtains detailed information about the work space.
    2. Moves to its work position.
3. Performs the "work assist" task (AutoArm.cs). The command grammar for this task is constructed dynamically in the AddGrammar function.
4. Upon completion it returns to its start position (MoveToStart.cs).

Each of the steps required the accumulation of a significant number of assumptions about the environment etc.  Though I generally do not comment code any more, the listing of these key assumptions have been left at the top of the main classes as comments.
